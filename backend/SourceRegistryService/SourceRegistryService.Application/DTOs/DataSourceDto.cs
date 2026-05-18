using SourceRegistryService.Domain.Enums;

namespace SourceRegistryService.Application.DTOs;

public class DataSourceDto
{
    public Guid Id { get; set; }
    public ScopeType ScopeType { get; set; }
    public Guid ScopeId { get; set; }
    public string Url { get; set; } = "";
    public string Description { get; set; } = "";
    public DataSourceStatus Status { get; set; }
    public DataSourceOrigin Origin { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public uint RowVersion { get; set; }
}

public class CreateDataSourceRequest
{
    public ScopeType ScopeType { get; set; }
    public Guid ScopeId { get; set; }
    public string Url { get; set; } = "";
    public string Description { get; set; } = "";
}

public class PatchDataSourceRequest
{
    public string? Description { get; set; }
    public uint RowVersion { get; set; }
}
