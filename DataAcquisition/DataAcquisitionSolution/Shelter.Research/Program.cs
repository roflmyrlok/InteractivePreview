using Anthropic.SDK;
using Microsoft.Extensions.Logging;
using Shelter.Research;
using Shelter.Shared.Ai;
using Shelter.Shared.Dedup;
using Shelter.Shared.IO;
using Shelter.Shared.Parsers;
using Shelter.Shared.Sources;

// Shelter.Research: top-down research runner.
//
// Filesystem mode (default):
//   dotnet run -- --oblast KyivOblast                    (full oblast: oblast-level + each hromada + merge)
//   dotnet run -- --oblast KyivOblast --hromada vyshneve (single hromada only)
//   dotnet run -- --oblast KyivOblast --merge-only       (skip fetching, just merge hromadas → oblast)
//   dotnet run -- --oblast KyivOblast --dry-run          (no file writes)
//   dotnet run -- --oblast KyivOblast --remap            (delete cached AI mappings before run)
//   dotnet run -- --oblast KyivOblast --anthropic-key sk-...  (or set ANTHROPIC_API_KEY env var)
//
// Registry mode (--registry-url):
//   dotnet run -- --oblast UA32 --registry-url https://host/api --registry-token <jwt>
//   dotnet run -- --oblast UA32 --hromada brovary --registry-url https://host/api --registry-token <jwt>
//   In registry mode --oblast is the oblast code (e.g. UA32) and --hromada is the slug.
//   Source lists come from the SourceRegistryService API (Active status only).
//   REGISTRY_TOKEN env var is also accepted instead of --registry-token.
//
// Reads from: ../../Schema/shelter-schema.json
//             (+ filesystem sources when registry not configured)
// Writes to:  ../../Oblasts/<Oblast>/hromadas/<H>/output.json,
//             ../../Oblasts/<Oblast>/output.json

var argv = Environment.GetCommandLineArgs().Skip(1).ToArray();
var oblast = GetArg(argv, "--oblast") ?? throw new ArgumentException("--oblast is required");
var hromada = GetArg(argv, "--hromada");
var dryRun = argv.Contains("--dry-run");
var mergeOnly = argv.Contains("--merge-only");
var remap = argv.Contains("--remap");
var anthropicKey = GetArg(argv, "--anthropic-key")
    ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
    ?? "";
var registryUrl = GetArg(argv, "--registry-url");
var registryToken = GetArg(argv, "--registry-token")
    ?? Environment.GetEnvironmentVariable("REGISTRY_TOKEN")
    ?? "";
RegistryClient? registry = null;
if (!string.IsNullOrEmpty(registryUrl))
{
    if (string.IsNullOrEmpty(registryToken))
    {
        Console.Error.WriteLine("--registry-url requires --registry-token or REGISTRY_TOKEN env var.");
        return 1;
    }
    registry = RegistryClient.Create(registryUrl, registryToken);
}

// Find repo root: walk up from the executable until we find a Schema/ folder
var repoRoot = FindRepoRoot();
var paths = new PathLayout(repoRoot);

using var loggerFactory = LoggerFactory.Create(b => b.AddSimpleConsole(o =>
{
    o.SingleLine = true;
    o.TimestampFormat = "HH:mm:ss ";
}));
var logger = loggerFactory.CreateLogger("Shelter.Research");

logger.LogInformation("Repo root: {Root}", repoRoot);

if (mergeOnly)
{
    var runner = BuildRunner();
    runner.MergeOblastOutput(oblast);
    return 0;
}

if (string.IsNullOrEmpty(anthropicKey))
{
    Console.Error.WriteLine("ANTHROPIC_API_KEY not set. Pass --anthropic-key or set the env var.");
    return 1;
}

var research = BuildRunner();

