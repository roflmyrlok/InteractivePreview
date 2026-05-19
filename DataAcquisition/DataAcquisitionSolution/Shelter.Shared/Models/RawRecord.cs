namespace Shelter.Shared.Models;

// Output of parsers, before AI normalization
public class RawRecord
{
    public Dictionary<string, string> Fields { get; set; } = [];
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Address { get; set; }
}

public record GeoPoint(double Lat, double Lon);
