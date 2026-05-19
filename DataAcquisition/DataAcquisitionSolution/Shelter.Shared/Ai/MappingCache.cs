using System.Text.Json;

namespace Shelter.Shared.Ai;

// Field mapping is deterministic per source URL — cached so reruns don't re-call Claude.
public class MappingCache(string cacheDir)
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public Dictionary<string, string?>? TryLoad(string sourceUrl)
    {
        var path = PathFor(sourceUrl);
        if (!File.Exists(path)) return null;
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Dictionary<string, string?>>(json);
    }

    public void Save(string sourceUrl, Dictionary<string, string?> mapping)
    {
        Directory.CreateDirectory(cacheDir);
        File.WriteAllText(PathFor(sourceUrl), JsonSerializer.Serialize(mapping, JsonOpts));
    }

    public void Delete(string sourceUrl)
    {
        var path = PathFor(sourceUrl);
        if (File.Exists(path)) File.Delete(path);
    }

    private string PathFor(string sourceUrl)
    {
        // Hash the url for a stable filename
        var hash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(sourceUrl)))[..16].ToLowerInvariant();
        return Path.Combine(cacheDir, $"{hash}.json");
    }
}