if (!string.IsNullOrEmpty(hromada))
{
    if (registry != null)
    {
        var hromadaId = await registry.FindHromadaIdAsync(oblast, hromada, CancellationToken.None);
        if (hromadaId == null)
        {
            logger.LogError("Hromada slug '{Slug}' not found under oblast '{Code}' in registry", hromada, oblast);
            return 1;
        }
        var sources = await registry.GetHromadaSourcesAsync(hromadaId.Value, CancellationToken.None);
        logger.LogInformation("[{Oblast}/{Hromada}] Registry sources: {Count}", oblast, hromada, sources.Count);
        await research.RunHromadaAsync(oblast, hromada, dryRun, CancellationToken.None, sources);
    }
    else
    {
        await research.RunHromadaAsync(oblast, hromada, dryRun, CancellationToken.None);
    }
}
else if (registry != null)
{
    // Registry mode: fetch all hromadas for this oblast code from the API
    var oblastSources = await registry.GetOblastSourcesAsync(oblast, CancellationToken.None);
    logger.LogInformation("[{Oblast}] Registry oblast-level sources: {Count}", oblast, oblastSources.Count);
    await research.RunOblastLevelAsync(oblast, dryRun, CancellationToken.None, oblastSources);

    var hromadaIds = await registry.ListHromadaIdsAsync(oblast, CancellationToken.None);
    foreach (var (slug, id) in hromadaIds)
    {
        if (slug.StartsWith("_")) continue;
        try
        {
            var sources = await registry.GetHromadaSourcesAsync(id, CancellationToken.None);
            if (sources.Count == 0)
            {
                logger.LogInformation("[{Oblast}/{H}] No Active sources in registry — skipping", oblast, slug);
                continue;
            }
            await research.RunHromadaAsync(oblast, slug, dryRun, CancellationToken.None, sources);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[{Oblast}/{H}] Failed", oblast, slug);
        }
    }

    if (!dryRun) research.MergeOblastOutput(oblast);
}
else
{
    // Filesystem mode: oblast-level → distribute to hromadas → run each hromada's own sources → merge
    await research.RunOblastLevelAsync(oblast, dryRun, CancellationToken.None);

    foreach (var h in paths.ListHromadas(oblast))
    {
        if (h.StartsWith("_")) continue;
        if (!File.Exists(paths.HromadaSourcesPath(oblast, h)))
        {
            logger.LogInformation("[{Oblast}/{H}] No sources.json — skipping", oblast, h);
            continue;
        }
        try
        {
            await research.RunHromadaAsync(oblast, h, dryRun, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[{Oblast}/{H}] Failed", oblast, h);
        }
    }

    if (!dryRun) research.MergeOblastOutput(oblast);
}

return 0;

ResearchRunner BuildRunner()
{
    var schema = JsonIO.Read<Shelter.Shared.Models.Schema>(paths.SchemaPath);

    var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
    http.DefaultRequestHeaders.UserAgent.ParseAdd("ShelterResearch/1.0");

    var parsers = new ISourceParser[]
    {
        new KmzParser(),
        new KmlParser(),
        new EsriJsonParser(),
        new CsvParser(),
        new HtmlParser()  // last — fallback
    };

    var fetcher = new SourceFetcher(http, parsers, loggerFactory.CreateLogger<SourceFetcher>());

    var cache = new MappingCache(paths.MappingsCacheDir);
    var anthropic = new AnthropicClient(anthropicKey);
    var mapper = new ClaudeMapper(anthropic, schema, cache, loggerFactory.CreateLogger<ClaudeMapper>());

    var dedup = new Deduplicator(loggerFactory.CreateLogger<Deduplicator>());

    if (remap)
    {
        logger.LogInformation("--remap: clearing AI mapping cache at {Path}", paths.MappingsCacheDir);
        if (Directory.Exists(paths.MappingsCacheDir))
            Directory.Delete(paths.MappingsCacheDir, recursive: true);
    }

    return new ResearchRunner(paths, schema, fetcher, mapper, dedup,
        loggerFactory.CreateLogger<ResearchRunner>());
}

static string? GetArg(string[] args, string flag)
{
    var i = Array.IndexOf(args, flag);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}

static string FindRepoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir != null)
    {
        if (Directory.Exists(Path.Combine(dir.FullName, "Schema"))
            && Directory.Exists(Path.Combine(dir.FullName, "Oblasts")))
            return dir.FullName;
        dir = dir.Parent;
    }
    throw new InvalidOperationException("Could not find DataAcquisition root (looking for Schema/ and Oblasts/ siblings).");
}
