using System.IO.Compression;
using SharpKml.Dom;
using SharpKml.Engine;
using Shelter.Shared.Models;

namespace Shelter.Shared.Parsers;

public class KmzParser : ISourceParser
{
    public string Format => "kmz";

    public bool CanParse(string url, string contentType, byte[] firstBytes)
    {
        if (url.EndsWith(".kmz", StringComparison.OrdinalIgnoreCase)) return true;
        if (contentType.Contains("kmz", StringComparison.OrdinalIgnoreCase)) return true;
        // ZIP magic number — KMZ files are ZIP archives
        return firstBytes.Length >= 4 && firstBytes[0] == 0x50 && firstBytes[1] == 0x4B;
    }

    public Task<IReadOnlyList<RawRecord>> ParseAsync(Stream data, string sourceUrl)
    {
        using var zip = new ZipArchive(data, ZipArchiveMode.Read);
        var kmlEntry = zip.Entries.FirstOrDefault(e =>
            e.Name.EndsWith(".kml", StringComparison.OrdinalIgnoreCase));

        if (kmlEntry == null)
            return Task.FromResult<IReadOnlyList<RawRecord>>([]);

        using var kmlStream = kmlEntry.Open();
        return Task.FromResult(KmlParser.ParseKmlStream(kmlStream));
    }
}
