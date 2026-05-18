using SourceRegistryService.Domain.Entities;

namespace SourceRegistryService.Application.Interfaces;

public interface IHromadaRepository
{
    Task<IEnumerable<Hromada>> GetByOblastIdAsync(Guid oblastId);
    Task<Hromada?> GetByIdAsync(Guid id);
    Task<Hromada?> GetBySlugAndOblastAsync(string slug, Guid oblastId);
    Task<Hromada> AddAsync(Hromada hromada);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException() : base("RowVersion mismatch") { }
}
