using SourceRegistryService.Domain.Enums;

namespace SourceRegistryService.Domain.Entities;

public class DataSource
{
    public Guid Id { get; set; }
    public ScopeType ScopeType { get; set; }
    public Guid ScopeId { get; set; }
    public string Url { get; set; } = "";
    public string Description { get; set; } = "";
    public DataSourceStatus Status { get; set; } = DataSourceStatus.Pending;
    public DataSourceOrigin Origin { get; set; } = DataSourceOrigin.Manual;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public uint RowVersion { get; set; }
}
