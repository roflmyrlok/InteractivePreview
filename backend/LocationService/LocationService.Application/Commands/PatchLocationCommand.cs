using LocationService.Application.DTOs;
using MediatR;

namespace LocationService.Application.Commands
{
    // Partial update. Each provided field is overlaid onto the existing record.
    // Missing fields are left unchanged. Details are upserted by PropertyName.
    public class PatchLocationCommand : IRequest<UpdateLocationResult>
    {
        public Guid Id { get; set; }
        public uint RowVersion { get; set; }

        public string? Address { get; set; }

        // For each entry: if PropertyName already exists on the location, value is
        // replaced; otherwise a new detail is inserted. To remove a detail, send
        // PropertyValue = "" (treated as removal).
        public List<LocationDetailDto> Details { get; set; } = new();
    }
}
