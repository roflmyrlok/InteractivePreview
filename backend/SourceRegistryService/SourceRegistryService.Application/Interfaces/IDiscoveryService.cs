using SourceRegistryService.Application.DTOs;
using SourceRegistryService.Domain.Enums;

namespace SourceRegistryService.Application.Interfaces;

public interface IDiscoveryService
{
    // Discover .gov.ua shelter-data sources for any tier (oblast/hromada/village).
    // autoActivate inserts results as Active (first-fill) rather than Pending.
    Task<DiscoveryRunDto> RunAsync(ScopeType scope, Guid scopeId, bool autoActivate, CancellationToken ct = default);
}
