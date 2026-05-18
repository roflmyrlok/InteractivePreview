using MediatR;
using SourceRegistryService.Application.Commands;
using SourceRegistryService.Application.Interfaces;

namespace SourceRegistryService.Application.Handlers;

public class DeleteHromadaHandler : IRequestHandler<DeleteHromadaCommand, DeleteHromadaResult>
{
    private readonly IHromadaRepository _hromadas;
    private readonly ICurrentUserContext _user;

    public DeleteHromadaHandler(IHromadaRepository hromadas, ICurrentUserContext user)
    {
        _hromadas = hromadas;
        _user = user;
    }

    public async Task<DeleteHromadaResult> Handle(DeleteHromadaCommand request, CancellationToken ct)
    {
        var hromada = await _hromadas.GetByIdAsync(request.Id);
        if (hromada == null)
            return new DeleteHromadaResult { Outcome = DeleteHromadaOutcome.NotFound };

        if (hromada.RowVersion != request.RowVersion)
            return new DeleteHromadaResult { Outcome = DeleteHromadaOutcome.Conflict };

        hromada.IsDeleted = true;
        hromada.DeletedAt = DateTime.UtcNow;
        hromada.DeletedByUserId = _user.UserId;

        try
        {
            await _hromadas.SaveChangesAsync(ct);
            return new DeleteHromadaResult { Outcome = DeleteHromadaOutcome.Deleted };
        }
        catch (ConcurrencyConflictException)
        {
            return new DeleteHromadaResult { Outcome = DeleteHromadaOutcome.Conflict };
        }
    }
}
