namespace Shelter.Shared.Models;

public class Schema
{
    public string Version { get; set; } = "";
    public string Description { get; set; } = "";
    public List<SchemaField> SystemFields { get; set; } = [];
    public List<SchemaField> Fields { get; set; } = [];
}

public class SchemaField
{
    public string Name { get; set; } = "";
    public string Ua { get; set; } = "";
    public string Example { get; set; } = "";
    public string? Description { get; set; }
}
