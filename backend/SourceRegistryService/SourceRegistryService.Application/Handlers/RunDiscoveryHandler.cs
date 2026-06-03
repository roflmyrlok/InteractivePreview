using MediatR;
using SourceRegistryService.Application.Commands;
using SourceRegistryService.Application.Interfaces;
using SourceRegistryService.Domain.Enums;

namespace SourceRegistryService.Application.Handlers;

public class RunDiscoveryHandler : IRequestHandler<RunDiscoveryCommand, RunDiscoveryResult>
{
    private readonly IDiscoveryService _discovery;
    private readonly IOblastRepository _oblasts;
    private readonly IHromadaRepository _hromadas;
    private readonly IVillageRepository _villages;

    public RunDiscoveryHandler(
        IDiscoveryService discovery,
        IOblastRepository oblasts,
        IHromadaRepository hromadas,
        IVillageRepository villages)
    {
        _discovery = discovery;
        _oblasts = oblasts;
        _hromadas = hromadas;
        _villages = villages;
    }

    public async Task<RunDiscoveryResult> Handle(RunDiscoveryCommand request, CancellationToken ct)
    {
        var exists = request.ScopeType switch
        {
            ScopeType.Oblast => await _oblasts.GetByIdAsync(request.ScopeId) is not null,
            ScopeType.Hromada => await _hromadas.GetByIdAsync(request.ScopeId) is not null,
            ScopeType.Village => await _villages.GetByIdAsync(request.ScopeId) is not null,
            _ => false
        };
        if (!exists)
            return new RunDiscoveryResult { Success = false, Error = $"{request.ScopeType} {request.ScopeId} not found" };

        try
        {
            var run = await _discovery.RunAsync(request.ScopeType, request.ScopeId, request.AutoActivate, ct);
            return new RunDiscoveryResult { Success = true, Run = run };
        }
        catch (Exception ex)
        {
            return new RunDiscoveryResult { Success = false, Error = ex.Message };
        }
    }
}
