using System.Security.Claims;
using CEMETRIX.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;

namespace CEMETRIX.Web.Services;

public class UserSessionService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public UserSessionService(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public string? UserId =>
        _accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? _accessor.HttpContext?.User?.FindFirstValue("sub");
    public string? UserName => _accessor.HttpContext?.User?.Identity?.Name;
    public bool IsAuthenticated => _accessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
    public bool IsInRole(string role) => _accessor.HttpContext?.User?.IsInRole(role) ?? false;
}
