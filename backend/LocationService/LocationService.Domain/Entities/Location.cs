namespace LocationService.Domain.Entities
{
	public class Location
	{
		public Guid Id { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public double Latitude { get; set; }
		public double Longitude { get; set; }
		public string Address { get; set; }
		public ICollection<LocationDetail> Details { get; set; } = new List<LocationDetail>();

		// Soft delete: rows are never physically removed. Filter by IsDeleted in queries.
		public bool IsDeleted { get; set; }
		public DateTime? DeletedAt { get; set; }
		public Guid? DeletedByUserId { get; set; }

		// Optimistic concurrency token. PostgreSQL "xmin" system column maps to a uint
		// version that EF Core auto-bumps on each update. Used as ETag for PUT/PATCH.
		public uint RowVersion { get; set; }
	}
}
