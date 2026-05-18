using MediatR;

namespace SourceRegistryService.Application.Commands;

public class PatchDataSourceCommand : IRequest<PatchDataSourceResult>
{
    public Guid Id { get; set; }
    public string? Description { get; set; }
    public uint RowVersion { get; set; }
}

public enum PatchDataSourceOutcome { Updated, NotFound, Conflict }
public class PatchDataSourceResult
{
    public PatchDataSourceOutcome Outcome { get; set; }
    public uint NewRowVersion { get; set; }
}
