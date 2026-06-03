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
            try
            {
                await ProcessSource(source, existing, level: "hromada", levelName: hromada, paths.HromadaDir(oblast, hromada), ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[{Oblast}/{Hromada}] Source failed, skipping: {Url}", oblast, hromada, source.Url);
            }
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

    // Process one village: fetch sources (registry or sources.json), AI-map, merge into the village output.json.
    public async Task<OutputDocument> RunVillageAsync(
        string oblast, string hromada, string village, bool dryRun, CancellationToken ct,
        IEnumerable<SourceEntry>? externalSources = null)
    {
        var outputPath = paths.VillageOutputPath(oblast, hromada, village);

        VillageSources sources;
        if (externalSources != null)
        {
            sources = new VillageSources { Village = village, Sources = externalSources.ToList() };
        }
        else
        {
            var srcPath = paths.VillageSourcesPath(oblast, hromada, village);
            if (!File.Exists(srcPath))
                throw new FileNotFoundException($"sources.json not found at {srcPath}");
            sources = JsonIO.Read<VillageSources>(srcPath);
        }
        var existing = JsonIO.ReadIfExists<OutputDocument>(outputPath) ?? new OutputDocument
        {
            Meta = new OutputMeta { Oblast = oblast, Hromada = hromada, Village = village }
        };

        logger.LogInformation("[{Oblast}/{Hromada}/{Village}] Sources: {Count}, existing records: {Records}",
            oblast, hromada, village, sources.Sources.Count, existing.Records.Count);

        foreach (var source in sources.Sources)
        {
            try
            {
                await ProcessSource(source, existing, level: "village", levelName: village,
                    paths.VillageDir(oblast, hromada, village), ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[{Oblast}/{Hromada}/{Village}] Source failed, skipping: {Url}",
                    oblast, hromada, village, source.Url);
            }
        }

        existing.Meta.GeneratedAt = DateTime.UtcNow;

        if (!dryRun)
        {
            JsonIO.Write(outputPath, existing);
            logger.LogInformation("[{Oblast}/{Hromada}/{Village}] Wrote {Path} ({Count} records)",
                oblast, hromada, village, outputPath, existing.Records.Count);
        }
        else
        {
            logger.LogInformation("[{Oblast}/{Hromada}/{Village}] DRY RUN — not writing {Path}",
                oblast, hromada, village, outputPath);
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
            try
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
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[{Oblast}] Oblast-level source failed, skipping: {Url}", oblast, source.Url);
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
        var village = output.Meta.Village;
        foreach (var r in incoming)
        {
            r.SourceUrl = source.Url;
            r.Details.Add(new DetailDto("Oblast", oblast));
            if (!string.IsNullOrEmpty(hromada))
                r.Details.Add(new DetailDto("City", hromada));
            if (!string.IsNullOrEmpty(village))
                r.Details.Add(new DetailDto("Village", village));
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

    // Merge all village output.json files under a hromada UP into the hromada output.json.
    // Uses the deduplicator so cross-level duplicates resolve (village-specific fields fill gaps
    // left by hromada/oblast-level records). Idempotent: re-running adds no duplicate records.
    public void MergeVillagesIntoHromada(string oblast, string hromada, bool dryRun)
    {
        var outputPath = paths.HromadaOutputPath(oblast, hromada);
        var doc = JsonIO.ReadIfExists<OutputDocument>(outputPath) ?? new OutputDocument
        {
            Meta = new OutputMeta { Oblast = oblast, Hromada = hromada }
        };

        var villageCount = 0;
        foreach (var village in paths.ListVillages(oblast, hromada))
        {
            if (village.StartsWith("_")) continue;
            var vdoc = JsonIO.ReadIfExists<OutputDocument>(paths.VillageOutputPath(oblast, hromada, village));
            if (vdoc == null) continue;
            var merged = dedup.Merge(doc.Records, vdoc.Records);
            doc.Records = merged.AllRecords;
            doc.Meta.SourcesUsed.AddRange(vdoc.Meta.SourcesUsed);
            villageCount++;
        }

        doc.Meta.GeneratedAt = DateTime.UtcNow;
        if (!dryRun) JsonIO.Write(outputPath, doc);
        logger.LogInformation("[{Oblast}/{Hromada}] Merged {V} villages → {Count} records",
            oblast, hromada, villageCount, doc.Records.Count);
    }

    // Merge all hromada output.json files UP into oblast-level output.json (dedup, not concat).
    public void MergeOblastOutput(string oblast)
    {
        var merged = new OutputDocument
        {
            Meta = new OutputMeta { Oblast = oblast, GeneratedAt = DateTime.UtcNow }
        };

        foreach (var hromada in paths.ListHromadas(oblast))
        {
            // _unassigned is included in the merge — those records still need to ship.
            // The folder name just signals "I didn't have a hromada to put these under".
            var doc = JsonIO.ReadIfExists<OutputDocument>(paths.HromadaOutputPath(oblast, hromada));
            if (doc == null) continue;
            var step = dedup.Merge(merged.Records, doc.Records);
            merged.Records = step.AllRecords;
            merged.Meta.SourcesUsed.AddRange(doc.Meta.SourcesUsed);
        }

        JsonIO.Write(paths.OblastOutputPath(oblast), merged);
        logger.LogInformation("[{Oblast}] Merged oblast output: {Count} records → {Path}",
            oblast, merged.Records.Count, paths.OblastOutputPath(oblast));
    }
}
