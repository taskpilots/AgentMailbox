using AgentMailbox.Core.Options;
using AgentMailbox.Repositories.Migrations;
using AgentMailbox.Repositories.Abstractions;
using AgentMailbox.Repositories.Storage;
using AgentMailbox.WebApis.HostedServices;
using AgentMailbox.WebApis.Services;
using Dapper;
using FluentMigrator.Runner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgentMailbox.WebApis;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAgentMailboxApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        DapperSqliteTypeHandlers.Register();

        services.Configure<StorageOptions>(configuration.GetSection("Storage"));
        services.Configure<InboundListenerOptions>(configuration.GetSection("InboundListener"));
        services.Configure<OutboundSmtpOptions>(configuration.GetSection("OutboundSmtp"));
        services.Configure<RetryOptions>(configuration.GetSection("Retry"));

        services.AddHealthChecks();

        services.AddSingleton<ISqliteConnectionFactory, SqliteConnectionFactory>();
        services.AddFluentMigratorCore()
            .ConfigureRunner(runnerBuilder =>
            {
                runnerBuilder
                    .AddSQLite()
                    .WithGlobalConnectionString(GetSqliteConnectionString(configuration))
                    .ScanIn(typeof(InitialSqliteSchemaMigration).Assembly).For.Migrations();
            });

        services.AddScoped<SqliteMailRepository>();
        services.AddScoped<IMailboxRepository>(serviceProvider => serviceProvider.GetRequiredService<SqliteMailRepository>());
        services.AddScoped<IMailMessageRepository>(serviceProvider => serviceProvider.GetRequiredService<SqliteMailRepository>());
        services.AddScoped<IOutboundTaskRepository>(serviceProvider => serviceProvider.GetRequiredService<SqliteMailRepository>());

        services.AddScoped<IMailboxApplicationService, MailboxApplicationService>();
        services.AddScoped<IMailQueryApplicationService, MailQueryApplicationService>();
        services.AddScoped<IMailSendApplicationService, MailSendApplicationService>();
        services.AddSingleton<IDatabaseMigrationRunner, DatabaseMigrationRunner>();

        services.AddHostedService<DatabaseMigrationHostedService>();
        services.AddHostedService<InboundMailListenerHostedService>();
        services.AddHostedService<OutboundMailDispatcherHostedService>();

        return services;
    }

    private static string GetSqliteConnectionString(IConfiguration configuration)
    {
        var databasePath = configuration.GetSection("Storage").GetValue<string>("DatabasePath") ?? "app_data/agentmailbox.db";
        var fullDatabasePath = SqliteConnectionFactory.GetFullDatabasePath(databasePath);
        var directory = Path.GetDirectoryName(fullDatabasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return $"Data Source={fullDatabasePath}";
    }
}
