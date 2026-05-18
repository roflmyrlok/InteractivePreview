using MediatR;
using SourceRegistryService.Application.DTOs;
using SourceRegistryService.Domain.Enums;

namespace SourceRegistryService.Application.Queries;

public class GetSourcesQuery : IRequest<IEnumerable<DataSourceDto>>
{
    public Guid? HromadaId { get; set; }
    public DataSourceStatus? Status { get; set; }
}
