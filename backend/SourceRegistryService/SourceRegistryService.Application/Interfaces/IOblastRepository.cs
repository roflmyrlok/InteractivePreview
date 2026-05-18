using SourceRegistryService.Domain.Entities;

namespace SourceRegistryService.Application.Interfaces;

public interface IOblastRepository
{
    Task<IEnumerable<Oblast>> GetAllAsync();
    Task<Oblast?> GetByCodeAsync(string code);
    Task<Oblast?> GetByIdAsync(Guid id);
    Task<Oblast> AddAsync(Oblast oblast);
    Task SaveChangesAsync(CancellationToken ct = default);
}
