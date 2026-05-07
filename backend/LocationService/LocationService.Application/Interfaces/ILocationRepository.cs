using System.Linq.Expressions;
using LocationService.Domain.Entities;

namespace LocationService.Application.Interfaces
{
	public interface ILocationRepository
	{
		Task<IEnumerable<Location>> GetAllAsync();
		Task<Location> GetByIdAsync(Guid id);
		Task<IEnumerable<Location>> FindAsync(Expression<Func<Location, bool>> predicate);
		Task<Location> AddAsync(Location location);
		Task UpdateAsync(Location location);
		Task DeleteAsync(Guid id);
		Task<bool> ExistsAsync(Guid id);

		// Returns a tracked Location (with Details loaded) suitable for mutation.
		// Returns null if not found OR soft-deleted.
		Task<Location?> GetForUpdateAsync(Guid id);

		// Removes a LocationDetail from change tracker (so SaveChanges deletes it).
		void RemoveDetail(LocationDetail detail);

		// Persists pending changes. Throws ConcurrencyConflictException on RowVersion mismatch.
		Task SaveChangesAsync(CancellationToken ct = default);
	}

	public class ConcurrencyConflictException : Exception
	{
		public ConcurrencyConflictException() : base("RowVersion mismatch") { }
	}

	public interface ILocationDetailRepository
	{
		Task<LocationDetail> GetByIdAsync(Guid id);
		Task<IEnumerable<LocationDetail>> GetByLocationIdAsync(Guid locationId);
		Task<LocationDetail> AddAsync(LocationDetail detail);
		Task UpdateAsync(LocationDetail detail);
		Task DeleteAsync(Guid id);
	}
}
