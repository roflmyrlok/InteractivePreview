using MediatR;
using SourceRegistryService.Application.DTOs;
using SourceRegistryService.Application.Interfaces;
using SourceRegistryService.Application.Queries;

namespace SourceRegistryService.Application.Handlers;

public class GetDiscoveryRunsHandler : IRequestHandler<GetDiscoveryRunsQuery, IEnumerable<DiscoveryRunDto>>
{
    private readonly IDiscoveryRunRepository _runs;

    public GetDiscoveryRunsHandler(IDiscoveryRunRepository runs) => _runs = runs;

    public async Task<IEnumerable<DiscoveryRunDto>> Handle(GetDiscoveryRunsQuery request, CancellationToken ct)
    {
        var runs = await _runs.GetByScopeAsync(request.ScopeType, request.ScopeId);
        return runs.OrderByDescending(r => r.StartedAt).Select(r => new DiscoveryRunDto
        {
            Id = r.Id,
            ScopeType = r.ScopeType,
            ScopeId = r.ScopeId,
            StartedAt = r.StartedAt,
            CompletedAt = r.CompletedAt,
            CandidatesFound = r.CandidatesFound,
            CandidatesInserted = r.CandidatesInserted,
            Error = r.Error
        });
    }
}
