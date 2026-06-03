using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SourceRegistryService.Application.DTOs;
using SourceRegistryService.Application.Interfaces;
using SourceRegistryService.Domain.Entities;
using SourceRegistryService.Domain.Enums;

namespace SourceRegistryService.API.Services;

// Calls Claude Sonnet with the web_search_20250305 built-in tool to discover
// official .gov.ua URLs publishing civil shelter data for a given hromada.
// Uses direct Anthropic REST API (not SDK) to support the built-in tool type.
public class DiscoveryService : IDiscoveryService
{
    private const string AnthropicApiUrl = "https://api.anthropic.com/v1/messages";
    private const string DefaultModelId = "claude-sonnet-4-6";
    private const string AnthropicVersion = "2023-06-01";

    private readonly HttpClient _http;
    private readonly IOblastRepository _oblasts;
    private readonly IHromadaRepository _hromadas;
    private readonly IVillageRepository _villages;
    private readonly IDataSourceRepository _sources;
    private readonly IDiscoveryRunRepository _runs;
    private readonly ICurrentUserContext _user;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DiscoveryService> _logger;

    public DiscoveryService(
        IHttpClientFactory httpClientFactory,
        IOblastRepository oblasts,
        IHromadaRepository hromadas,
        IVillageRepository villages,
        IDataSourceRepository sources,
        IDiscoveryRunRepository runs,
        ICurrentUserContext user,
        IConfiguration configuration,
        ILogger<DiscoveryService> logger)
    {
        _http = httpClientFactory.CreateClient("anthropic");
        _oblasts = oblasts;
        _hromadas = hromadas;
        _villages = villages;
        _sources = sources;
        _runs = runs;
        _user = user;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<DiscoveryRunDto> RunAsync(ScopeType scope, Guid scopeId, bool autoActivate, CancellationToken ct = default)
    {
        var target = await ResolveTargetAsync(scope, scopeId);

        var run = await _runs.AddAsync(new DiscoveryRun
        {
            Id = Guid.NewGuid(),
            ScopeType = scope,
            ScopeId = scopeId,
            StartedAt = DateTime.UtcNow,
            TriggeredByUserId = _user.UserId
        });

        try
        {
            var candidates = await CallClaudeAsync(target.Prompt, ct);
            _logger.LogInformation("Claude returned {Count} candidates for {Scope} {Name}",
                candidates.Count, scope, target.DisplayName);

            int inserted = 0;
            foreach (var candidate in candidates)
            {
                if (!IsValidGovUaUrl(candidate.Url)) continue;

                var alreadyExists = await _sources.ExistsByUrlAndScopeAsync(
                    candidate.Url, scope, scopeId);
                if (alreadyExists) continue;

                await _sources.AddAsync(new DataSource
                {
                    Id = Guid.NewGuid(),
                    ScopeType = scope,
                    ScopeId = scopeId,
                    Url = candidate.Url,
                    Description = $"{candidate.PageTitle} — {candidate.WhyRelevant} (confidence: {candidate.Confidence})",
                    Status = autoActivate ? DataSourceStatus.Active : DataSourceStatus.Pending,
                    Origin = DataSourceOrigin.AiDiscovered,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = _user.UserId
                });
                inserted++;
            }

            run.CompletedAt = DateTime.UtcNow;
            run.CandidatesFound = candidates.Count;
            run.CandidatesInserted = inserted;
            await _runs.SaveChangesAsync(ct);

            return ToDto(run);
        }
        catch (Exception ex)
        {
            run.CompletedAt = DateTime.UtcNow;
            run.Error = ex.Message;
            await _runs.SaveChangesAsync(ct);
            throw;
        }
    }

    private record DiscoveryTarget(string DisplayName, string Prompt);

    // Resolve the unit and build a tier-aware web-search prompt with the full parent chain.
    private async Task<DiscoveryTarget> ResolveTargetAsync(ScopeType scope, Guid scopeId)
    {
        switch (scope)
        {
            case ScopeType.Oblast:
            {
                var o = await _oblasts.GetByIdAsync(scopeId)
                    ?? throw new KeyNotFoundException($"Oblast {scopeId} not found");
                return new DiscoveryTarget(o.Name,
                    BuildPrompt($"{o.NameUk} ({o.Name})", "oblast (область)", "in Ukraine"));
            }
            case ScopeType.Hromada:
            {
                var h = await _hromadas.GetByIdAsync(scopeId)
                    ?? throw new KeyNotFoundException($"Hromada {scopeId} not found");
                var oblastName = h.Oblast?.Name ?? "Ukraine";
                return new DiscoveryTarget(h.Name,
                    BuildPrompt($"{h.NameUk} ({h.Name})", "hromada (territorial community)", $"in {oblastName}"));
            }
            case ScopeType.Village:
            {
                var v = await _villages.GetByIdAsync(scopeId)
                    ?? throw new KeyNotFoundException($"Village {scopeId} not found");
                var hromadaName = v.Hromada?.Name ?? "";
                var oblastName = v.Hromada?.Oblast?.Name ?? "Ukraine";
                var ctx = string.IsNullOrEmpty(hromadaName)
                    ? $"in {oblastName}"
                    : $"in {hromadaName} hromada, {oblastName}";
                return new DiscoveryTarget(v.Name,
                    BuildPrompt($"{v.NameUk} ({v.Name})", "village (село)", ctx));
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(scope), scope, "Unknown scope type");
        }
    }

    private static string BuildPrompt(string unit, string unitKind, string locationContext)
    {
        return "Search for official Ukrainian government websites (.gov.ua domains only) that publish\n"
            + $"civil shelter (укриття / захисні споруди) location data for the {unitKind} {unit}\n"
            + $"{locationContext}.\n\n"
            + "Look specifically for:\n"
            + "- Hromada/city council pages listing shelter locations\n"
            + "- Civil defense department pages with shelter maps\n"
            + "- Google My Maps or KML/KMZ files linked from official pages\n"
            + "- CSV or structured data files on .gov.ua domains\n\n"
            + "Return a JSON object with this exact shape (no markdown, no explanation):\n"
            + "{ \"candidates\": [ { \"url\": \"https://example.gov.ua/shelters\", \"page_title\": \"Title\", \"why_relevant\": \"One sentence\", \"confidence\": \"high|medium|low\" } ] }\n\n"
            + "Rules:\n"
            + "- ONLY include URLs on .gov.ua domains\n"
            + "- Maximum 5 candidates\n"
            + "- If no results found, return {\"candidates\":[]}\n"
            + "- Return ONLY the JSON object, nothing else";
    }

    private async Task<List<DiscoveryCandidate>> CallClaudeAsync(string prompt, CancellationToken ct)
    {
        var apiKey = _configuration["Anthropic:ApiKey"]
            ?? throw new InvalidOperationException("Anthropic:ApiKey not configured");
        var modelId = _configuration["Anthropic:Model"] ?? DefaultModelId;

        var requestBody = new
        {
            model = modelId,
            max_tokens = 2048,
            tools = new[]
            {
                new { type = "web_search_20250305", name = "web_search" }
            },
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        using var request = new HttpRequestMessage(HttpMethod.Post, AnthropicApiUrl)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", AnthropicVersion);
        request.Headers.Add("anthropic-beta", "web-search-2025-03-05");

        using var response = await _http.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Anthropic API error {Status}: {Body}", response.StatusCode, responseBody);
            throw new InvalidOperationException($"Anthropic API returned {response.StatusCode}");
        }

        return ParseCandidatesFromResponse(responseBody);
    }

    private List<DiscoveryCandidate> ParseCandidatesFromResponse(string responseBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            // Walk the content blocks to find the final text block
            var content = doc.RootElement.GetProperty("content");
            string? textBlock = null;

            foreach (var block in content.EnumerateArray())
            {
                if (block.TryGetProperty("type", out var typeEl) && typeEl.GetString() == "text"
                    && block.TryGetProperty("text", out var textEl))
                {
                    textBlock = textEl.GetString();
                }
            }

            if (string.IsNullOrWhiteSpace(textBlock))
            {
                _logger.LogWarning("No text block in Claude response");
                return [];
            }

            // Strip markdown fences if Claude added them
            if (textBlock.Contains("```"))
            {
                var start = textBlock.IndexOf('{');
                var end = textBlock.LastIndexOf('}');
                if (start >= 0 && end > start)
                    textBlock = textBlock[start..(end + 1)];
            }

            using var resultDoc = JsonDocument.Parse(textBlock);
            if (!resultDoc.RootElement.TryGetProperty("candidates", out var candidatesEl))
                return [];

            var candidates = new List<DiscoveryCandidate>();
            foreach (var c in candidatesEl.EnumerateArray())
            {
                var url = c.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "";
                if (string.IsNullOrWhiteSpace(url)) continue;

                candidates.Add(new DiscoveryCandidate
                {
                    Url = url,
                    PageTitle = c.TryGetProperty("page_title", out var pt) ? pt.GetString() ?? "" : "",
                    WhyRelevant = c.TryGetProperty("why_relevant", out var wr) ? wr.GetString() ?? "" : "",
                    Confidence = c.TryGetProperty("confidence", out var conf) ? conf.GetString() ?? "" : ""
                });
            }

            return candidates;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Claude response: {Body}", responseBody[..Math.Min(500, responseBody.Length)]);
            return [];
        }
    }

    private static bool IsValidGovUaUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        return (uri.Scheme == "https" || uri.Scheme == "http")
               && uri.Host.EndsWith(".gov.ua", StringComparison.OrdinalIgnoreCase);
    }

    private static DiscoveryRunDto ToDto(DiscoveryRun run) => new()
    {
        Id = run.Id,
        ScopeType = run.ScopeType,
        ScopeId = run.ScopeId,
        StartedAt = run.StartedAt,
        CompletedAt = run.CompletedAt,
        CandidatesFound = run.CandidatesFound,
        CandidatesInserted = run.CandidatesInserted,
        Error = run.Error
    };

    private class DiscoveryCandidate
    {
        public string Url { get; set; } = "";
        public string PageTitle { get; set; } = "";
        public string WhyRelevant { get; set; } = "";
        public string Confidence { get; set; } = "";
    }
}
