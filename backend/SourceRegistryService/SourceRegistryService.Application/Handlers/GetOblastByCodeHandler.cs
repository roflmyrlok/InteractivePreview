using MediatR;
using SourceRegistryService.Application.DTOs;
using SourceRegistryService.Application.Interfaces;
using SourceRegistryService.Application.Queries;
using SourceRegistryService.Domain.Enums;

namespace SourceRegistryService.Application.Handlers;

public class GetOblastByCodeHandler : IRequestHandler<GetOblastByCodeQuery, OblastDetailDto?>
{
    private readonly IOblastRepository _oblasts;
    private readonly IDataSourceRepository _sources;

    public GetOblastByCodeHandler(IOblastRepository oblasts, IDataSourceRepository sources)
    {
        _oblasts = oblasts;
        _sources = sources;
    }

    public async Task<OblastDetailDto?> Handle(GetOblastByCodeQuery request, CancellationToken ct)
    {
        var oblast = await _oblasts.GetByCodeAsync(request.Code);
        if (oblast == null) return null;

        var hromadaDtos = new List<HromadaSummaryDto>();
        foreach (var h in oblast.Hromadas.OrderBy(x => x.Name))
        {
            var activeSources = await _sources.GetByScopeAsync(ScopeType.Hromada, h.Id, DataSourceStatus.Active);
            var pendingSources = await _sources.GetByScopeAsync(ScopeType.Hromada, h.Id, DataSourceStatus.Pending);
            hromadaDtos.Add(new HromadaSummaryDto
            {
                Id = h.Id,
                Name = h.Name,
                NameUk = h.NameUk,
                Slug = h.Slug,
                ActiveSourceCount = activeSources.Count(),
                PendingSourceCount = pendingSources.Count()
            });
        }

        return new OblastDetailDto
        {
            Id = oblast.Id,
            Code = oblast.Code,
            Name = oblast.Name,
            NameUk = oblast.NameUk,
            IsOccupied = oblast.IsOccupied,
            RowVersion = oblast.RowVersion,
            Hromadas = hromadaDtos
        };
    }
}
