namespace SourceRegistryService.Application.DTOs;

public class VillageSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string NameUk { get; set; } = "";
    public string Slug { get; set; } = "";
    public string KatottgCode { get; set; } = "";
    public int ActiveSourceCount { get; set; }
    public int PendingSourceCount { get; set; }
}

public class VillageDetailDto
{
    public Guid Id { get; set; }
    public Guid HromadaId { get; set; }
    public string HromadaName { get; set; } = "";
    public string OblastName { get; set; } = "";
    public string Name { get; set; } = "";
    public string NameUk { get; set; } = "";
    public string Slug { get; set; } = "";
    public string KatottgCode { get; set; } = "";
    public uint RowVersion { get; set; }
    public IList<DataSourceDto> Sources { get; set; } = new List<DataSourceDto>();
}
