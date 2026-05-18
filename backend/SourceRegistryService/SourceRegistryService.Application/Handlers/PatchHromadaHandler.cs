using MediatR;
using SourceRegistryService.Application.Commands;
using SourceRegistryService.Application.Interfaces;

namespace SourceRegistryService.Application.Handlers;

public class PatchHromadaHandler : IRequestHandler<PatchHromadaCommand, PatchHromadaResult>
{
    private readonly IHromadaRepository _hromadas;
    private readonly ICurrentUserContext _user;

    public PatchHromadaHandler(IHromadaRepository hromadas, ICurrentUserContext user)
    {
        _hromadas = hromadas;
        _user = user;
    }

    public async Task<PatchHromadaResult> Handle(PatchHromadaCommand request, CancellationToken ct)
    {
        var hromada = await _hromadas.GetByIdAsync(request.Id);
        if (hromada == null)
            return new PatchHromadaResult { Outcome = PatchHromadaOutcome.NotFound };

        if (hromada.RowVersion != request.RowVersion)
            return new PatchHromadaResult { Outcome = PatchHromadaOutcome.Conflict };

        if (request.Name != null) hromada.Name = request.Name;
        if (request.NameUk != null) hromada.NameUk = request.NameUk;
        if (request.Slug != null) hromada.Slug = request.Slug;
        hromada.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _hromadas.SaveChangesAsync(ct);
            return new PatchHromadaResult { Outcome = PatchHromadaOutcome.Updated, NewRowVersion = hromada.RowVersion };
        }
        catch (ConcurrencyConflictException)
        {
            return new PatchHromadaResult { Outcome = PatchHromadaOutcome.Conflict };
        }
    }
}
