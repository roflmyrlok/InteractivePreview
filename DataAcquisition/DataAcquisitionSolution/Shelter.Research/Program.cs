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
var village = GetArg(argv, "--village");
var dryRun = argv.Contains("--dry-run");
var mergeOnly = argv.Contains("--merge-only");
var remap = argv.Contains("--remap");
// --discover: when a node has no Active sources, run AI discovery (auto-activated) and re-fetch.
var discover = argv.Contains("--discover");
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
var ct = CancellationToken.None;

// Any tier can be the entry point; each recurses into all levels below it and merges upward.
//   --oblast X                       → oblast + every hromada + every village
//   --oblast X --hromada H           → hromada H + its villages
//   --oblast X --hromada H --village V → village V only
if (registry != null)
    await RunRegistryModeAsync();
else
    await RunFilesystemModeAsync();

return 0;

// ───────────────────────── Registry mode (automated, recursive) ─────────────────────────
async Task RunRegistryModeAsync()
{
    if (!string.IsNullOrEmpty(village))
    {
        if (string.IsNullOrEmpty(hromada)) { logger.LogError("--village requires --hromada"); return; }
        var hid = await registry!.FindHromadaIdAsync(oblast, hromada, ct);
        if (hid == null) { logger.LogError("Hromada '{H}' not found under '{O}'", hromada, oblast); return; }
        var match = (await registry.ListVillageIdsAsync(hid.Value, ct))
            .FirstOrDefault(v => string.Equals(v.Slug, village, StringComparison.OrdinalIgnoreCase));
        if (match == default) { logger.LogError("Village '{V}' not found under '{H}'", village, hromada); return; }
        await RunVillageNodeAsync(hromada, village, match.Id);
        research.MergeVillagesIntoHromada(oblast, hromada, dryRun);
        if (!dryRun) research.MergeOblastOutput(oblast);
        return;
    }

    if (!string.IsNullOrEmpty(hromada))
    {
        var hid = await registry!.FindHromadaIdAsync(oblast, hromada, ct);
        if (hid == null) { logger.LogError("Hromada '{H}' not found under '{O}'", hromada, oblast); return; }
        await RunHromadaNodeAsync(hromada, hid.Value);
        if (!dryRun) research.MergeOblastOutput(oblast);
        return;
    }

    // Whole oblast.
    var oblastSources = await registry!.GetOblastSourcesAsync(oblast, ct);
    if (oblastSources.Count == 0 && discover)
    {
        var oid = await registry.GetOblastIdAsync(oblast, ct);
        logger.LogInformation("[{O}] No Active oblast sources — running AI discovery", oblast);
        if (!await registry.TriggerDiscoveryAsync("oblast", oid, true, ct))
            logger.LogWarning("[{O}] Discovery request failed — continuing without new oblast sources", oblast);
        oblastSources = await registry.GetOblastSourcesAsync(oblast, ct);
    }
    logger.LogInformation("[{O}] Oblast-level sources: {C}", oblast, oblastSources.Count);
    await research.RunOblastLevelAsync(oblast, dryRun, ct, oblastSources);

    foreach (var (slug, id) in await registry.ListHromadaIdsAsync(oblast, ct))
    {
        if (slug.StartsWith("_")) continue;
        try { await RunHromadaNodeAsync(slug, id); }
        catch (Exception ex) { logger.LogError(ex, "[{O}/{H}] Failed", oblast, slug); }
    }

    if (!dryRun) research.MergeOblastOutput(oblast);
}

