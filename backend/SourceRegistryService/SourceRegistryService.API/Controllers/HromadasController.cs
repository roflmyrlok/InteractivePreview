using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SourceRegistryService.API.Auth;
using SourceRegistryService.Application.Commands;
using SourceRegistryService.Application.Queries;

namespace SourceRegistryService.API.Controllers;

[ApiController]
[Route("api/hromadas")]
public class HromadasController : ControllerBase
{
    private const string RowVersionHeader = "X-Row-Version";
    private readonly IMediator _mediator;

    public HromadasController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetHromadaByIdQuery { Id = id });
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = Roles.HumanAdmins)]
    public async Task<IActionResult> Create([FromBody] CreateHromadaCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, new { Id = id });
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = Roles.HumanAdmins)]
    public async Task<IActionResult> Patch(Guid id, [FromBody] PatchHromadaCommand command)
    {
        command.Id = id;
        if (command.RowVersion == 0) command.RowVersion = ReadRowVersionHeader();

        var result = await _mediator.Send(command);
        return result.Outcome switch
        {
            PatchHromadaOutcome.Updated => Ok(new { id, rowVersion = result.NewRowVersion }),
            PatchHromadaOutcome.NotFound => NotFound(),
            PatchHromadaOutcome.Conflict => Conflict(new { error = "RowVersion mismatch — refresh and retry." }),
            _ => StatusCode(500)
        };
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.HumanAdmins)]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] uint? rowVersion)
    {
        var command = new DeleteHromadaCommand
        {
            Id = id,
            RowVersion = rowVersion ?? ReadRowVersionHeader()
        };
        var result = await _mediator.Send(command);
        return result.Outcome switch
        {
            DeleteHromadaOutcome.Deleted => NoContent(),
            DeleteHromadaOutcome.NotFound => NotFound(),
            DeleteHromadaOutcome.Conflict => Conflict(new { error = "RowVersion mismatch — refresh and retry." }),
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
