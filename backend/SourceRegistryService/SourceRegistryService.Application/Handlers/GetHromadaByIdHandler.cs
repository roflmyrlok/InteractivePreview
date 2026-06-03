using MediatR;
using SourceRegistryService.Application.DTOs;
using SourceRegistryService.Application.Interfaces;
using SourceRegistryService.Application.Queries;
using SourceRegistryService.Domain.Enums;

namespace SourceRegistryService.Application.Handlers;

public class GetHromadaByIdHandler : IRequestHandler<GetHromadaByIdQuery, HromadaDetailDto?>
{
    private readonly IHromadaRepository _hromadas;
    private readonly IVillageRepository _villages;
    private readonly IDataSourceRepository _sources;

    public GetHromadaByIdHandler(IHromadaRepository hromadas, IVillageRepository villages, IDataSourceRepository sources)
    {
        _hromadas = hromadas;
        _villages = villages;
        _sources = sources;
    }

    public async Task<HromadaDetailDto?> Handle(GetHromadaByIdQuery request, CancellationToken ct)
    {
        var hromada = await _hromadas.GetByIdAsync(request.Id);
        if (hromada == null) return null;

        var sources = await _sources.GetByScopeAsync(ScopeType.Hromada, hromada.Id);

        var villageDtos = new List<VillageSummaryDto>();
        foreach (var v in await _villages.GetByHromadaIdAsync(hromada.Id))
        {
            var active = await _sources.GetByScopeAsync(ScopeType.Village, v.Id, DataSourceStatus.Active);
            var pending = await _sources.GetByScopeAsync(ScopeType.Village, v.Id, DataSourceStatus.Pending);
            villageDtos.Add(new VillageSummaryDto
            {
                Id = v.Id,
                Name = v.Name,
                NameUk = v.NameUk,
                Slug = v.Slug,
                KatottgCode = v.KatottgCode,
                ActiveSourceCount = active.Count(),
                PendingSourceCount = pending.Count()
            });
        }

        return new HromadaDetailDto
        {
            Villages = villageDtos,
            Id = hromada.Id,
            OblastId = hromada.OblastId,
            OblastName = hromada.Oblast?.Name ?? "",
            Name = hromada.Name,
            NameUk = hromada.NameUk,
            Slug = hromada.Slug,
            RowVersion = hromada.RowVersion,
            Sources = sources.OrderByDescending(s => s.CreatedAt).Select(s => new DataSourceDto
            {
                Id = s.Id,
                ScopeType = s.ScopeType,
                ScopeId = s.ScopeId,
                Url = s.Url,
                Description = s.Description,
                Status = s.Status,
                Origin = s.Origin,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                RowVersion = s.RowVersion
            }).ToList()
        };
    }
}
