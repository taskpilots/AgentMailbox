using Microsoft.Data.Sqlite;

namespace AgentMailbox.Repositories.Storage;

public interface ISqliteConnectionFactory
{
    Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}
