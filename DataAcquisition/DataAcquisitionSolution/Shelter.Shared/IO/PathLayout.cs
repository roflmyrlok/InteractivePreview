namespace Shelter.Shared.IO;

// Resolves all the conventional paths under DataAcquisition/.
public class PathLayout(string repoRoot)
{
    public string RepoRoot => repoRoot;
    public string SchemaPath => Path.Combine(repoRoot, "Schema", "shelter-schema.json");
    public string MappingsCacheDir => Path.Combine(repoRoot, ".mapping-cache");

    public string OblastDir(string oblast) =>
        Path.Combine(repoRoot, "Oblasts", oblast);

    public string OblastSourcesPath(string oblast) =>
        Path.Combine(OblastDir(oblast), "oblast-sources.json");

    public string OblastOutputPath(string oblast) =>
        Path.Combine(OblastDir(oblast), "output.json");

    public string HromadasDir(string oblast) =>
        Path.Combine(OblastDir(oblast), "hromadas");

    public string HromadaDir(string oblast, string hromada) =>
        Path.Combine(HromadasDir(oblast), hromada);

    public string HromadaSourcesPath(string oblast, string hromada) =>
        Path.Combine(HromadaDir(oblast, hromada), "sources.json");

    public string HromadaOutputPath(string oblast, string hromada) =>
        Path.Combine(HromadaDir(oblast, hromada), "output.json");

    public IEnumerable<string> ListHromadas(string oblast)
    {
        var dir = HromadasDir(oblast);
        if (!Directory.Exists(dir)) return [];
        return Directory.GetDirectories(dir).Select(Path.GetFileName)!;
    }
}
