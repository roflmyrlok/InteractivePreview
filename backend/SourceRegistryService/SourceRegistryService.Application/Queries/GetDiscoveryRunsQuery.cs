using MediatR;
using SourceRegistryService.Application.DTOs;
using SourceRegistryService.Domain.Enums;

namespace SourceRegistryService.Application.Queries;

public class GetDiscoveryRunsQuery : IRequest<IEnumerable<DiscoveryRunDto>>
{
    public ScopeType ScopeType { get; set; }
    public Guid ScopeId { get; set; }
}
