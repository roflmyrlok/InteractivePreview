using MediatR;
using SourceRegistryService.Application.DTOs;

namespace SourceRegistryService.Application.Commands;

public class RunDiscoveryCommand : IRequest<RunDiscoveryResult>
{
    public Guid HromadaId { get; set; }
}

public class RunDiscoveryResult
{
    public bool Success { get; set; }
    public DiscoveryRunDto? Run { get; set; }
    public string? Error { get; set; }
}
