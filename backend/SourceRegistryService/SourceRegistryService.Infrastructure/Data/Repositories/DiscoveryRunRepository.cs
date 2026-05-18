using Microsoft.EntityFrameworkCore;
using SourceRegistryService.Application.Interfaces;
using SourceRegistryService.Domain.Entities;
using SourceRegistryService.Infrastructure.Data;

namespace SourceRegistryService.Infrastructure.Data.Repositories;

public class DiscoveryRunRepository : IDiscoveryRunRepository
{
    private readonly SourceRegistryDbContext _context;

    public DiscoveryRunRepository(SourceRegistryDbContext context) => _context = context;

    public async Task<IEnumerable<DiscoveryRun>> GetByHromadaIdAsync(Guid hromadaId)
        => await _context.DiscoveryRuns
            .Where(r => r.HromadaId == hromadaId)
            .OrderByDescending(r => r.StartedAt)
            .ToListAsync();

    public async Task<DiscoveryRun> AddAsync(DiscoveryRun run)
    {
        _context.DiscoveryRuns.Add(run);
        await _context.SaveChangesAsync();
        return run;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try { await _context.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException(); }
    }
}
