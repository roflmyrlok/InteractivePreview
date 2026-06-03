using Microsoft.EntityFrameworkCore;
using SourceRegistryService.Application.Interfaces;
using SourceRegistryService.Domain.Entities;
using SourceRegistryService.Infrastructure.Data;

namespace SourceRegistryService.Infrastructure.Data.Repositories;

public class VillageRepository : IVillageRepository
{
    private readonly SourceRegistryDbContext _context;

    public VillageRepository(SourceRegistryDbContext context) => _context = context;

    public async Task<IEnumerable<Village>> GetByHromadaIdAsync(Guid hromadaId)
        => await _context.Villages
            .Where(v => v.HromadaId == hromadaId)
            .OrderBy(v => v.Name)
            .ToListAsync();

    public async Task<Village?> GetByIdAsync(Guid id)
        => await _context.Villages
            .Include(v => v.Hromada).ThenInclude(h => h.Oblast)
            .FirstOrDefaultAsync(v => v.Id == id);

    public async Task<Village?> GetBySlugAndHromadaAsync(string slug, Guid hromadaId)
        => await _context.Villages
            .FirstOrDefaultAsync(v => v.Slug == slug && v.HromadaId == hromadaId);

    public async Task<Village> AddAsync(Village village)
    {
        _context.Villages.Add(village);
        await _context.SaveChangesAsync();
        return village;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try { await _context.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException(); }
    }
}
