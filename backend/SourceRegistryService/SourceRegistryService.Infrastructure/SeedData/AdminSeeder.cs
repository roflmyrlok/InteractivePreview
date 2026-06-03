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
        var villagesInserted = 0;

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
            if (!string.IsNullOrEmpty(oblastData.KatottgCode))
                oblast.KatottgCode = oblastData.KatottgCode;

            foreach (var h in oblastData.Hromadas ?? [])
            {
                var hromada = await context.Hromadas.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.OblastId == oblast.Id && x.Slug == h.Slug);

                if (hromada == null)
                {
                    hromada = new Hromada
                    {
                        Id = Guid.NewGuid(),
                        OblastId = oblast.Id,
                        Slug = h.Slug,
                        CreatedAt = DateTime.UtcNow,
                        Name = h.Name,
                        NameUk = h.NameUk
                    };
                    context.Hromadas.Add(hromada);
                    hromadasInserted++;
                }
                else
                {
                    hromada.Name = h.Name;
                    hromada.NameUk = h.NameUk;
                }
                if (!string.IsNullOrEmpty(h.KatottgCode))
                    hromada.KatottgCode = h.KatottgCode;

                // Upsert villages: match by (HromadaId, Slug); Slug is unique within a hromada.
                foreach (var v in h.Villages ?? [])
                {
                    var village = await context.Villages.IgnoreQueryFilters()
                        .FirstOrDefaultAsync(x => x.HromadaId == hromada.Id && x.Slug == v.Slug);

                    if (village == null)
                    {
                        context.Villages.Add(new Village
                        {
                            Id = Guid.NewGuid(),
                            HromadaId = hromada.Id,
                            Slug = v.Slug,
                            CreatedAt = DateTime.UtcNow,
                            Name = v.Name,
                            NameUk = v.NameUk,
                            KatottgCode = v.KatottgCode
                        });
                        villagesInserted++;
                    }
                    else
                    {
                        village.Name = v.Name;
                        village.NameUk = v.NameUk;
                        if (!string.IsNullOrEmpty(v.KatottgCode))
                            village.KatottgCode = v.KatottgCode;
                    }
                }
            }
        }

        await context.SaveChangesAsync();
        logger.LogInformation(
            "Seed sync complete — oblasts: +{O}, hromadas: +{H}, villages: +{V}",
            oblastsInserted, hromadasInserted, villagesInserted);
    }

    private class UkraineAdminData
    {
        public List<OblastSeedEntry> Oblasts { get; set; } = [];
    }

    private class OblastSeedEntry
    {
        public string Code { get; set; } = "";
        public string KatottgCode { get; set; } = "";
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
        public string KatottgCode { get; set; } = "";
        public List<VillageSeedEntry> Villages { get; set; } = [];
    }

    private class VillageSeedEntry
    {
        public string Name { get; set; } = "";
        public string NameUk { get; set; } = "";
        public string Slug { get; set; } = "";
        public string KatottgCode { get; set; } = "";
    }
}
