using MediatR;
using SourceRegistryService.Application.DTOs;

namespace SourceRegistryService.Application.Queries;

public class GetAllOblastsQuery : IRequest<IEnumerable<OblastSummaryDto>> { }
