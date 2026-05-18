using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SourceRegistryService.API.Auth;
using SourceRegistryService.Application.Commands;
using SourceRegistryService.Application.Queries;
using SourceRegistryService.Domain.Enums;

namespace SourceRegistryService.API.Controllers;

[ApiController]
[Route("api/sources")]
public class SourcesController : ControllerBase
{
    private const string RowVersionHeader = "X-Row-Version";
    private readonly IMediator _mediator;

    public SourcesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Get([FromQuery] Guid? hromadaId, [FromQuery] DataSourceStatus? status)
    {
        var result = await _mediator.Send(new GetSourcesQuery { HromadaId = hromadaId, Status = status });
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = Roles.HumanAdmins)]
    public async Task<IActionResult> Create([FromBody] CreateDataSourceCommand command)
    {
        command.Origin = DataSourceOrigin.Manual;
        command.Status = DataSourceStatus.Active;

        var result = await _mediator.Send(command);
        return result.Outcome switch
        {
            CreateDataSourceOutcome.Created => CreatedAtAction(nameof(Get),
                new { hromadaId = command.ScopeId }, new { Id = result.Id }),
            CreateDataSourceOutcome.Duplicate => Conflict(new { error = "This URL is already registered for this scope." }),
            _ => StatusCode(500)
        };
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = Roles.HumanAdmins)]
    public async Task<IActionResult> Patch(Guid id, [FromBody] PatchDataSourceCommand command)
    {
        command.Id = id;
        if (command.RowVersion == 0) command.RowVersion = ReadRowVersionHeader();

        var result = await _mediator.Send(command);
        return result.Outcome switch
        {
            PatchDataSourceOutcome.Updated => Ok(new { id, rowVersion = result.NewRowVersion }),
            PatchDataSourceOutcome.NotFound => NotFound(),
            PatchDataSourceOutcome.Conflict => Conflict(new { error = "RowVersion mismatch — refresh and retry." }),
            _ => StatusCode(500)
        };
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = Roles.HumanAdmins)]
    public async Task<IActionResult> Approve(Guid id, [FromQuery] uint? rowVersion)
    {
        var command = new ApproveDataSourceCommand
        {
            Id = id,
            RowVersion = rowVersion ?? ReadRowVersionHeader()
        };
        var result = await _mediator.Send(command);
        return result.Outcome switch
        {
            ApproveDataSourceOutcome.Approved => Ok(new { id, rowVersion = result.NewRowVersion }),
            ApproveDataSourceOutcome.AlreadyActive => Ok(new { id, rowVersion = result.NewRowVersion }),
            ApproveDataSourceOutcome.NotFound => NotFound(),
            ApproveDataSourceOutcome.Conflict => Conflict(new { error = "RowVersion mismatch — refresh and retry." }),
            _ => StatusCode(500)
        };
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = Roles.HumanAdmins)]
    public async Task<IActionResult> Reject(Guid id, [FromQuery] uint? rowVersion)
    {
        var command = new RejectDataSourceCommand
        {
            Id = id,
            RowVersion = rowVersion ?? ReadRowVersionHeader()
        };
        var result = await _mediator.Send(command);
        return result.Outcome switch
        {
            RejectDataSourceOutcome.Rejected => Ok(new { id, rowVersion = result.NewRowVersion }),
            RejectDataSourceOutcome.NotFound => NotFound(),
            RejectDataSourceOutcome.Conflict => Conflict(new { error = "RowVersion mismatch — refresh and retry." }),
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
