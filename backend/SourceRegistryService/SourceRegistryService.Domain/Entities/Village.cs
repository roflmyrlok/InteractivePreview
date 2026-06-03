namespace SourceRegistryService.Domain.Entities;

public class Village
{
    public Guid Id { get; set; }
    public Guid HromadaId { get; set; }
    public string Name { get; set; } = "";
    public string NameUk { get; set; } = "";
    public string Slug { get; set; } = "";
    public string KatottgCode { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public uint RowVersion { get; set; }

    public Hromada Hromada { get; set; } = null!;
    public ICollection<DataSource> DataSources { get; set; } = new List<DataSource>();
}
