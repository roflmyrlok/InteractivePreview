using MediatR;
using SourceRegistryService.Application.DTOs;
using SourceRegistryService.Application.Interfaces;
using SourceRegistryService.Application.Queries;
using SourceRegistryService.Domain.Enums;

namespace SourceRegistryService.Application.Handlers;

public class GetHromadaByIdHandler : IRequestHandler<GetHromadaByIdQuery, HromadaDetailDto?>
{
    private readonly IHromadaRepository _hromadas;
    private readonly IDataSourceRepository _sources;

    public GetHromadaByIdHandler(IHromadaRepository hromadas, IDataSourceRepository sources)
    {
        _hromadas = hromadas;
        _sources = sources;
    }

    public async Task<HromadaDetailDto?> Handle(GetHromadaByIdQuery request, CancellationToken ct)
    {
        var hromada = await _hromadas.GetByIdAsync(request.Id);
        if (hromada == null) return null;

        var sources = await _sources.GetByScopeAsync(ScopeType.Hromada, hromada.Id);

        return new HromadaDetailDto
        {
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
