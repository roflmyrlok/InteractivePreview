namespace SourceRegistryService.Application.DTOs;

public class HromadaSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string NameUk { get; set; } = "";
    public string Slug { get; set; } = "";
    public int ActiveSourceCount { get; set; }
    public int PendingSourceCount { get; set; }
}

public class HromadaDetailDto
{
    public Guid Id { get; set; }
    public Guid OblastId { get; set; }
    public string OblastName { get; set; } = "";
    public string Name { get; set; } = "";
    public string NameUk { get; set; } = "";
    public string Slug { get; set; } = "";
    public uint RowVersion { get; set; }
    public IList<DataSourceDto> Sources { get; set; } = new List<DataSourceDto>();
}

public class CreateHromadaRequest
{
    public Guid OblastId { get; set; }
    public string Name { get; set; } = "";
    public string NameUk { get; set; } = "";
    public string Slug { get; set; } = "";
}

public class PatchHromadaRequest
{
    public string? Name { get; set; }
    public string? NameUk { get; set; }
    public string? Slug { get; set; }
    public uint RowVersion { get; set; }
}
