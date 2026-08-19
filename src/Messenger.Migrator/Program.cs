using Messenger.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Migrator
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var connectionString =
                Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? throw new InvalidOperationException(
                    "ConnectionStrings__DefaultConnection is not configured.");

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(connectionString)
                .Options;

            await using var db = new AppDbContext(options);

            Console.WriteLine("Applying migrations...");

            await db.Database.MigrateAsync();

            Console.WriteLine("Migrations applied successfully.");
        }
    }
}
