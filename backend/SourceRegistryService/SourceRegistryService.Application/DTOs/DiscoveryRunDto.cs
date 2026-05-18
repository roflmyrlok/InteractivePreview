namespace SourceRegistryService.Application.DTOs;

public class DiscoveryRunDto
{
    public Guid Id { get; set; }
    public Guid HromadaId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int CandidatesFound { get; set; }
    public int CandidatesInserted { get; set; }
    public string? Error { get; set; }
}
