using System.Security.Claims;
using Messenger.Web.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace Messenger.Web.Services;

public class ApiAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ApiSession _session;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiAuthenticationStateProvider(ApiSession session, IHttpContextAccessor httpContextAccessor)
    {
        _session = session;
        _httpContextAccessor = httpContextAccessor;
    }

    public CurrentUserDto? CurrentUser
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.Identity?.IsAuthenticated == true ? MapFromPrincipal(user) : null;
        }
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        _session.RestoreCookies();

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User.Identity?.IsAuthenticated == true)
        {
            return Task.FromResult(new AuthenticationState(httpContext.User));
        }

        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    private static CurrentUserDto MapFromPrincipal(ClaimsPrincipal user) => new()
    {
        Id = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!),
        UserName = user.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
        DisplayName = user.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty,
        Role = user.FindFirstValue(ClaimTypes.Role) ?? "User",
        IsActive = true
    };
}
