using MediatR;

namespace SourceRegistryService.Application.Commands;

public class RejectDataSourceCommand : IRequest<RejectDataSourceResult>
{
    public Guid Id { get; set; }
    public uint RowVersion { get; set; }
}

public enum RejectDataSourceOutcome { Rejected, NotFound, Conflict }
public class RejectDataSourceResult
{
    public RejectDataSourceOutcome Outcome { get; set; }
    public uint NewRowVersion { get; set; }
}
