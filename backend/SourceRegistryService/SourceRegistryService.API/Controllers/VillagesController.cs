using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SourceRegistryService.Application.Queries;

namespace SourceRegistryService.API.Controllers;

[ApiController]
[Route("api/villages")]
public class VillagesController : ControllerBase
{
    private readonly IMediator _mediator;

    public VillagesController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetVillageByIdQuery { Id = id });
        if (result == null) return NotFound();
        return Ok(result);
    }
}
