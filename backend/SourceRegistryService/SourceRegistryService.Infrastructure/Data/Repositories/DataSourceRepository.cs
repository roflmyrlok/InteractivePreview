using Microsoft.EntityFrameworkCore;
using SourceRegistryService.Application.Interfaces;
using SourceRegistryService.Domain.Entities;
using SourceRegistryService.Domain.Enums;
using SourceRegistryService.Infrastructure.Data;

namespace SourceRegistryService.Infrastructure.Data.Repositories;

public class DataSourceRepository : IDataSourceRepository
{
    private readonly SourceRegistryDbContext _context;

    public DataSourceRepository(SourceRegistryDbContext context) => _context = context;

    public async Task<IEnumerable<DataSource>> GetByScopeAsync(ScopeType scopeType, Guid scopeId, DataSourceStatus? status = null)
    {
        var query = _context.DataSources
            .Where(ds => ds.ScopeType == scopeType && ds.ScopeId == scopeId);
        if (status.HasValue)
            query = query.Where(ds => ds.Status == status.Value);
        return await query.OrderByDescending(ds => ds.CreatedAt).ToListAsync();
    }

    public async Task<DataSource?> GetByIdAsync(Guid id)
        => await _context.DataSources.FirstOrDefaultAsync(ds => ds.Id == id);

    public async Task<bool> ExistsByUrlAndScopeAsync(string url, ScopeType scopeType, Guid scopeId)
        // IgnoreQueryFilters so soft-deleted (rejected) sources still block re-insertion
        => await _context.DataSources.IgnoreQueryFilters()
            .AnyAsync(ds => ds.Url == url && ds.ScopeType == scopeType && ds.ScopeId == scopeId);

    public async Task<DataSource> AddAsync(DataSource dataSource)
    {
        _context.DataSources.Add(dataSource);
        await _context.SaveChangesAsync();
        return dataSource;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try { await _context.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException(); }
    }
}
