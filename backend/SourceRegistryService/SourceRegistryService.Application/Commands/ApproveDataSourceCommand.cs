using MediatR;

namespace SourceRegistryService.Application.Commands;

public class ApproveDataSourceCommand : IRequest<ApproveDataSourceResult>
{
    public Guid Id { get; set; }
    public uint RowVersion { get; set; }
}

public enum ApproveDataSourceOutcome { Approved, NotFound, Conflict, AlreadyActive }
public class ApproveDataSourceResult
{
    public ApproveDataSourceOutcome Outcome { get; set; }
    public uint NewRowVersion { get; set; }
}
