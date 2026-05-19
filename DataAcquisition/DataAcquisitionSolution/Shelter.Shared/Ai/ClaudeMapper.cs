using System.Text;
using System.Text.Json;
using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using Microsoft.Extensions.Logging;
using Shelter.Shared.Models;

namespace Shelter.Shared.Ai;

// Asks Claude to map raw source field names → canonical schema PropertyNames.
// Mapping is cached per source URL for reproducibility and to avoid repeat API calls.
public class ClaudeMapper(
    AnthropicClient client,
    Schema schema,
    MappingCache cache,
    ILogger<ClaudeMapper> logger)
{
    private const string SystemPrompt =
        "You map source data fields to a canonical Ukrainian shelter schema. " +
        "Respond ONLY with valid JSON, no markdown.";

    public async Task<Dictionary<string, string?>> GetMappingAsync(
        string sourceUrl, IReadOnlyList<RawRecord> sample, CancellationToken ct)
    {
        var cached = cache.TryLoad(sourceUrl);
        if (cached != null)
        {
            logger.LogInformation("Mapping cache hit: {Url}", sourceUrl);
            return cached;
        }

        logger.LogInformation("Requesting AI mapping: {Url}", sourceUrl);

        var schemaText = BuildSchemaText();
        var sampleJson = JsonSerializer.Serialize(
            sample.Take(5).Select(r => r.Fields),
            new JsonSerializerOptions { WriteIndented = true });

        var jsonShape = """
            { "<source_field>": "<CanonicalName or null>", ... }
            """;

        var canonicalNames = string.Join(", ", schema.Fields.Select(f => f.Name));

        var prompt = $"""
            ## Canonical Schema
            {schemaText}

            ## Sample records from source
            {sampleJson}

            ## Task
            Return JSON with this shape:
            {jsonShape}

            Rules:
            - Map each source field name to one of: {canonicalNames}
            - Use null for fields with no canonical equivalent (IDs, timestamps, internal codes, raw HTML)
            - System fields (Oblast, City, DataSource) are NOT mapped here — orchestrator injects them
            - Output strict JSON, no markdown fences
            """;

        var request = new MessageParameters
        {
            // Hard-code the model id rather than using the SDK constant —
            // SDK 4.5.0 still points its Haiku constant at the retired
            // "claude-3-5-haiku-20241022". Update this string when newer
            // Haiku revisions ship.
            Model = "claude-haiku-4-5-20251001",
            MaxTokens = 1024,
            System = new List<SystemMessage> { new(SystemPrompt) },
            Messages =
            [
                new()
                {
                    Role = RoleType.User,
                    Content = new List<ContentBase> { new TextContent { Text = prompt } }
                }
            ]
        };

        var response = await client.Messages.GetClaudeMessageAsync(request);
        var rawJson = (response.Message?.ToString() ?? "").Trim();

        var mapping = ParseMapping(rawJson);
        cache.Save(sourceUrl, mapping);

        var mapped = mapping.Count(kv => kv.Value != null);
        logger.LogInformation("AI mapped {Mapped}/{Total} fields for {Url}",
            mapped, mapping.Count, sourceUrl);

        return mapping;
    }

    public List<DetailDto> Apply(RawRecord record, Dictionary<string, string?> mapping)
    {
        var details = new List<DetailDto>();
        foreach (var (sourceField, canonical) in mapping)
        {
            if (canonical == null) continue;
            if (!record.Fields.TryGetValue(sourceField, out var value)) continue;
            if (string.IsNullOrWhiteSpace(value) || value == "-") continue;
            details.Add(new DetailDto(canonical, value));
        }
        return details;
    }

    private string BuildSchemaText()
    {
        var sb = new StringBuilder();
        foreach (var f in schema.Fields)
            sb.AppendLine($"- {f.Name} (ua: {f.Ua}, example: {f.Example}) — {f.Description}");
        return sb.ToString();
    }

    private Dictionary<string, string?> ParseMapping(string json)
    {
        try
        {
            // Strip code fences if model added them
            if (json.StartsWith("```"))
            {
                var start = json.IndexOf('\n') + 1;
                var end = json.LastIndexOf("```");
                if (end > start) json = json[start..end].Trim();
            }
            var doc = JsonDocument.Parse(json);
            var dict = new Dictionary<string, string?>();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.ValueKind == JsonValueKind.Null
                    ? null : prop.Value.GetString();
            }
            return dict;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse AI mapping. Raw: {Raw}", json);
            return [];
        }
    }
}
