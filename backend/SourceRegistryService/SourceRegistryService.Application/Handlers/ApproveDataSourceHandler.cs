using MediatR;
using SourceRegistryService.Application.Commands;
using SourceRegistryService.Application.Interfaces;
using SourceRegistryService.Domain.Enums;

namespace SourceRegistryService.Application.Handlers;

public class ApproveDataSourceHandler : IRequestHandler<ApproveDataSourceCommand, ApproveDataSourceResult>
{
    private readonly IDataSourceRepository _sources;
    private readonly ICurrentUserContext _user;

    public ApproveDataSourceHandler(IDataSourceRepository sources, ICurrentUserContext user)
    {
        _sources = sources;
        _user = user;
    }

    public async Task<ApproveDataSourceResult> Handle(ApproveDataSourceCommand request, CancellationToken ct)
    {
        var source = await _sources.GetByIdAsync(request.Id);
        if (source == null)
            return new ApproveDataSourceResult { Outcome = ApproveDataSourceOutcome.NotFound };

        if (source.Status == DataSourceStatus.Active)
            return new ApproveDataSourceResult { Outcome = ApproveDataSourceOutcome.AlreadyActive, NewRowVersion = source.RowVersion };

        if (source.RowVersion != request.RowVersion)
            return new ApproveDataSourceResult { Outcome = ApproveDataSourceOutcome.Conflict };

        source.Status = DataSourceStatus.Active;
        source.UpdatedAt = DateTime.UtcNow;
        source.UpdatedByUserId = _user.UserId;

        try
        {
            await _sources.SaveChangesAsync(ct);
            return new ApproveDataSourceResult { Outcome = ApproveDataSourceOutcome.Approved, NewRowVersion = source.RowVersion };
        }
        catch (ConcurrencyConflictException)
        {
            return new ApproveDataSourceResult { Outcome = ApproveDataSourceOutcome.Conflict };
        }
    }
}
