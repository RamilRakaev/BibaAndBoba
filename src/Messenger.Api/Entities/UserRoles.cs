namespace Messenger.Api.Entities;

public static class UserRoles
{
    public const string User = "User";
    public const string Admin = "Admin";

    public static bool IsValid(string? role) =>
        role == User || role == Admin;
}
