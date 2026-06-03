using SourceRegistryService.Domain.Entities;

namespace SourceRegistryService.Application.Interfaces;

public interface IVillageRepository
{
    Task<IEnumerable<Village>> GetByHromadaIdAsync(Guid hromadaId);
    // Loads the village together with its Hromada and the Hromada's Oblast,
    // so callers (e.g. discovery) can build the full parent chain.
    Task<Village?> GetByIdAsync(Guid id);
    Task<Village?> GetBySlugAndHromadaAsync(string slug, Guid hromadaId);
    Task<Village> AddAsync(Village village);
    Task SaveChangesAsync(CancellationToken ct = default);
}
