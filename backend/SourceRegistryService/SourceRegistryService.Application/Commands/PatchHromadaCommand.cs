using MediatR;

namespace SourceRegistryService.Application.Commands;

public class PatchHromadaCommand : IRequest<PatchHromadaResult>
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? NameUk { get; set; }
    public string? Slug { get; set; }
    public uint RowVersion { get; set; }
}

public enum PatchHromadaOutcome { Updated, NotFound, Conflict }
public class PatchHromadaResult
{
    public PatchHromadaOutcome Outcome { get; set; }
    public uint NewRowVersion { get; set; }
}
