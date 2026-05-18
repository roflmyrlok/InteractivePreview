using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SourceRegistryService.Application.Queries;

namespace SourceRegistryService.API.Controllers;

[ApiController]
[Route("api/oblasts")]
public class OblastsController : ControllerBase
{
    private readonly IMediator _mediator;

    public OblastsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetAllOblastsQuery());
        return Ok(result);
    }

    [HttpGet("{code}")]
    [Authorize]
    public async Task<IActionResult> GetByCode(string code)
    {
        var result = await _mediator.Send(new GetOblastByCodeQuery { Code = code });
        if (result == null) return NotFound();
        return Ok(result);
    }
}
