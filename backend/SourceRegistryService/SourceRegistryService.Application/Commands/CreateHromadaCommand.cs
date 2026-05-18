using MediatR;

namespace SourceRegistryService.Application.Commands;

public class CreateHromadaCommand : IRequest<Guid>
{
    public Guid OblastId { get; set; }
    public string Name { get; set; } = "";
    public string NameUk { get; set; } = "";
    public string Slug { get; set; } = "";
}
