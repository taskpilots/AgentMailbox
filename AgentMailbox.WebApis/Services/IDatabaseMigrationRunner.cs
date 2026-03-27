namespace AgentMailbox.WebApis.Services;

public interface IDatabaseMigrationRunner
{
    Task MigrateUpAsync(CancellationToken cancellationToken = default);
}
