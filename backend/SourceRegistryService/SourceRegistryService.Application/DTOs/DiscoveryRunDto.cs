using SourceRegistryService.Domain.Enums;

namespace SourceRegistryService.Application.DTOs;

public class DiscoveryRunDto
{
    public Guid Id { get; set; }
    public ScopeType ScopeType { get; set; }
    public Guid ScopeId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int CandidatesFound { get; set; }
    public int CandidatesInserted { get; set; }
    public string? Error { get; set; }
}
