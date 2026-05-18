using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using SourceRegistryService.Application.Interfaces;

namespace SourceRegistryService.API.Auth;

public class HttpCurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _accessor;

    public HttpCurrentUserContext(IHttpContextAccessor accessor) => _accessor = accessor;

    public bool IsAuthenticated =>
        _accessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            var user = _accessor.HttpContext?.User;
            if (user == null) return null;
            var raw = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                   ?? user.FindFirst("sub")?.Value;
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public string? Role =>
        _accessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
}
