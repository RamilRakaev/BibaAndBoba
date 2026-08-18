using Messenger.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Messenger.Web;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("");

        group.MapPost("/auth/login", async (
            HttpContext context,
            WebAuthService auth,
            [FromForm] string userName,
            [FromForm] string password) =>
        {
            var (success, error) = await auth.LoginAsync(context, userName, password);
            return success
                ? Results.Redirect("/chats")
                : Results.Redirect($"/login?error={Uri.EscapeDataString(error ?? "Ошибка авторизации")}");
        }).DisableAntiforgery();

        group.MapGet("/auth/logout", async (HttpContext context, WebAuthService auth) =>
        {
            await auth.LogoutAsync(context);
            return Results.Redirect("/login");
        });

        return group;
    }
}
