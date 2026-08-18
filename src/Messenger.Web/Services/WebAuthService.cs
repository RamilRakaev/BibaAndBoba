using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Messenger.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Messenger.Web.Services;

public class WebAuthService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IConfiguration _configuration;

    public WebAuthService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<(bool Success, string? Error)> LoginAsync(HttpContext context, string userName, string password)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            return (false, "Имя пользователя и пароль обязательны.");
        }

        var apiUri = GetApiUri();
        var cookieContainer = new CookieContainer();
        using var handler = new HttpClientHandler
        {
            CookieContainer = cookieContainer,
            UseCookies = true
        };
        using var client = new HttpClient(handler) { BaseAddress = apiUri };

        using var response = await client.PostAsJsonAsync("api/auth/login", new LoginRequest
        {
            UserName = userName.Trim(),
            Password = password
        });

        if (!response.IsSuccessStatusCode)
        {
            return (false, await ReadErrorAsync(response));
        }

        var user = await response.Content.ReadFromJsonAsync<CurrentUserDto>(JsonOptions);
        if (user is null)
        {
            return (false, "Пустой ответ от API.");
        }

        var apiCookie = cookieContainer.GetCookies(apiUri)[ApiSession.ApiCookieName];
        if (apiCookie is null)
        {
            return (false, "Не удалось получить cookie авторизации API.");
        }

        context.Response.Cookies.Append(ApiSession.ApiAuthCookieName, apiCookie.Value, CreateCookieOptions(context));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.GivenName, user.DisplayName),
            new Claim(ClaimTypes.Role, user.Role)
        };

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            });

        return (true, null);
    }

    public async Task LogoutAsync(HttpContext context)
    {
        var apiCookie = context.Request.Cookies[ApiSession.ApiAuthCookieName];
        if (!string.IsNullOrWhiteSpace(apiCookie))
        {
            var apiUri = GetApiUri();
            var cookieContainer = new CookieContainer();
            cookieContainer.Add(apiUri, new Cookie(ApiSession.ApiCookieName, apiCookie, "/"));

            using var handler = new HttpClientHandler
            {
                CookieContainer = cookieContainer,
                UseCookies = true
            };
            using var client = new HttpClient(handler) { BaseAddress = apiUri };
            try
            {
                await client.PostAsync("api/auth/logout", null);
            }
            catch
            {
                // Ignore API logout errors during local sign-out.
            }
        }

        context.Response.Cookies.Delete(ApiSession.ApiAuthCookieName);
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private Uri GetApiUri()
    {
        var baseUrl = (_configuration["ApiSettings:BaseUrl"] ?? "http://localhost:8081").TrimEnd('/');
        return new Uri(baseUrl + "/");
    }

    private static CookieOptions CreateCookieOptions(HttpContext context) => new()
    {
        HttpOnly = true,
        SameSite = SameSiteMode.Lax,
        Secure = context.Request.IsHttps,
        IsEssential = true,
        Expires = DateTimeOffset.UtcNow.AddDays(7)
    };

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadAsStringAsync();
        try
        {
            var error = JsonSerializer.Deserialize<ApiError>(payload, JsonOptions);
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                return error.Message;
            }
        }
        catch (JsonException)
        {
            // Keep raw payload.
        }

        return response.StatusCode == System.Net.HttpStatusCode.Unauthorized
            ? "Неверный логин или пароль."
            : "Ошибка авторизации.";
    }
}
