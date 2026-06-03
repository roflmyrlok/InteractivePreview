using SourceRegistryService.Domain.Entities;
using SourceRegistryService.Domain.Enums;

namespace SourceRegistryService.Application.Interfaces;

public interface IDiscoveryRunRepository
{
    Task<IEnumerable<DiscoveryRun>> GetByScopeAsync(ScopeType scopeType, Guid scopeId);
    Task<DiscoveryRun> AddAsync(DiscoveryRun run);
    Task SaveChangesAsync(CancellationToken ct = default);
}
