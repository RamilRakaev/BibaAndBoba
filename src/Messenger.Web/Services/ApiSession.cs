using System.Net;

namespace Messenger.Web.Services;

public class ApiSession
{
    public CookieContainer Cookies { get; } = new();

    public void Clear()
    {
        foreach (Cookie cookie in Cookies.GetAllCookies())
        {
            cookie.Expired = true;
        }
    }
}
