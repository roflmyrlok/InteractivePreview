using SourceRegistryService.Domain.Entities;
using SourceRegistryService.Domain.Enums;

namespace SourceRegistryService.Application.Interfaces;

public interface IDataSourceRepository
{
    Task<IEnumerable<DataSource>> GetByScopeAsync(ScopeType scopeType, Guid scopeId, DataSourceStatus? status = null);
    Task<DataSource?> GetByIdAsync(Guid id);
    Task<bool> ExistsByUrlAndScopeAsync(string url, ScopeType scopeType, Guid scopeId);
    Task<DataSource> AddAsync(DataSource dataSource);
    Task SaveChangesAsync(CancellationToken ct = default);
}
