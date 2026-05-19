namespace Shelter.Shared.Models;

// One shelter record after AI normalization. Stored in output.json and consumed by Ingestion.
public class NormalizedShelter
{
    public string Address { get; set; } = "";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public List<DetailDto> Details { get; set; } = [];
    public string SourceUrl { get; set; } = "";
}

public class DetailDto
{
    public string PropertyName { get; set; } = "";
    public string PropertyValue { get; set; } = "";

    public DetailDto() { }
    public DetailDto(string name, string value)
    {
        PropertyName = name;
        PropertyValue = value;
    }
}
