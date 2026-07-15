using System.Linq;
using System.Security.Claims;
using CEMETRIX.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;

namespace CEMETRIX.API.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;
    public CurrentUserService(IHttpContextAccessor accessor) => _accessor = accessor;
    public string? UserId => _accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    public string? UserName => _accessor.HttpContext?.User?.Identity?.Name;
    public bool IsAuthenticated => _accessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
    public bool IsInRole(string role) => _accessor.HttpContext?.User?.IsInRole(role) ?? false;
}
