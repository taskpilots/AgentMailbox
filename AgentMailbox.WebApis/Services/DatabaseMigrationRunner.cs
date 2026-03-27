using FluentMigrator.Runner;

namespace AgentMailbox.WebApis.Services;

public sealed class DatabaseMigrationRunner : IDatabaseMigrationRunner
{
    private readonly IServiceProvider _serviceProvider;

    public DatabaseMigrationRunner(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task MigrateUpAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var migrationRunner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        migrationRunner.MigrateUp();
        return Task.CompletedTask;
    }
}
