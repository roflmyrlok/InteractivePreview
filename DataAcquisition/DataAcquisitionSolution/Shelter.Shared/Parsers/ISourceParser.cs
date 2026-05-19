using Shelter.Shared.Models;

namespace Shelter.Shared.Parsers;

public interface ISourceParser
{
    string Format { get; } // "kmz" | "kml" | "html" | "csv" | "esri-json"
    bool CanParse(string url, string contentType, byte[] firstBytes);
    Task<IReadOnlyList<RawRecord>> ParseAsync(Stream data, string sourceUrl);
}
