using System.Net;

namespace Messenger.Web.Services;

public class ApiSession
{
    public const string ApiAuthCookieName = "Messenger.Api.Auth";
    public const string ApiCookieName = "Messenger.Auth";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Uri _apiUri;
    private bool _restored;

    public CookieContainer Cookies { get; } = new();

    public ApiSession(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        var baseUrl = (configuration["ApiSettings:BaseUrl"] ?? "http://localhost:8081").TrimEnd('/');
        _apiUri = new Uri(baseUrl + "/");
        RestoreCookies();
    }

    public void RestoreCookies()
    {
        if (_restored)
        {
            return;
        }

        var cookieValue = _httpContextAccessor.HttpContext?.Request.Cookies[ApiAuthCookieName];
        if (!string.IsNullOrWhiteSpace(cookieValue))
        {
            Cookies.Add(_apiUri, new Cookie(ApiCookieName, cookieValue, "/"));
        }

        _restored = true;
    }
}
