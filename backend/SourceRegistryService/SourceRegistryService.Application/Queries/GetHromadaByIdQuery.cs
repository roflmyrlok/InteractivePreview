using MediatR;
using SourceRegistryService.Application.DTOs;

namespace SourceRegistryService.Application.Queries;

public class GetHromadaByIdQuery : IRequest<HromadaDetailDto?>
{
    public Guid Id { get; set; }
}
