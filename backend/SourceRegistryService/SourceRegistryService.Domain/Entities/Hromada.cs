namespace SourceRegistryService.Domain.Entities;

public class Hromada
{
    public Guid Id { get; set; }
    public Guid OblastId { get; set; }
    public string Name { get; set; } = "";
    public string NameUk { get; set; } = "";
    public string Slug { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public uint RowVersion { get; set; }

    public Oblast Oblast { get; set; } = null!;
    public ICollection<DataSource> DataSources { get; set; } = new List<DataSource>();
    public ICollection<DiscoveryRun> DiscoveryRuns { get; set; } = new List<DiscoveryRun>();
}
