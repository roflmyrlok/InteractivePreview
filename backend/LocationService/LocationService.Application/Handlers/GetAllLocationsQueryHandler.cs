using LocationService.Application.DTOs;
using LocationService.Application.Interfaces;
using LocationService.Application.Mapping;
using LocationService.Application.Queries;
using MediatR;

namespace LocationService.Application.Handlers
{
	public class GetAllLocationsQueryHandler : IRequestHandler<GetAllLocationsQuery, IEnumerable<LocationDto>>
	{
		private readonly ILocationRepository _locationRepository;
		private readonly LocationMapper _mapper;

		public GetAllLocationsQueryHandler(ILocationRepository locationRepository, LocationMapper mapper)
		{
			_locationRepository = locationRepository;
			_mapper = mapper;
		}

		public async Task<IEnumerable<LocationDto>> Handle(GetAllLocationsQuery request, CancellationToken cancellationToken)
		{
			var locations = await _locationRepository.GetAllAsync();
			return _mapper.LocationsToLocationDtos(locations);
		}
	}
}
