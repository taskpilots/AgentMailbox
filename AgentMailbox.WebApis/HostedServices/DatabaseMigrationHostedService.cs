using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AgentMailbox.WebApis.Services;

namespace AgentMailbox.WebApis.HostedServices;

public sealed class DatabaseMigrationHostedService : IHostedService
{
    private readonly IDatabaseMigrationRunner _databaseMigrationRunner;
    private readonly ILogger<DatabaseMigrationHostedService> _logger;

    public DatabaseMigrationHostedService(
        IDatabaseMigrationRunner databaseMigrationRunner,
        ILogger<DatabaseMigrationHostedService> logger)
    {
        _databaseMigrationRunner = databaseMigrationRunner;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Applying database migrations.");
        await _databaseMigrationRunner.MigrateUpAsync(cancellationToken);
        _logger.LogInformation("Database migrations completed.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
