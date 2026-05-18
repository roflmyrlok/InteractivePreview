using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SourceRegistryService.Domain.Entities;
using SourceRegistryService.Infrastructure.Data;

namespace SourceRegistryService.Infrastructure.SeedData;

public static class AdminSeeder
{
    public static async Task SeedAsync(SourceRegistryDbContext context, ILogger logger)
    {
        logger.LogInformation("Syncing Ukrainian administrative seed data...");

        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("ukraine-admin.json"));

        if (resourceName == null)
            throw new InvalidOperationException("Embedded resource ukraine-admin.json not found");

        await using var stream = assembly.GetManifestResourceStream(resourceName)!;
        var data = await JsonSerializer.DeserializeAsync<UkraineAdminData>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (data?.Oblasts == null) return;

        var oblastsInserted = 0;
        var hromadasInserted = 0;

        foreach (var oblastData in data.Oblasts)
        {
            var oblast = await context.Oblasts.IgnoreQueryFilters()
                .FirstOrDefaultAsync(o => o.Code == oblastData.Code);

            if (oblast == null)
            {
                oblast = new Oblast
                {
                    Id = Guid.NewGuid(),
                    Code = oblastData.Code,
                    CreatedAt = DateTime.UtcNow
                };
                context.Oblasts.Add(oblast);
                oblastsInserted++;
            }

            oblast.Name = oblastData.Name;
            oblast.NameUk = oblastData.NameUk;
            oblast.IsOccupied = oblastData.IsOccupied;

            foreach (var h in oblastData.Hromadas ?? [])
            {
                var hromada = await context.Hromadas.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.OblastId == oblast.Id && x.Slug == h.Slug);

                if (hromada == null)
                {
                    context.Hromadas.Add(new Hromada
                    {
                        Id = Guid.NewGuid(),
                        OblastId = oblast.Id,
                        Slug = h.Slug,
                        CreatedAt = DateTime.UtcNow,
                        Name = h.Name,
                        NameUk = h.NameUk
                    });
                    hromadasInserted++;
                }
                else
                {
                    hromada.Name = h.Name;
                    hromada.NameUk = h.NameUk;
                }
            }
        }

        await context.SaveChangesAsync();
        logger.LogInformation(
            "Seed sync complete — oblasts: +{O}, hromadas: +{H}",
            oblastsInserted, hromadasInserted);
    }

    private class UkraineAdminData
    {
        public List<OblastSeedEntry> Oblasts { get; set; } = [];
    }

    private class OblastSeedEntry
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string NameUk { get; set; } = "";
        public bool IsOccupied { get; set; }
        public List<HromadaSeedEntry> Hromadas { get; set; } = [];
    }

    private class HromadaSeedEntry
    {
        public string Name { get; set; } = "";
        public string NameUk { get; set; } = "";
        public string Slug { get; set; } = "";
    }
}
