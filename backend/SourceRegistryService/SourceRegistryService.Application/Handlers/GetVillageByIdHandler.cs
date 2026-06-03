using MediatR;
using SourceRegistryService.Application.DTOs;
using SourceRegistryService.Application.Interfaces;
using SourceRegistryService.Application.Queries;
using SourceRegistryService.Domain.Enums;

namespace SourceRegistryService.Application.Handlers;

public class GetVillageByIdHandler : IRequestHandler<GetVillageByIdQuery, VillageDetailDto?>
{
    private readonly IVillageRepository _villages;
    private readonly IDataSourceRepository _sources;

    public GetVillageByIdHandler(IVillageRepository villages, IDataSourceRepository sources)
    {
        _villages = villages;
        _sources = sources;
    }

    public async Task<VillageDetailDto?> Handle(GetVillageByIdQuery request, CancellationToken ct)
    {
        var village = await _villages.GetByIdAsync(request.Id);
        if (village == null) return null;

        var sources = await _sources.GetByScopeAsync(ScopeType.Village, village.Id);

        return new VillageDetailDto
        {
            Id = village.Id,
            HromadaId = village.HromadaId,
            HromadaName = village.Hromada?.Name ?? "",
            OblastName = village.Hromada?.Oblast?.Name ?? "",
            Name = village.Name,
            NameUk = village.NameUk,
            Slug = village.Slug,
            KatottgCode = village.KatottgCode,
            RowVersion = village.RowVersion,
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
