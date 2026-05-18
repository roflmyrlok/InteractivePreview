using System.Text.Json;
using SourceRegistryService.Application.Interfaces;
using SourceRegistryService.Domain.Entities;
using SourceRegistryService.Domain.Enums;

namespace SourceRegistryService.API.Services;

// One-time importer: walks DataAcquisition/Oblasts/<O>/oblast-sources.json
// and hromadas/<h>/sources.json, posting each as origin=Manual, status=Active.
// Idempotent — skips URLs already present.
public class FilesystemImporter
{
    private readonly IOblastRepository _oblasts;
    private readonly IHromadaRepository _hromadas;
    private readonly IDataSourceRepository _sources;
    private readonly ILogger<FilesystemImporter> _logger;

    public FilesystemImporter(
        IOblastRepository oblasts,
        IHromadaRepository hromadas,
        IDataSourceRepository sources,
        ILogger<FilesystemImporter> logger)
    {
        _oblasts = oblasts;
        _hromadas = hromadas;
        _sources = sources;
        _logger = logger;
    }

    public async Task ImportAsync(string dataAcquisitionRoot, CancellationToken ct = default)
    {
        var oblastsDir = Path.Combine(dataAcquisitionRoot, "Oblasts");
        if (!Directory.Exists(oblastsDir))
        {
            _logger.LogError("Oblasts directory not found: {Path}", oblastsDir);
            return;
        }

        var imported = 0;
        var skipped = 0;

        foreach (var oblastDir in Directory.GetDirectories(oblastsDir))
        {
            var oblastCode = Path.GetFileName(oblastDir);

            // Import oblast-level sources
            var oblastSourcesPath = Path.Combine(oblastDir, "oblast-sources.json");
            if (File.Exists(oblastSourcesPath))
            {
                var (i, s) = await ImportOblastSourcesAsync(oblastSourcesPath, oblastCode, ct);
                imported += i;
                skipped += s;
            }

            // Import hromada sources
            var hromadasDir = Path.Combine(oblastDir, "hromadas");
            if (!Directory.Exists(hromadasDir)) continue;

            foreach (var hromadaDir in Directory.GetDirectories(hromadasDir))
            {
                var hromadaSlug = Path.GetFileName(hromadaDir);
                if (hromadaSlug.StartsWith("_")) continue;

                var sourcesPath = Path.Combine(hromadaDir, "sources.json");
                if (!File.Exists(sourcesPath)) continue;

                var (hi, hs) = await ImportHromadaSourcesAsync(sourcesPath, oblastCode, hromadaSlug, ct);
                imported += hi;
                skipped += hs;
            }
        }

        _logger.LogInformation("Import complete: {Imported} imported, {Skipped} skipped", imported, skipped);
    }

    private async Task<(int imported, int skipped)> ImportOblastSourcesAsync(
        string path, string oblastCode, CancellationToken ct)
    {
        var oblast = await _oblasts.GetByCodeAsync(oblastCode);
        if (oblast == null)
        {
            _logger.LogWarning("Oblast not found in registry: {Code} — skipping {Path}", oblastCode, path);
            return (0, 0);
        }

        var data = await ReadJsonAsync<OblastSourcesFile>(path);
        if (data?.Sources == null) return (0, 0);

        int imp = 0, skip = 0;
        foreach (var entry in data.Sources)
        {
            if (string.IsNullOrWhiteSpace(entry.Url)) { skip++; continue; }

            var exists = await _sources.ExistsByUrlAndScopeAsync(entry.Url, ScopeType.Oblast, oblast.Id);
            if (exists) { skip++; continue; }

            await _sources.AddAsync(new DataSource
            {
                Id = Guid.NewGuid(),
                ScopeType = ScopeType.Oblast,
                ScopeId = oblast.Id,
                Url = entry.Url,
                Description = entry.Description ?? "",
                Status = DataSourceStatus.Active,
                Origin = DataSourceOrigin.Manual,
                CreatedAt = DateTime.UtcNow
            });
            imp++;
        }

        return (imp, skip);
    }

    private async Task<(int imported, int skipped)> ImportHromadaSourcesAsync(
        string path, string oblastCode, string hromadaSlug, CancellationToken ct)
    {
        var oblast = await _oblasts.GetByCodeAsync(oblastCode);
        if (oblast == null)
        {
            _logger.LogWarning("Oblast not found: {Code}", oblastCode);
            return (0, 0);
        }

        var hromada = await _hromadas.GetBySlugAndOblastAsync(hromadaSlug, oblast.Id);
        if (hromada == null)
        {
            _logger.LogWarning("Hromada not found: {Slug} in {Oblast}", hromadaSlug, oblastCode);
            return (0, 0);
        }

        var data = await ReadJsonAsync<HromadaSourcesFile>(path);
        if (data?.Sources == null) return (0, 0);

        int imp = 0, skip = 0;
        foreach (var entry in data.Sources)
        {
            if (string.IsNullOrWhiteSpace(entry.Url)) { skip++; continue; }

            var exists = await _sources.ExistsByUrlAndScopeAsync(entry.Url, ScopeType.Hromada, hromada.Id);
            if (exists) { skip++; continue; }

            await _sources.AddAsync(new DataSource
            {
                Id = Guid.NewGuid(),
                ScopeType = ScopeType.Hromada,
                ScopeId = hromada.Id,
                Url = entry.Url,
                Description = entry.Description ?? "",
                Status = DataSourceStatus.Active,
                Origin = DataSourceOrigin.Manual,
                CreatedAt = DateTime.UtcNow
            });
            imp++;
        }

        return (imp, skip);
    }

    private static async Task<T?> ReadJsonAsync<T>(string path)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private class OblastSourcesFile
    {
        public string Oblast { get; set; } = "";
        public List<SourceEntry>? Sources { get; set; }
    }

    private class HromadaSourcesFile
    {
        public string Hromada { get; set; } = "";
        public List<SourceEntry>? Sources { get; set; }
    }

    private class SourceEntry
    {
        public string Url { get; set; } = "";
        public string? Description { get; set; }
    }
}
