using Messenger.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Messenger.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAdminAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var hasher = services.GetRequiredService<IPasswordHasher<User>>();
        var options = services.GetRequiredService<IOptions<AdminSeedOptions>>().Value;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        var hasAdmin = await db.Users.AnyAsync(u => u.Role == UserRoles.Admin);
        if (hasAdmin)
        {
            return;
        }

        var userName = string.IsNullOrWhiteSpace(options.UserName) ? "admin" : options.UserName.Trim();
        var displayName = string.IsNullOrWhiteSpace(options.DisplayName) ? "Administrator" : options.DisplayName.Trim();
        var password = string.IsNullOrWhiteSpace(options.Password) ? "admin" : options.Password;

        var admin = new User
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            DisplayName = displayName,
            Role = UserRoles.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        admin.PasswordHash = hasher.HashPassword(admin, password);
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        logger.LogInformation("Initial administrator '{UserName}' has been created.", admin.UserName);
    }
}
