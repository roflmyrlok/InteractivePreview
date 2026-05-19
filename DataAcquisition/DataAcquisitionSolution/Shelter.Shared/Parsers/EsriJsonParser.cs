using System.Globalization;
using System.Text.Json;
using Shelter.Shared.Models;

namespace Shelter.Shared.Parsers;

public class EsriJsonParser : ISourceParser
{
    public string Format => "esri-json";

    public bool CanParse(string url, string contentType, byte[] firstBytes)
    {
        var head = System.Text.Encoding.UTF8.GetString(firstBytes);
        return (head.Contains("\"features\"") && head.Contains("\"attributes\""))
            || head.Contains("esriGeometryPoint");
    }

    public async Task<IReadOnlyList<RawRecord>> ParseAsync(Stream data, string sourceUrl)
    {
        var doc = await JsonDocument.ParseAsync(data);
        var records = new List<RawRecord>();

        if (!doc.RootElement.TryGetProperty("features", out var features)) return records;

        foreach (var feature in features.EnumerateArray())
        {
            var record = new RawRecord();
            if (feature.TryGetProperty("attributes", out var attrs))
            {
                foreach (var attr in attrs.EnumerateObject())
                {
                    var value = attr.Value.ValueKind switch
                    {
                        JsonValueKind.String => attr.Value.GetString() ?? "",
                        JsonValueKind.Number => attr.Value.ToString(),
                        JsonValueKind.True => "true",
                        JsonValueKind.False => "false",
                        _ => ""
                    };
                    if (!string.IsNullOrWhiteSpace(value))
                        record.Fields[attr.Name] = value;
                }
            }

            // Common Esri lat/lon attribute names
            foreach (var (latKey, lonKey) in new[] { ("lat", "long"), ("lat", "lon"), ("latitude", "longitude") })
            {
                if (record.Fields.TryGetValue(latKey, out var lat) &&
                    record.Fields.TryGetValue(lonKey, out var lon) &&
                    double.TryParse(lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var dLat) &&
                    double.TryParse(lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var dLon))
                {
                    record.Latitude = dLat;
                    record.Longitude = dLon;
                    break;
                }
            }

            if (record.Fields.Count > 0) records.Add(record);
        }
        return records;
    }
}
