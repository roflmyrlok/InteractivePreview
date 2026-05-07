using LocationService.API.Auth;
using LocationService.Application.Commands;
using LocationService.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocationService.API.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class LocationsController : ControllerBase
	{
		private const string RowVersionHeader = "X-Row-Version";
		private readonly IMediator _mediator;

		public LocationsController(IMediator mediator)
		{
			_mediator = mediator;
		}

		// PUBLIC — anyone can read the shelter map.
		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> GetAll()
		{
			var query = new GetAllLocationsQuery();
			var locations = await _mediator.Send(query);
			return Ok(locations);
		}

		[HttpGet("{id}")]
		[AllowAnonymous]
		public async Task<IActionResult> GetById(Guid id)
		{
			try
			{
				var query = new GetLocationByIdQuery { Id = id };
				var location = await _mediator.Send(query);
				return Ok(location);
			}
			catch (Exception)
			{
				return NotFound();
			}
		}

		[HttpGet("validate/{id}")]
		[AllowAnonymous]
		public async Task<IActionResult> ValidateLocation(Guid id)
		{
			try
			{
				var query = new GetLocationByIdQuery { Id = id };
				var location = await _mediator.Send(query);
				return Ok(new { exists = location != null });
			}
			catch (Exception)
			{
				return Ok(new { exists = false });
			}
		}

		[HttpGet("nearby")]
		[AllowAnonymous]
		public async Task<IActionResult> GetNearby(
			[FromQuery] double latitude,
			[FromQuery] double longitude,
			[FromQuery] double radiusKm = 10)
		{
			var query = new GetNearbyLocationsQuery
			{
				Latitude = latitude,
				Longitude = longitude,
				RadiusKm = radiusKm
			};

			var locations = await _mediator.Send(query);
			return Ok(locations);
		}

		// WRITE — Admin/SuperAdmin/ServiceAccount.
		[HttpPost]
		[Authorize(Roles = Roles.Writers)]
		public async Task<IActionResult> Create([FromBody] CreateLocationCommand command)
		{
			var locationId = await _mediator.Send(command);
			return CreatedAtAction(nameof(GetById), new { id = locationId }, new { Id = locationId });
		}

		// PUT — full replace. Requires the RowVersion that the caller previously read.
		// Send it either in the body or in the X-Row-Version header.
		[HttpPut("{id}")]
		[Authorize(Roles = Roles.Writers)]
		public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLocationCommand command)
		{
			command.Id = id;
			if (command.RowVersion == 0) command.RowVersion = ReadRowVersionHeader();

			var result = await _mediator.Send(command);
			return result.Outcome switch
			{
				UpdateOutcome.Updated => Ok(new { id, rowVersion = result.NewRowVersion }),
				UpdateOutcome.NotFound => NotFound(),
				UpdateOutcome.Conflict => Conflict(new { error = "RowVersion mismatch — refresh and retry." }),
				_ => StatusCode(500)
			};
		}

		// PATCH — partial update. Same auth + RowVersion rules as PUT.
		[HttpPatch("{id}")]
		[Authorize(Roles = Roles.Writers)]
		public async Task<IActionResult> Patch(Guid id, [FromBody] PatchLocationCommand command)
		{
			command.Id = id;
			if (command.RowVersion == 0) command.RowVersion = ReadRowVersionHeader();

			var result = await _mediator.Send(command);
			return result.Outcome switch
			{
				UpdateOutcome.Updated => Ok(new { id, rowVersion = result.NewRowVersion }),
				UpdateOutcome.NotFound => NotFound(),
				UpdateOutcome.Conflict => Conflict(new { error = "RowVersion mismatch — refresh and retry." }),
				_ => StatusCode(500)
			};
		}

		// DELETE — soft delete only. Restricted to human admins (no service accounts).
		[HttpDelete("{id}")]
		[Authorize(Roles = Roles.HumanAdmins)]
		public async Task<IActionResult> Delete(Guid id, [FromQuery] uint? rowVersion)
		{
			var command = new DeleteLocationCommand
			{
				Id = id,
				RowVersion = rowVersion ?? ReadRowVersionHeader()
			};
			var result = await _mediator.Send(command);
			return result.Outcome switch
			{
				DeleteOutcome.Deleted => NoContent(),
				DeleteOutcome.NotFound => NotFound(),
				DeleteOutcome.Conflict => Conflict(new { error = "RowVersion mismatch — refresh and retry." }),
				_ => StatusCode(500)
			};
		}

		private uint ReadRowVersionHeader()
		{
			if (Request.Headers.TryGetValue(RowVersionHeader, out var values)
				&& uint.TryParse(values.ToString(), out var v))
				return v;
			return 0;
		}
	}
}
