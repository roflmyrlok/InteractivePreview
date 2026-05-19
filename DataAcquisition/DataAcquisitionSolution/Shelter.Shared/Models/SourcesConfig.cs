namespace Shelter.Shared.Models;

// oblast-sources.json
public class OblastSources
{
    public string Oblast { get; set; } = "";
    public List<SourceEntry> Sources { get; set; } = [];
}

// hromadas/{name}/sources.json
public class HromadaSources
{
    public string Hromada { get; set; } = "";
    public List<SourceEntry> Sources { get; set; } = [];
}

public class SourceEntry
{
    public string Url { get; set; } = "";
    public string? LocalPath { get; set; }
    public string Description { get; set; } = "";
    public DateTime? LastFetched { get; set; }
}
