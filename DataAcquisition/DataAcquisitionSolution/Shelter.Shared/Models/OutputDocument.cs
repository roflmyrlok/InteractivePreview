namespace Shelter.Shared.Models;

// The reproducible artifact: one of these per hromada (and a merged one per oblast).
// Read by Shelter.Ingestion → POST to API. Updated in place by Shelter.Research.
public class OutputDocument
{
    public OutputMeta Meta { get; set; } = new();
    public List<NormalizedShelter> Records { get; set; } = [];
}

public class OutputMeta
{
    public string Oblast { get; set; } = "";
    public string? Hromada { get; set; }
    public string? Village { get; set; }
    public DateTime GeneratedAt { get; set; }
    public List<SourceUsed> SourcesUsed { get; set; } = [];
}

public class SourceUsed
{
    public string Level { get; set; } = "";          // "oblast" | "hromada" | "village"
    public string? Name { get; set; }                 // e.g. "Vyshneve"
    public string Url { get; set; } = "";
    public DateTime FetchedAt { get; set; }
    public int RecordsAdded { get; set; }
    public int DuplicatesMerged { get; set; }
    public int FieldsFilledByMerge { get; set; }
}
