using System.Net.Http.Headers;
using System.Net.Http.Json;
using Shelter.Shared.Models;

namespace Shelter.Shared.Sources;

// Fetches Active data sources from the SourceRegistryService HTTP API.
// In registry mode, --oblast is the oblast code (e.g. "UA32") and
// --hromada is the hromada slug (e.g. "brovary").
public class RegistryClient(HttpClient http)
{
    // The registry serializes enums as numbers (System.Text.Json default), so Status/Origin
    // arrive as ints: DataSourceStatus { Pending=0, Active=1, Rejected=2 }.
    private record DataSourceDto(Guid Id, string Url, string Description, int Status, int Origin);
    private const int ActiveStatus = 1;
    private record VillageSummaryDto(Guid Id, string Name, string NameUk, string Slug);
    private record VillageDetailDto(Guid Id, string Name, string NameUk, string Slug,
        List<DataSourceDto>? Sources);
    private record HromadaDto(Guid Id, string Name, string NameUk, string Slug, Guid OblastId,
        List<DataSourceDto>? Sources, List<VillageSummaryDto>? Villages);
    private record OblastDto(Guid Id, string Code, string Name, string NameUk,
        List<HromadaDto>? Hromadas, List<DataSourceDto>? Sources);

    private static bool IsActive(DataSourceDto s) => s.Status == ActiveStatus;

    // GET /api/hromadas/{id} → Active sources as SourceEntry list.
    public async Task<List<SourceEntry>> GetHromadaSourcesAsync(Guid hromadaId, CancellationToken ct = default)
    {
        var dto = await http.GetFromJsonAsync<HromadaDto>($"/api/hromadas/{hromadaId}", ct)
            ?? throw new InvalidOperationException($"Hromada {hromadaId} not found in registry");

        return (dto.Sources ?? [])
            .Where(IsActive)
            .Select(s => new SourceEntry { Url = s.Url, Description = s.Description })
            .ToList();
    }

    // GET /api/oblasts/{code} → Active oblast-scope sources as SourceEntry list.
    public async Task<List<SourceEntry>> GetOblastSourcesAsync(string oblastCode, CancellationToken ct = default)
    {
        var dto = await http.GetFromJsonAsync<OblastDto>($"/api/oblasts/{oblastCode}", ct)
            ?? throw new InvalidOperationException($"Oblast {oblastCode} not found in registry");

        return (dto.Sources ?? [])
            .Where(IsActive)
            .Select(s => new SourceEntry { Url = s.Url, Description = s.Description })
            .ToList();
    }

    // GET /api/villages/{id} → Active village-scope sources as SourceEntry list.
    public async Task<List<SourceEntry>> GetVillageSourcesAsync(Guid villageId, CancellationToken ct = default)
    {
        var dto = await http.GetFromJsonAsync<VillageDetailDto>($"/api/villages/{villageId}", ct)
            ?? throw new InvalidOperationException($"Village {villageId} not found in registry");

        return (dto.Sources ?? [])
            .Where(IsActive)
            .Select(s => new SourceEntry { Url = s.Url, Description = s.Description })
            .ToList();
    }

    // Resolve oblast code → list of (slug, id) pairs for iterating hromadas.
    public async Task<List<(string Slug, Guid Id)>> ListHromadaIdsAsync(string oblastCode, CancellationToken ct = default)
    {
        var dto = await http.GetFromJsonAsync<OblastDto>($"/api/oblasts/{oblastCode}", ct)
            ?? throw new InvalidOperationException($"Oblast {oblastCode} not found in registry");

        return (dto.Hromadas ?? [])
            .Select(h => (h.Slug, h.Id))
            .ToList();
    }

    // Resolve hromada id → list of (slug, id) pairs for iterating its villages.
    public async Task<List<(string Slug, Guid Id)>> ListVillageIdsAsync(Guid hromadaId, CancellationToken ct = default)
    {
        var dto = await http.GetFromJsonAsync<HromadaDto>($"/api/hromadas/{hromadaId}", ct)
            ?? throw new InvalidOperationException($"Hromada {hromadaId} not found in registry");

        return (dto.Villages ?? [])
            .Select(v => (v.Slug, v.Id))
            .ToList();
    }

    public async Task<Guid> GetOblastIdAsync(string oblastCode, CancellationToken ct = default)
    {
        var dto = await http.GetFromJsonAsync<OblastDto>($"/api/oblasts/{oblastCode}", ct)
            ?? throw new InvalidOperationException($"Oblast {oblastCode} not found in registry");
        return dto.Id;
    }

    // POST /api/discovery/{scope}/{id}?autoActivate=... — kick off AI source discovery for a node.
    // scope is "oblast" | "hromada" | "village". Returns false on any failure so a single
    // node's discovery error (e.g. the server's Anthropic key is rejected) does not abort the run.
    public async Task<bool> TriggerDiscoveryAsync(string scope, Guid id, bool autoActivate, CancellationToken ct = default)
    {
        var url = $"/api/discovery/{scope}/{id}?autoActivate={(autoActivate ? "true" : "false")}";
        // Retry with exponential backoff: discovery calls Anthropic web_search server-side,
        // which is easily rate-limited (429) when many nodes are processed in a burst.
        for (var attempt = 1; attempt <= MaxDiscoveryAttempts; attempt++)
        {
            try
            {
                using var resp = await http.PostAsync(url, null, ct);
                if (resp.IsSuccessStatusCode) return true;
            }
            catch (HttpRequestException) { /* fall through to backoff */ }

            if (attempt < MaxDiscoveryAttempts)
                await Task.Delay(TimeSpan.FromSeconds(BackoffBaseSeconds * attempt), ct);
        }
        return false;
    }

    private const int MaxDiscoveryAttempts = 4;
    private const int BackoffBaseSeconds = 8; // waits 8s, 16s, 24s between attempts

    // Resolve oblast code + hromada slug → hromada GUID.
    public async Task<Guid?> FindHromadaIdAsync(string oblastCode, string hromadaSlug, CancellationToken ct = default)
    {
        var hromadas = await ListHromadaIdsAsync(oblastCode, ct);
        var match = hromadas.FirstOrDefault(h =>
            string.Equals(h.Slug, hromadaSlug, StringComparison.OrdinalIgnoreCase));
        return match == default ? null : match.Id;
    }

    public static RegistryClient Create(string baseUrl, string token)
    {
        var http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return new RegistryClient(http);
    }
}
