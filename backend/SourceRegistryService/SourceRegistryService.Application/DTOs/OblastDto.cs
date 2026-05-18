namespace SourceRegistryService.Application.DTOs;

public class OblastSummaryDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string NameUk { get; set; } = "";
    public bool IsOccupied { get; set; }
    public int HromadaCount { get; set; }
    public int ActiveSourceCount { get; set; }
}

public class OblastDetailDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string NameUk { get; set; } = "";
    public bool IsOccupied { get; set; }
    public uint RowVersion { get; set; }
    public IList<HromadaSummaryDto> Hromadas { get; set; } = new List<HromadaSummaryDto>();
}
