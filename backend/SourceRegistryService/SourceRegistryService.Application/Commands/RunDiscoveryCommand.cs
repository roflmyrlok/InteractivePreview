using MediatR;
using SourceRegistryService.Application.DTOs;
using SourceRegistryService.Domain.Enums;

namespace SourceRegistryService.Application.Commands;

public class RunDiscoveryCommand : IRequest<RunDiscoveryResult>
{
    public ScopeType ScopeType { get; set; }
    public Guid ScopeId { get; set; }
    // When true (first-fill automation), discovered sources are inserted as Active
    // instead of Pending, so the pipeline can use them with no human approval.
    public bool AutoActivate { get; set; }
}

public class RunDiscoveryResult
{
    public bool Success { get; set; }
    public DiscoveryRunDto? Run { get; set; }
    public string? Error { get; set; }
}
