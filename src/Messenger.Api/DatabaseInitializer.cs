using Messenger.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Api;

public class DatabaseInitializer : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(IServiceProvider services, ILogger<DatabaseInitializer> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        for (var attempt = 1; attempt <= 10; attempt++)
        {
            try
            {
                await db.Database.MigrateAsync(cancellationToken);
                await DbSeeder.SeedAdminAsync(scope.ServiceProvider);
                return;
            }
            catch (Exception ex) when (attempt < 10)
            {
                _logger.LogWarning(ex, "Database is not ready yet. Retry {Attempt}/10.", attempt);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
