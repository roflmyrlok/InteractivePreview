using SourceRegistryService.Application.DTOs;

namespace SourceRegistryService.Application.Interfaces;

public interface IDiscoveryService
{
    Task<DiscoveryRunDto> RunAsync(Guid hromadaId, CancellationToken ct = default);
}
