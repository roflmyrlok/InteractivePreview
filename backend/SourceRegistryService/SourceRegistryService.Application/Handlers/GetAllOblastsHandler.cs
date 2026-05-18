using MediatR;
using SourceRegistryService.Application.DTOs;
using SourceRegistryService.Application.Interfaces;
using SourceRegistryService.Application.Queries;
using SourceRegistryService.Domain.Enums;

namespace SourceRegistryService.Application.Handlers;

public class GetAllOblastsHandler : IRequestHandler<GetAllOblastsQuery, IEnumerable<OblastSummaryDto>>
{
    private readonly IOblastRepository _oblasts;
    private readonly IDataSourceRepository _sources;

    public GetAllOblastsHandler(IOblastRepository oblasts, IDataSourceRepository sources)
    {
        _oblasts = oblasts;
        _sources = sources;
    }

    public async Task<IEnumerable<OblastSummaryDto>> Handle(GetAllOblastsQuery request, CancellationToken ct)
    {
        var oblasts = await _oblasts.GetAllAsync();
        var result = new List<OblastSummaryDto>();

        foreach (var o in oblasts.OrderBy(x => x.Name))
        {
            var hromadasInOblast = o.Hromadas.Count;
            var activeSources = 0;
            foreach (var h in o.Hromadas)
            {
                var sources = await _sources.GetByScopeAsync(ScopeType.Hromada, h.Id, DataSourceStatus.Active);
                activeSources += sources.Count();
            }

            result.Add(new OblastSummaryDto
            {
                Id = o.Id,
                Code = o.Code,
                Name = o.Name,
                NameUk = o.NameUk,
                IsOccupied = o.IsOccupied,
                HromadaCount = hromadasInOblast,
                ActiveSourceCount = activeSources
            });
        }

        return result;
    }
}
