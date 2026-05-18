using SourceRegistryService.Domain.Entities;

namespace SourceRegistryService.Application.Interfaces;

public interface IDiscoveryRunRepository
{
    Task<IEnumerable<DiscoveryRun>> GetByHromadaIdAsync(Guid hromadaId);
    Task<DiscoveryRun> AddAsync(DiscoveryRun run);
    Task SaveChangesAsync(CancellationToken ct = default);
}
