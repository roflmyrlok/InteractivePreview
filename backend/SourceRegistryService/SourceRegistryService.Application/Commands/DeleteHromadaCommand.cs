using MediatR;

namespace SourceRegistryService.Application.Commands;

public class DeleteHromadaCommand : IRequest<DeleteHromadaResult>
{
    public Guid Id { get; set; }
    public uint RowVersion { get; set; }
}

public enum DeleteHromadaOutcome { Deleted, NotFound, Conflict }
public class DeleteHromadaResult
{
    public DeleteHromadaOutcome Outcome { get; set; }
}
