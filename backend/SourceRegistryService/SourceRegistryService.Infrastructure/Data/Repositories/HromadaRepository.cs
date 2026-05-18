using Microsoft.EntityFrameworkCore;
using SourceRegistryService.Application.Interfaces;
using SourceRegistryService.Domain.Entities;
using SourceRegistryService.Infrastructure.Data;

namespace SourceRegistryService.Infrastructure.Data.Repositories;

public class HromadaRepository : IHromadaRepository
{
    private readonly SourceRegistryDbContext _context;

    public HromadaRepository(SourceRegistryDbContext context) => _context = context;

    public async Task<IEnumerable<Hromada>> GetByOblastIdAsync(Guid oblastId)
        => await _context.Hromadas.Include(h => h.Oblast)
            .Where(h => h.OblastId == oblastId)
            .ToListAsync();

    public async Task<Hromada?> GetByIdAsync(Guid id)
        => await _context.Hromadas.Include(h => h.Oblast)
            .FirstOrDefaultAsync(h => h.Id == id);

    public async Task<Hromada?> GetBySlugAndOblastAsync(string slug, Guid oblastId)
        => await _context.Hromadas
            .FirstOrDefaultAsync(h => h.Slug == slug && h.OblastId == oblastId);

    public async Task<Hromada> AddAsync(Hromada hromada)
    {
        _context.Hromadas.Add(hromada);
        await _context.SaveChangesAsync();
        return hromada;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try { await _context.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException(); }
    }
}
