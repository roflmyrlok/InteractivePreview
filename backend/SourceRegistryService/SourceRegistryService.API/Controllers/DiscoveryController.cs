using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SourceRegistryService.API.Auth;
using SourceRegistryService.Application.Commands;
using SourceRegistryService.Application.Queries;
using SourceRegistryService.Domain.Enums;

namespace SourceRegistryService.API.Controllers;

[ApiController]
[Route("api/discovery")]
public class DiscoveryController : ControllerBase
{
    private readonly IMediator _mediator;

    public DiscoveryController(IMediator mediator) => _mediator = mediator;

    [HttpPost("oblast/{id:guid}")]
    [Authorize(Roles = Roles.HumanAdmins)]
    public Task<IActionResult> RunOblastDiscovery(Guid id, [FromQuery] bool autoActivate = false)
        => RunDiscovery(ScopeType.Oblast, id, autoActivate);

    [HttpPost("hromada/{id:guid}")]
    [Authorize(Roles = Roles.HumanAdmins)]
    public Task<IActionResult> RunHromadaDiscovery(Guid id, [FromQuery] bool autoActivate = false)
        => RunDiscovery(ScopeType.Hromada, id, autoActivate);

    [HttpPost("village/{id:guid}")]
    [Authorize(Roles = Roles.HumanAdmins)]
    public Task<IActionResult> RunVillageDiscovery(Guid id, [FromQuery] bool autoActivate = false)
        => RunDiscovery(ScopeType.Village, id, autoActivate);

    private async Task<IActionResult> RunDiscovery(ScopeType scope, Guid id, bool autoActivate)
    {
        var result = await _mediator.Send(new RunDiscoveryCommand
        {
            ScopeType = scope,
            ScopeId = id,
            AutoActivate = autoActivate
        });
        if (!result.Success)
            return BadRequest(new { error = result.Error });
        return Ok(result.Run);
    }

    // Back-compatible: ?hromadaId=... still works; or pass ?scopeType=&scopeId=.
    [HttpGet("runs")]
    [Authorize]
    public async Task<IActionResult> GetRuns(
        [FromQuery] Guid hromadaId,
        [FromQuery] ScopeType? scopeType,
        [FromQuery] Guid? scopeId)
    {
        var resolvedScope = scopeType ?? ScopeType.Hromada;
        var resolvedId = scopeId ?? hromadaId;
        if (resolvedId == Guid.Empty)
            return BadRequest(new { error = "scopeId (or hromadaId) is required" });

        var result = await _mediator.Send(new GetDiscoveryRunsQuery
        {
            ScopeType = resolvedScope,
            ScopeId = resolvedId
        });
        return Ok(result);
    }
}
