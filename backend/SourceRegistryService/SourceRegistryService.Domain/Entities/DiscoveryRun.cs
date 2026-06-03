using SourceRegistryService.Domain.Enums;

namespace SourceRegistryService.Domain.Entities;

public class DiscoveryRun
{
    public Guid Id { get; set; }
    // Polymorphic scope (mirrors DataSource): which tier this discovery ran against.
    public ScopeType ScopeType { get; set; }
    public Guid ScopeId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int CandidatesFound { get; set; }
    public int CandidatesInserted { get; set; }
    public string? Error { get; set; }
    public Guid? TriggeredByUserId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public uint RowVersion { get; set; }
}
