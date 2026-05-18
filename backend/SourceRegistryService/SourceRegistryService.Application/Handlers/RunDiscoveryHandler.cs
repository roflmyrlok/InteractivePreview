using MediatR;
using SourceRegistryService.Application.Commands;
using SourceRegistryService.Application.Interfaces;

namespace SourceRegistryService.Application.Handlers;

public class RunDiscoveryHandler : IRequestHandler<RunDiscoveryCommand, RunDiscoveryResult>
{
    private readonly IDiscoveryService _discovery;
    private readonly IHromadaRepository _hromadas;

    public RunDiscoveryHandler(IDiscoveryService discovery, IHromadaRepository hromadas)
    {
        _discovery = discovery;
        _hromadas = hromadas;
    }

    public async Task<RunDiscoveryResult> Handle(RunDiscoveryCommand request, CancellationToken ct)
    {
        var hromada = await _hromadas.GetByIdAsync(request.HromadaId);
        if (hromada == null)
            return new RunDiscoveryResult { Success = false, Error = "Hromada not found" };

        try
        {
            var run = await _discovery.RunAsync(request.HromadaId, ct);
            return new RunDiscoveryResult { Success = true, Run = run };
        }
        catch (Exception ex)
        {
            return new RunDiscoveryResult { Success = false, Error = ex.Message };
        }
    }
}
