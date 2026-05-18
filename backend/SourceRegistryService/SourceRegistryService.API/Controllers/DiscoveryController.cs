using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SourceRegistryService.API.Auth;
using SourceRegistryService.Application.Commands;
using SourceRegistryService.Application.Queries;

namespace SourceRegistryService.API.Controllers;

[ApiController]
[Route("api/discovery")]
public class DiscoveryController : ControllerBase
{
    private readonly IMediator _mediator;

    public DiscoveryController(IMediator mediator) => _mediator = mediator;

    [HttpPost("hromada/{id:guid}")]
    [Authorize(Roles = Roles.HumanAdmins)]
    public async Task<IActionResult> RunDiscovery(Guid id)
    {
        var result = await _mediator.Send(new RunDiscoveryCommand { HromadaId = id });
        if (!result.Success)
            return BadRequest(new { error = result.Error });
        return Ok(result.Run);
    }

    [HttpGet("runs")]
    [Authorize]
    public async Task<IActionResult> GetRuns([FromQuery] Guid hromadaId)
    {
        if (hromadaId == Guid.Empty) return BadRequest(new { error = "hromadaId is required" });
        var result = await _mediator.Send(new GetDiscoveryRunsQuery { HromadaId = hromadaId });
        return Ok(result);
    }
}
