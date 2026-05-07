namespace LocationService.Application.DTOs
{
	public class LocationDto
	{
		public Guid Id { get; set; }
		public double Latitude { get; set; }
		public double Longitude { get; set; }
		public string Address { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
		// ETag value for optimistic concurrency. Send this on PUT/PATCH/DELETE
		// in the X-Row-Version header (or in the request body).
		public uint RowVersion { get; set; }
		public ICollection<LocationDetailDto> Details { get; set; } = new List<LocationDetailDto>();
	}
}