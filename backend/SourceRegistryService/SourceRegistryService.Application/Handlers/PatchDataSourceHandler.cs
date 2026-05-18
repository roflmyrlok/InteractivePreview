using MediatR;
using SourceRegistryService.Application.Commands;
using SourceRegistryService.Application.Interfaces;

namespace SourceRegistryService.Application.Handlers;

public class PatchDataSourceHandler : IRequestHandler<PatchDataSourceCommand, PatchDataSourceResult>
{
    private readonly IDataSourceRepository _sources;
    private readonly ICurrentUserContext _user;

    public PatchDataSourceHandler(IDataSourceRepository sources, ICurrentUserContext user)
    {
        _sources = sources;
        _user = user;
    }

    public async Task<PatchDataSourceResult> Handle(PatchDataSourceCommand request, CancellationToken ct)
    {
        var source = await _sources.GetByIdAsync(request.Id);
        if (source == null)
            return new PatchDataSourceResult { Outcome = PatchDataSourceOutcome.NotFound };

        if (source.RowVersion != request.RowVersion)
            return new PatchDataSourceResult { Outcome = PatchDataSourceOutcome.Conflict };

        if (request.Description != null) source.Description = request.Description;
        source.UpdatedAt = DateTime.UtcNow;
        source.UpdatedByUserId = _user.UserId;

        try
        {
            await _sources.SaveChangesAsync(ct);
            return new PatchDataSourceResult { Outcome = PatchDataSourceOutcome.Updated, NewRowVersion = source.RowVersion };
        }
        catch (ConcurrencyConflictException)
        {
            return new PatchDataSourceResult { Outcome = PatchDataSourceOutcome.Conflict };
        }
    }
}
