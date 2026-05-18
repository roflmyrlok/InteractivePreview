using MediatR;
using SourceRegistryService.Application.DTOs;
using SourceRegistryService.Application.Interfaces;
using SourceRegistryService.Application.Queries;
using SourceRegistryService.Domain.Entities;
using SourceRegistryService.Domain.Enums;

namespace SourceRegistryService.Application.Handlers;

public class GetSourcesHandler : IRequestHandler<GetSourcesQuery, IEnumerable<DataSourceDto>>
{
    private readonly IDataSourceRepository _sources;

    public GetSourcesHandler(IDataSourceRepository sources) => _sources = sources;

    public async Task<IEnumerable<DataSourceDto>> Handle(GetSourcesQuery request, CancellationToken ct)
    {
        IEnumerable<DataSource> rows;

        if (request.HromadaId.HasValue)
            rows = await _sources.GetByScopeAsync(ScopeType.Hromada, request.HromadaId.Value, request.Status);
        else
            rows = Enumerable.Empty<DataSource>();

        return rows.Select(s => new DataSourceDto
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
        });
    }
}
