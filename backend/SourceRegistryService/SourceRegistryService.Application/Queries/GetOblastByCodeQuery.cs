using MediatR;
using SourceRegistryService.Application.DTOs;

namespace SourceRegistryService.Application.Queries;

public class GetOblastByCodeQuery : IRequest<OblastDetailDto?>
{
    public string Code { get; set; } = "";
}
