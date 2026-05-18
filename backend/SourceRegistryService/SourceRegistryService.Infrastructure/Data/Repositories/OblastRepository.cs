using Microsoft.EntityFrameworkCore;
using SourceRegistryService.Application.Interfaces;
using SourceRegistryService.Domain.Entities;
using SourceRegistryService.Infrastructure.Data;

namespace SourceRegistryService.Infrastructure.Data.Repositories;

public class OblastRepository : IOblastRepository
{
    private readonly SourceRegistryDbContext _context;

    public OblastRepository(SourceRegistryDbContext context) => _context = context;

    public async Task<IEnumerable<Oblast>> GetAllAsync()
        => await _context.Oblasts.Include(o => o.Hromadas).ToListAsync();

    public async Task<Oblast?> GetByCodeAsync(string code)
        => await _context.Oblasts.Include(o => o.Hromadas)
            .FirstOrDefaultAsync(o => o.Code == code);

    public async Task<Oblast?> GetByIdAsync(Guid id)
        => await _context.Oblasts.Include(o => o.Hromadas)
            .FirstOrDefaultAsync(o => o.Id == id);

    public async Task<Oblast> AddAsync(Oblast oblast)
    {
        _context.Oblasts.Add(oblast);
        await _context.SaveChangesAsync();
        return oblast;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try { await _context.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException(); }
    }
}
