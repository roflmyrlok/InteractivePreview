using System.Text.Json;
using Shelter.Shared.Models;

namespace Shelter.Shared.IO;

public static class JsonIO
{
    public static readonly JsonSerializerOptions ReadOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static readonly JsonSerializerOptions WriteOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static T Read<T>(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, ReadOpts)
            ?? throw new InvalidOperationException($"Failed to deserialize {path}");
    }

    public static T? ReadIfExists<T>(string path)
    {
        if (!File.Exists(path)) return default;
        return Read<T>(path);
    }

    public static void Write<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(value, WriteOpts));
    }
}

public class SchemaLoader
{
    public Schema Load(string repoRoot)
    {
        var path = Path.Combine(repoRoot, "Schema", "shelter-schema.json");
        return JsonIO.Read<Schema>(path);
    }
}
