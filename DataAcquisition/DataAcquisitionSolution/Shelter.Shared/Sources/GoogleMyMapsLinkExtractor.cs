using System.Text.RegularExpressions;

namespace Shelter.Shared.Sources;

// Many Ukrainian municipal pages embed or link to Google My Maps.
// A Google My Maps map can be exported as KMZ via the URL pattern:
//   https://www.google.com/maps/d/kml?mid=<MAP_ID>
// This helper finds the map id (mid) on a page and returns the KMZ url.
public static class GoogleMyMapsLinkExtractor
{
    private static readonly Regex MidRegex = new(
        @"google\.com/maps/d/(?:edit|viewer|view)?\??[^""'\s<>]*?mid=([\w-]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DirectKmzRegex = new(
        @"https?://[^""'\s<>]+\.kmz",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string? FindKmzUrl(string html)
    {
        // Direct .kmz link wins
        var direct = DirectKmzRegex.Match(html);
        if (direct.Success) return direct.Value;

        // Otherwise extract Google My Maps mid → KMZ export endpoint
        var mid = MidRegex.Match(html);
        if (mid.Success)
            return $"https://www.google.com/maps/d/kml?mid={mid.Groups[1].Value}";

        return null;
    }
}
