using System.Security.Claims;
using Messenger.Api.Entities;

namespace Messenger.Api;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(value) || !Guid.TryParse(value, out var id))
        {
            throw new InvalidOperationException("User id claim is missing.");
        }

        return id;
    }

    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole(UserRoles.Admin);
}
