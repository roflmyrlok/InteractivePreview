using Microsoft.Extensions.Logging;
using Shelter.Shared.Ai;
using Shelter.Shared.Dedup;
using Shelter.Shared.IO;
using Shelter.Shared.Models;
using Shelter.Shared.Sources;

namespace Shelter.Research;

public class ResearchRunner(
    PathLayout paths,
    Schema schema,
    SourceFetcher fetcher,
    ClaudeMapper mapper,
    Deduplicator dedup,
    ILogger<ResearchRunner> logger)
{
    // Process one hromada: fetch sources (from registry if provided, else sources.json), AI-map, merge into output.json.
    public async Task<OutputDocument> RunHromadaAsync(
        string oblast, string hromada, bool dryRun, CancellationToken ct,
        IEnumerable<SourceEntry>? externalSources = null)
    {
        var outputPath = paths.HromadaOutputPath(oblast, hromada);

        HromadaSources sources;
        if (externalSources != null)
        {
            sources = new HromadaSources { Hromada = hromada, Sources = externalSources.ToList() };
        }
        else
        {
            var srcPath = paths.HromadaSourcesPath(oblast, hromada);
            if (!File.Exists(srcPath))
                throw new FileNotFoundException($"sources.json not found at {srcPath}");
            sources = JsonIO.Read<HromadaSources>(srcPath);
        }
        var existing = JsonIO.ReadIfExists<OutputDocument>(outputPath) ?? new OutputDocument
        {
            Meta = new OutputMeta { Oblast = oblast, Hromada = hromada }
        };

        logger.LogInformation("[{Oblast}/{Hromada}] Sources: {Count}, existing records: {Records}",
            oblast, hromada, sources.Sources.Count, existing.Records.Count);

        foreach (var source in sources.Sources)
        {
            await ProcessSource(source, existing, level: "hromada", levelName: hromada, paths.HromadaDir(oblast, hromada), ct);
        }

        existing.Meta.GeneratedAt = DateTime.UtcNow;

        if (!dryRun)
        {
            JsonIO.Write(outputPath, existing);
            logger.LogInformation("[{Oblast}/{Hromada}] Wrote {Path} ({Count} records)", oblast, hromada, outputPath, existing.Records.Count);
        }
        else
        {
            logger.LogInformation("[{Oblast}/{Hromada}] DRY RUN — not writing {Path}", oblast, hromada, outputPath);
        }

        return existing;
    }

    // Process the oblast level: sources → group records by city → distribute to hromada folders.
    public async Task RunOblastLevelAsync(
        string oblast, bool dryRun, CancellationToken ct,
        IEnumerable<SourceEntry>? externalSources = null)
    {
        OblastSources sources;
        if (externalSources != null)
        {
            sources = new OblastSources { Oblast = oblast, Sources = externalSources.ToList() };
        }
        else
        {
            var srcPath = paths.OblastSourcesPath(oblast);
            if (!File.Exists(srcPath))
            {
                logger.LogInformation("[{Oblast}] No oblast-sources.json — skipping oblast level", oblast);
                return;
            }
            sources = JsonIO.Read<OblastSources>(srcPath);
        }
        logger.LogInformation("[{Oblast}] Oblast-level sources: {Count}", oblast, sources.Sources.Count);

        foreach (var source in sources.Sources)
        {
            // Fetch + parse + AI map
            var records = await FetchAndNormalize(source, paths.OblastDir(oblast), ct);

            // Group by city/hromada (heuristic: address contains hromada name)
            var hromadaFolders = paths.ListHromadas(oblast).ToList();
            var grouped = GroupByHromada(records, hromadaFolders);

            foreach (var (hromada, hromadaRecords) in grouped)
            {
                await MergeIntoHromada(oblast, hromada, hromadaRecords, source, dryRun, ct);
            }

            // Records that didn't match any hromada folder — log & save to "_unassigned"
            if (grouped.TryGetValue("_unassigned", out var orphans) && orphans.Count > 0)
            {
                logger.LogWarning("[{Oblast}] {Count} records didn't match any hromada folder (saved under _unassigned)",
                    oblast, orphans.Count);
            }
        }
    }

    // Merge oblast-level records into a hromada's output.json.
    private async Task MergeIntoHromada(
        string oblast, string hromada, List<NormalizedShelter> incoming, SourceEntry source, bool dryRun, CancellationToken _)
    {
        var outputPath = paths.HromadaOutputPath(oblast, hromada);
        Directory.CreateDirectory(paths.HromadaDir(oblast, hromada));

        var existing = JsonIO.ReadIfExists<OutputDocument>(outputPath) ?? new OutputDocument
        {
            Meta = new OutputMeta { Oblast = oblast, Hromada = hromada }
        };

        var merged = dedup.Merge(existing.Records, incoming);
        existing.Records = merged.AllRecords;
        existing.Meta.SourcesUsed.Add(new SourceUsed
        {
            Level = "oblast",
            Url = source.Url,
            FetchedAt = DateTime.UtcNow,
            RecordsAdded = merged.Added,
            DuplicatesMerged = merged.DuplicatesMerged,
            FieldsFilledByMerge = merged.FieldsFilledByMerge
        });
        existing.Meta.GeneratedAt = DateTime.UtcNow;

        if (!dryRun)
            JsonIO.Write(outputPath, existing);

        logger.LogInformation("  → {Hromada}: +{Added} new, {Dup} merged", hromada, merged.Added, merged.DuplicatesMerged);
    }

    private async Task ProcessSource(
        SourceEntry source, OutputDocument output, string level, string levelName, string baseDir, CancellationToken ct)
    {
        var incoming = await FetchAndNormalize(source, baseDir, ct);
        if (incoming.Count == 0)
        {
            logger.LogWarning("Source produced 0 records: {Url}", source.Url);
            return;
        }

        // Inject system fields
        var oblast = output.Meta.Oblast;
        var hromada = output.Meta.Hromada;
        foreach (var r in incoming)
        {
            r.SourceUrl = source.Url;
            r.Details.Add(new DetailDto("Oblast", oblast));
            if (!string.IsNullOrEmpty(hromada))
                r.Details.Add(new DetailDto("City", hromada));
            r.Details.Add(new DetailDto("DataSource", source.Url));
        }

        var merge = dedup.Merge(output.Records, incoming);
        output.Records = merge.AllRecords;
        output.Meta.SourcesUsed.Add(new SourceUsed
        {
            Level = level,
            Name = levelName,
            Url = source.Url,
            FetchedAt = DateTime.UtcNow,
            RecordsAdded = merge.Added,
            DuplicatesMerged = merge.DuplicatesMerged,
            FieldsFilledByMerge = merge.FieldsFilledByMerge
        });
        source.LastFetched = DateTime.UtcNow;
    }

    // Fetch raw records, ask Claude for field mapping, apply mapping → NormalizedShelter list.
    private async Task<List<NormalizedShelter>> FetchAndNormalize(SourceEntry source, string baseDir, CancellationToken ct)
    {
        var raw = await fetcher.FetchAsync(source, baseDir, ct);
        if (raw.Count == 0) return [];

        var mapping = await mapper.GetMappingAsync(source.Url, raw, ct);

        var result = new List<NormalizedShelter>(raw.Count);
        foreach (var r in raw)
        {
            var address = r.Address ?? r.Fields.GetValueOrDefault("address") ?? r.Fields.GetValueOrDefault("name") ?? "";
            if (string.IsNullOrWhiteSpace(address)) continue;

            result.Add(new NormalizedShelter
            {
                Address = address,
                Latitude = r.Latitude,
                Longitude = r.Longitude,
                Details = mapper.Apply(r, mapping),
                SourceUrl = source.Url
            });
        }
        return result;
    }

    // Distribute records into hromada folders by simple address-substring matching.
    // Records that don't match any go under "_unassigned".
    private static Dictionary<string, List<NormalizedShelter>> GroupByHromada(
        List<NormalizedShelter> records, List<string> hromadaFolderNames)
    {
        var groups = new Dictionary<string, List<NormalizedShelter>>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in records)
        {
            var addrLower = r.Address.ToLowerInvariant();
            string? matched = null;
            foreach (var name in hromadaFolderNames)
            {
                if (addrLower.Contains(name.ToLowerInvariant()))
                {
                    matched = name;
                    break;
                }
            }
            var key = matched ?? "_unassigned";
            if (!groups.ContainsKey(key)) groups[key] = [];
            groups[key].Add(r);
        }
        return groups;
    }

    // Merge all hromada output.json files into oblast-level output.json.
    public void MergeOblastOutput(string oblast)
    {
        var allRecords = new List<NormalizedShelter>();
        var allSources = new List<SourceUsed>();

        foreach (var hromada in paths.ListHromadas(oblast))
        {
            // _unassigned is included in the merge — those records still need to ship.
            // The folder name just signals "I didn't have a hromada to put these under".
            var doc = JsonIO.ReadIfExists<OutputDocument>(paths.HromadaOutputPath(oblast, hromada));
            if (doc == null) continue;
            allRecords.AddRange(doc.Records);
            allSources.AddRange(doc.Meta.SourcesUsed);
        }

        var merged = new OutputDocument
        {
            Meta = new OutputMeta
            {
                Oblast = oblast,
                GeneratedAt = DateTime.UtcNow,
                SourcesUsed = allSources
            },
            Records = allRecords
        };
        JsonIO.Write(paths.OblastOutputPath(oblast), merged);
        logger.LogInformation("[{Oblast}] Merged oblast output: {Count} records → {Path}",
            oblast, allRecords.Count, paths.OblastOutputPath(oblast));
    }
}
