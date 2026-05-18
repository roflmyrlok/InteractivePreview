using MediatR;
using SourceRegistryService.Application.DTOs;

namespace SourceRegistryService.Application.Queries;

public class GetDiscoveryRunsQuery : IRequest<IEnumerable<DiscoveryRunDto>>
{
    public Guid HromadaId { get; set; }
}
