using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Shelter.Shared.Models;

namespace Shelter.Shared.Dedup;

public class MergeResult
{
    public List<NormalizedShelter> AllRecords { get; set; } = [];
    public int Added { get; set; }
    public int DuplicatesMerged { get; set; }
    public int FieldsFilledByMerge { get; set; }
}

// Merges new records into existing ones.
// Match key: normalized address.
// On match: merge details — fill missing fields from incoming record.
// On conflict (both have a value, differ): keep existing (more-specific source wins
// because hromada-level processing runs after oblast-level).
public class Deduplicator(ILogger<Deduplicator> logger)
{
    public MergeResult Merge(IReadOnlyList<NormalizedShelter> existing, IReadOnlyList<NormalizedShelter> incoming)
    {
        var byKey = existing.ToDictionary(NormalizeAddress, r => r);
        var result = new MergeResult
        {
            AllRecords = [.. existing]
        };

        foreach (var newRecord in incoming)
        {
            var key = NormalizeAddress(newRecord);
            if (string.IsNullOrEmpty(key))
            {
                result.AllRecords.Add(newRecord);
                result.Added++;
                continue;
            }

            if (byKey.TryGetValue(key, out var matched))
            {
                var filled = MergeDetails(matched, newRecord);
                result.DuplicatesMerged++;
                result.FieldsFilledByMerge += filled;
            }
            else
            {
                byKey[key] = newRecord;
                result.AllRecords.Add(newRecord);
                result.Added++;
            }
        }

        logger.LogInformation(
            "Merge: existing={E}, incoming={I}, added={A}, deduped={D}, fieldsFilled={F}",
            existing.Count, incoming.Count, result.Added, result.DuplicatesMerged, result.FieldsFilledByMerge);

        return result;
    }

    // Mutates `target` in place. Returns count of fields newly filled from `source`.
    private static int MergeDetails(NormalizedShelter target, NormalizedShelter source)
    {
        int filled = 0;
        var existingNames = target.Details.Select(d => d.PropertyName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var d in source.Details)
        {
            // System fields (Oblast/City/DataSource) — keep both? No: target wins (it's the existing record).
            // For data fields, fill if target is missing or has the same value.
            if (existingNames.Contains(d.PropertyName)) continue;
            target.Details.Add(d);
            filled++;
        }

        // Coordinates: fill if missing
        if (!target.Latitude.HasValue && source.Latitude.HasValue) { target.Latitude = source.Latitude; filled++; }
        if (!target.Longitude.HasValue && source.Longitude.HasValue) { target.Longitude = source.Longitude; filled++; }

        // Source URL: keep both if different (record was confirmed by multiple sources)
        if (!string.IsNullOrEmpty(source.SourceUrl)
            && !string.IsNullOrEmpty(target.SourceUrl)
            && !target.SourceUrl.Contains(source.SourceUrl))
        {
            target.SourceUrl = $"{target.SourceUrl}; {source.SourceUrl}";
        }

        return filled;
    }

    private static readonly Regex Punct = new(@"[\p{P}\p{S}]", RegexOptions.Compiled);
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

    private static string NormalizeAddress(NormalizedShelter r)
    {
        if (string.IsNullOrWhiteSpace(r.Address)) return "";
        var s = r.Address.Trim().ToLower(CultureInfo.GetCultureInfo("uk-UA"));
        s = Punct.Replace(s, " ");
        s = Whitespace.Replace(s, " ").Trim();

        // Remove very common prefixes that vary between sources
        foreach (var prefix in new[] { "вул.", "вул", "вулиця", "просп.", "просп", "проспект", "пров.", "пров", "провулок", "пл.", "пл", "площа" })
        {
            if (s.StartsWith(prefix + " ")) s = s[(prefix.Length + 1)..];
        }
        return s;
    }
}
