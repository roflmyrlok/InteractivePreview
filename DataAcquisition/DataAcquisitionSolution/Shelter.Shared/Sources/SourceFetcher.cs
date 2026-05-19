using Microsoft.Extensions.Logging;
using Shelter.Shared.Models;
using Shelter.Shared.Parsers;

namespace Shelter.Shared.Sources;

// Fetches a URL or local file, detects the format, picks the right parser,
// and returns RawRecords. If the URL is an HTML page that links to a Google
// My Maps KMZ, it follows that link and parses the KMZ instead.
public class SourceFetcher(
    HttpClient http,
    IEnumerable<ISourceParser> parsers,
    ILogger<SourceFetcher> logger)
{
    public async Task<IReadOnlyList<RawRecord>> FetchAsync(SourceEntry source, string baseDir, CancellationToken ct)
    {
        var (bytes, contentType, finalUrl) = await LoadBytesAsync(source, baseDir, ct);

        // If we got an HTML page, check if it links to a Google My Maps KMZ first
        if (LooksLikeHtml(bytes, contentType))
        {
            var kmzUrl = GoogleMyMapsLinkExtractor.FindKmzUrl(System.Text.Encoding.UTF8.GetString(bytes));
            if (kmzUrl != null)
            {
                logger.LogInformation("Page links to Google My Maps KMZ: {Url}", kmzUrl);
                var kmzBytes = await http.GetByteArrayAsync(kmzUrl, ct);
                bytes = kmzBytes;
                contentType = "application/vnd.google-earth.kmz";
                finalUrl = kmzUrl;
            }
        }

        var firstBytes = bytes.Take(512).ToArray();
        var parser = parsers.FirstOrDefault(p => p.CanParse(finalUrl, contentType, firstBytes));
        if (parser == null)
        {
            logger.LogWarning("No parser matched url={Url} contentType={CT}", finalUrl, contentType);
            return [];
        }

        logger.LogInformation("Using parser: {Format} for {Url}", parser.Format, finalUrl);
        using var stream = new MemoryStream(bytes);
        return await parser.ParseAsync(stream, finalUrl);
    }

    private async Task<(byte[] bytes, string contentType, string url)> LoadBytesAsync(
        SourceEntry source, string baseDir, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(source.LocalPath))
        {
            var fullPath = Path.IsPathRooted(source.LocalPath)
                ? source.LocalPath
                : Path.GetFullPath(Path.Combine(baseDir, source.LocalPath));

            var bytes = await File.ReadAllBytesAsync(fullPath, ct);
            var contentType = GuessContentType(fullPath);
            return (bytes, contentType, fullPath);
        }

        if (string.IsNullOrEmpty(source.Url))
            throw new InvalidOperationException("SourceEntry has neither Url nor LocalPath");

        using var response = await http.GetAsync(source.Url, ct);
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadAsByteArrayAsync(ct);
        var ct_ = response.Content.Headers.ContentType?.MediaType ?? "";
        return (data, ct_, source.Url);
    }

    private static bool LooksLikeHtml(byte[] bytes, string contentType)
    {
        if (contentType.Contains("html", StringComparison.OrdinalIgnoreCase)) return true;
        var head = System.Text.Encoding.UTF8.GetString(bytes.Take(512).ToArray());
        return head.Contains("<html", StringComparison.OrdinalIgnoreCase)
            || head.Contains("<!doctype", StringComparison.OrdinalIgnoreCase);
    }

    private static string GuessContentType(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".kmz" => "application/vnd.google-earth.kmz",
            ".kml" => "application/vnd.google-earth.kml+xml",
            ".csv" => "text/csv",
            ".json" => "application/json",
            ".html" or ".htm" => "text/html",
            _ => "application/octet-stream"
        };
    }
}
