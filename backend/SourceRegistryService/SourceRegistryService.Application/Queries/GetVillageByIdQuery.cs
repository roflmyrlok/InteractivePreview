using MediatR;
using SourceRegistryService.Application.DTOs;

namespace SourceRegistryService.Application.Queries;

public class GetVillageByIdQuery : IRequest<VillageDetailDto?>
{
    public Guid Id { get; set; }
}
