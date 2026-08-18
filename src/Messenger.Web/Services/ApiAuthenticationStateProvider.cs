using System.Security.Claims;
using Messenger.Web.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace Messenger.Web.Services;

public class ApiAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ApiClient _api;
    private readonly ApiSession _session;
    private CurrentUserDto? _currentUser;

    public ApiAuthenticationStateProvider(ApiClient api, ApiSession session)
    {
        _api = api;
        _session = session;
    }

    public CurrentUserDto? CurrentUser => _currentUser;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            _currentUser = await _api.GetMeAsync();
        }
        catch
        {
            _currentUser = null;
        }

        return new AuthenticationState(CreatePrincipal(_currentUser));
    }

    public async Task<CurrentUserDto> LoginAsync(string userName, string password)
    {
        _currentUser = await _api.LoginAsync(new LoginRequest
        {
            UserName = userName,
            Password = password
        });
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(CreatePrincipal(_currentUser))));
        return _currentUser;
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _api.LogoutAsync();
        }
        finally
        {
            _session.Clear();
            _currentUser = null;
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(CreatePrincipal(null))));
        }
    }

    private static ClaimsPrincipal CreatePrincipal(CurrentUserDto? user)
    {
        if (user is null)
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.GivenName, user.DisplayName),
            new Claim(ClaimTypes.Role, user.Role)
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies"));
    }
}
