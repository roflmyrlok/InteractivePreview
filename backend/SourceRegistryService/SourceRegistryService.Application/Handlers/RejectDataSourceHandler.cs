using MediatR;
using SourceRegistryService.Application.Commands;
using SourceRegistryService.Application.Interfaces;
using SourceRegistryService.Domain.Enums;

namespace SourceRegistryService.Application.Handlers;

public class RejectDataSourceHandler : IRequestHandler<RejectDataSourceCommand, RejectDataSourceResult>
{
    private readonly IDataSourceRepository _sources;
    private readonly ICurrentUserContext _user;

    public RejectDataSourceHandler(IDataSourceRepository sources, ICurrentUserContext user)
    {
        _sources = sources;
        _user = user;
    }

    public async Task<RejectDataSourceResult> Handle(RejectDataSourceCommand request, CancellationToken ct)
    {
        var source = await _sources.GetByIdAsync(request.Id);
        if (source == null)
            return new RejectDataSourceResult { Outcome = RejectDataSourceOutcome.NotFound };

        if (source.RowVersion != request.RowVersion)
            return new RejectDataSourceResult { Outcome = RejectDataSourceOutcome.Conflict };

        source.Status = DataSourceStatus.Rejected;
        source.UpdatedAt = DateTime.UtcNow;
        source.UpdatedByUserId = _user.UserId;

        try
        {
            await _sources.SaveChangesAsync(ct);
            return new RejectDataSourceResult { Outcome = RejectDataSourceOutcome.Rejected, NewRowVersion = source.RowVersion };
        }
        catch (ConcurrencyConflictException)
        {
            return new RejectDataSourceResult { Outcome = RejectDataSourceOutcome.Conflict };
        }
    }
}
