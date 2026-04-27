using Riok.Mapperly.Abstractions;
using LocationService.Application.DTOs;
using LocationService.Domain.Entities;

namespace LocationService.Application.Mapping;

[Mapper]
public partial class LocationMapper
{
	public partial LocationDto LocationToLocationDto(Location location);
	public partial IEnumerable<LocationDto> LocationsToLocationDtos(IEnumerable<Location> locations);
	public partial LocationDetailDto LocationDetailToLocationDetailDto(LocationDetail detail);
}