// Run a hromada's own sources, then each of its villages, then merge villages up into the hromada.
async Task RunHromadaNodeAsync(string slug, Guid id)
{
    var sources = await registry!.GetHromadaSourcesAsync(id, ct);
    if (sources.Count == 0 && discover)
    {
        logger.LogInformation("[{O}/{H}] No Active sources — running AI discovery", oblast, slug);
        if (!await registry.TriggerDiscoveryAsync("hromada", id, true, ct))
            logger.LogWarning("[{O}/{H}] Discovery request failed — continuing without new sources", oblast, slug);
        sources = await registry.GetHromadaSourcesAsync(id, ct);
    }
    if (sources.Count > 0)
        await research.RunHromadaAsync(oblast, slug, dryRun, ct, sources);
    else
        logger.LogInformation("[{O}/{H}] No sources — skipping hromada-level fetch", oblast, slug);

    foreach (var (vslug, vid) in await registry.ListVillageIdsAsync(id, ct))
    {
        if (vslug.StartsWith("_")) continue;
        try { await RunVillageNodeAsync(slug, vslug, vid); }
        catch (Exception ex) { logger.LogError(ex, "[{O}/{H}/{V}] Failed", oblast, slug, vslug); }
    }

    research.MergeVillagesIntoHromada(oblast, slug, dryRun);
}

async Task RunVillageNodeAsync(string hromadaSlug, string villageSlug, Guid villageId)
{
    var sources = await registry!.GetVillageSourcesAsync(villageId, ct);
    if (sources.Count == 0 && discover)
    {
        logger.LogInformation("[{O}/{H}/{V}] No Active sources — running AI discovery", oblast, hromadaSlug, villageSlug);
        if (!await registry.TriggerDiscoveryAsync("village", villageId, true, ct))
            logger.LogWarning("[{O}/{H}/{V}] Discovery request failed — continuing without new sources", oblast, hromadaSlug, villageSlug);
        sources = await registry.GetVillageSourcesAsync(villageId, ct);
    }
    if (sources.Count > 0)
        await research.RunVillageAsync(oblast, hromadaSlug, villageSlug, dryRun, ct, sources);
    else
        logger.LogInformation("[{O}/{H}/{V}] No sources — skipping", oblast, hromadaSlug, villageSlug);
}

// ───────────────────────── Filesystem mode (manual/offline) ─────────────────────────
async Task RunFilesystemModeAsync()
{
    if (!string.IsNullOrEmpty(village))
    {
        if (string.IsNullOrEmpty(hromada)) { logger.LogError("--village requires --hromada"); return; }
        await research.RunVillageAsync(oblast, hromada, village, dryRun, ct);
        research.MergeVillagesIntoHromada(oblast, hromada, dryRun);
        return;
    }

    if (!string.IsNullOrEmpty(hromada))
    {
        await research.RunHromadaAsync(oblast, hromada, dryRun, ct);
        await RunVillagesFromFilesystemAsync(hromada);
        research.MergeVillagesIntoHromada(oblast, hromada, dryRun);
        return;
    }

    await research.RunOblastLevelAsync(oblast, dryRun, ct);
    foreach (var h in paths.ListHromadas(oblast))
    {
        if (h.StartsWith("_")) continue;
        if (File.Exists(paths.HromadaSourcesPath(oblast, h)))
        {
            try { await research.RunHromadaAsync(oblast, h, dryRun, ct); }
            catch (Exception ex) { logger.LogError(ex, "[{O}/{H}] Failed", oblast, h); }
        }
        else
        {
            logger.LogInformation("[{O}/{H}] No sources.json — skipping hromada-level fetch", oblast, h);
        }
        await RunVillagesFromFilesystemAsync(h);
        research.MergeVillagesIntoHromada(oblast, h, dryRun);
    }

    if (!dryRun) research.MergeOblastOutput(oblast);
}

async Task RunVillagesFromFilesystemAsync(string hromadaSlug)
{
    foreach (var v in paths.ListVillages(oblast, hromadaSlug))
    {
        if (v.StartsWith("_")) continue;
        if (!File.Exists(paths.VillageSourcesPath(oblast, hromadaSlug, v)))
        {
            logger.LogInformation("[{O}/{H}/{V}] No sources.json — skipping", oblast, hromadaSlug, v);
            continue;
        }
        try { await research.RunVillageAsync(oblast, hromadaSlug, v, dryRun, ct); }
        catch (Exception ex) { logger.LogError(ex, "[{O}/{H}/{V}] Failed", oblast, hromadaSlug, v); }
    }
}

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
