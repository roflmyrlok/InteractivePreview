using LocationService.Application.DTOs;
using MediatR;

namespace LocationService.Application.Commands
{
    // Full replacement of a location and its details. RowVersion is the ETag returned
    // by the previous GET — required to prevent lost-update conflicts.
    public class UpdateLocationCommand : IRequest<UpdateLocationResult>
    {
        public Guid Id { get; set; }
        public uint RowVersion { get; set; }

        public string Address { get; set; } = "";
        // Coordinates are intentionally not part of UpdateLocationCommand —
        // a real shelter doesn't move. To "move" one, delete and recreate.

        // Replaces the full Details collection.
        public List<LocationDetailDto> Details { get; set; } = new();
    }

    public enum UpdateOutcome
    {
        Updated,
        NotFound,
        Conflict   // RowVersion mismatch
    }

    public class UpdateLocationResult
    {
        public UpdateOutcome Outcome { get; set; }
        public uint NewRowVersion { get; set; }
    }
}
