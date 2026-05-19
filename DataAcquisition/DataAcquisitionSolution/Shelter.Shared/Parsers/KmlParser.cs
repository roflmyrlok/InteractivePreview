using SharpKml.Dom;
using SharpKml.Engine;
using Shelter.Shared.Models;

namespace Shelter.Shared.Parsers;

public class KmlParser : ISourceParser
{
    public string Format => "kml";

    public bool CanParse(string url, string contentType, byte[] firstBytes)
    {
        if (url.EndsWith(".kml", StringComparison.OrdinalIgnoreCase)) return true;
        if (contentType.Contains("kml", StringComparison.OrdinalIgnoreCase)) return true;
        var head = System.Text.Encoding.UTF8.GetString(firstBytes);
        return head.Contains("<kml", StringComparison.OrdinalIgnoreCase);
    }

    public Task<IReadOnlyList<RawRecord>> ParseAsync(Stream data, string sourceUrl) =>
        Task.FromResult(ParseKmlStream(data));

    public static IReadOnlyList<RawRecord> ParseKmlStream(Stream kmlStream)
    {
        var kmlFile = KmlFile.Load(kmlStream);
        var records = new List<RawRecord>();

        if (kmlFile.Root is not Kml kml) return records;

        foreach (var placemark in kml.Flatten().OfType<Placemark>())
        {
            var record = new RawRecord();
            var name = placemark.Name?.Trim();
            var explicitAddress = placemark.Address?.Trim();
            var description = placemark.Description?.Text?.Trim();

            // Capture all three as fields so AI mapping has visibility.
            if (!string.IsNullOrWhiteSpace(name))        record.Fields["name"] = name;
            if (!string.IsNullOrWhiteSpace(explicitAddress)) record.Fields["address"] = explicitAddress;
            if (!string.IsNullOrWhiteSpace(description)) record.Fields["description"] = description;

            // ExtendedData fields are typically the most reliable.
            if (placemark.ExtendedData != null)
                foreach (var item in placemark.ExtendedData.Data)
                    if (!string.IsNullOrWhiteSpace(item.Value))
                        record.Fields[item.Name] = item.Value;

            // Address resolution priority:
            // 1. Cyrillic ExtendedData address fields ("Адреса"/"Адрес")
            // 2. Explicit <address> element
            // 3. Placemark <name> (Google My Maps convention — name IS the address)
            // 4. <description> as last resort (often free-form text, not address)
            record.Address =
                FindFirstNonEmpty(record.Fields, "Адреса", "Адрес", "адреса", "адрес")
                ?? explicitAddress
                ?? name
                ?? description;

            if (placemark.Geometry is Point point)
            {
                record.Latitude = point.Coordinate.Latitude;
                record.Longitude = point.Coordinate.Longitude;
            }

            if (!string.IsNullOrWhiteSpace(record.Address) || record.Latitude.HasValue)
                records.Add(record);
        }

        return records;
    }

    private static string? FindFirstNonEmpty(Dictionary<string, string> fields, params string[] keys)
    {
        foreach (var k in keys)
            if (fields.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v))
                return v;
        return null;
    }
}
