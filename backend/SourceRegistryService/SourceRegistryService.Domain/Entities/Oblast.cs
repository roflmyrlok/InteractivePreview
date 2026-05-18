namespace SourceRegistryService.Domain.Entities;

public class Oblast
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string NameUk { get; set; } = "";
    public bool IsOccupied { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public uint RowVersion { get; set; }

    public ICollection<Hromada> Hromadas { get; set; } = new List<Hromada>();
}
