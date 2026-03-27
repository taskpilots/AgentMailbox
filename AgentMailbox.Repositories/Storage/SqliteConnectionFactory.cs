using AgentMailbox.Core.Options;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace AgentMailbox.Repositories.Storage;

public sealed class SqliteConnectionFactory : ISqliteConnectionFactory
{
    private readonly StorageOptions _storageOptions;

    public SqliteConnectionFactory(IOptions<StorageOptions> storageOptions)
    {
        _storageOptions = storageOptions.Value;
    }

    public async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var fullDatabasePath = GetFullDatabasePath(_storageOptions.DatabasePath);
        var directory = Path.GetDirectoryName(fullDatabasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        Directory.CreateDirectory(Path.GetFullPath(_storageOptions.RawMailRootPath));
        Directory.CreateDirectory(Path.GetFullPath(_storageOptions.AttachmentRootPath));

        var connection = new SqliteConnection($"Data Source={fullDatabasePath}");
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    public static string GetFullDatabasePath(string databasePath)
    {
        return Path.GetFullPath(databasePath);
    }
}
