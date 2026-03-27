using AgentMailbox.Core.Contracts;
using AgentMailbox.Repositories.Abstractions;
using AgentMailbox.WebApis;
using AgentMailbox.WebApis.HostedServices;
using AgentMailbox.WebApis.Services;
using FluentMigrator.Runner;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace AgentMailbox.WebApis.Tests;

public sealed class DatabaseMigrationTests
{
    [Fact]
    public async Task MigrationHostedService_ShouldCreateSchema_AndBeIdempotent()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        await using var serviceProvider = BuildServiceProvider(databasePath);

        var hostedService = new DatabaseMigrationHostedService(
            serviceProvider.GetRequiredService<IDatabaseMigrationRunner>(),
            NullLogger<DatabaseMigrationHostedService>.Instance);

        await hostedService.StartAsync(CancellationToken.None);
        await hostedService.StartAsync(CancellationToken.None);

        await using var connection = new SqliteConnection($"Data Source={Path.GetFullPath(databasePath)}");
        await connection.OpenAsync();

        var expectedTables = new[]
        {
            "mailboxes",
            "mail_threads",
            "mail_messages",
            "outbound_tasks",
            "VersionInfo"
        };

        foreach (var tableName in expectedTables)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT COUNT(*)
                FROM sqlite_master
                WHERE type = 'table' AND name = $name;
                """;
            command.Parameters.AddWithValue("$name", tableName);
            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            Assert.Equal(1, count);
        }
    }

    [Fact]
    public async Task MigrationHostedService_ShouldSupportMailMainFlow_AfterMigration()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        await using var serviceProvider = BuildServiceProvider(databasePath);

        var hostedService = new DatabaseMigrationHostedService(
            serviceProvider.GetRequiredService<IDatabaseMigrationRunner>(),
            NullLogger<DatabaseMigrationHostedService>.Instance);

        await hostedService.StartAsync(CancellationToken.None);

        using var scope = serviceProvider.CreateScope();
        var mailboxService = scope.ServiceProvider.GetRequiredService<IMailboxApplicationService>();
        var sendService = scope.ServiceProvider.GetRequiredService<IMailSendApplicationService>();
        var queryService = scope.ServiceProvider.GetRequiredService<IMailQueryApplicationService>();
        var outboundTaskRepository = scope.ServiceProvider.GetRequiredService<IOutboundTaskRepository>();

        var mailbox = await mailboxService.CreateMailboxAsync(new CreateMailboxRequest
        {
            Address = "support-agent@local.ai",
            DisplayName = "Support Agent"
        });

        var message = await sendService.SendMailAsync(new SendMailRequest
        {
            MailboxAddress = mailbox.Address,
            ToAddress = "user@example.com",
            Subject = "Hello",
            BodyText = "Welcome"
        });

        var queriedMessage = await queryService.GetMessageAsync(message.MessageId);
        var thread = await queryService.GetThreadAsync(message.ThreadId);
        var pendingTasks = await outboundTaskRepository.ListPendingAsync(10);

        Assert.NotNull(queriedMessage);
        Assert.NotNull(thread);
        Assert.Single(thread.Messages);
        Assert.Single(pendingTasks);
    }

    [Fact]
    public async Task MigrationHostedService_ShouldKeepRepliesOnExistingThread_ForSameMailbox()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        await using var serviceProvider = BuildServiceProvider(databasePath);

        var hostedService = new DatabaseMigrationHostedService(
            serviceProvider.GetRequiredService<IDatabaseMigrationRunner>(),
            NullLogger<DatabaseMigrationHostedService>.Instance);

        await hostedService.StartAsync(CancellationToken.None);

        using var scope = serviceProvider.CreateScope();
        var mailboxService = scope.ServiceProvider.GetRequiredService<IMailboxApplicationService>();
        var sendService = scope.ServiceProvider.GetRequiredService<IMailSendApplicationService>();
        var queryService = scope.ServiceProvider.GetRequiredService<IMailQueryApplicationService>();

        var mailbox = await mailboxService.CreateMailboxAsync(new CreateMailboxRequest
        {
            Address = "support-agent@local.ai",
            DisplayName = "Support Agent"
        });

        var originalMessage = await sendService.SendMailAsync(new SendMailRequest
        {
            MailboxAddress = mailbox.Address,
            ToAddress = "user@example.com",
            Subject = "Hello",
            BodyText = "Welcome"
        });

        var reply = await sendService.ReplyAsync(originalMessage.MessageId, new ReplyMailRequest
        {
            MailboxAddress = mailbox.Address,
            BodyText = "Follow-up"
        });

        var thread = await queryService.GetThreadAsync(originalMessage.ThreadId);

        Assert.NotNull(reply);
        Assert.NotNull(thread);
        Assert.Equal(originalMessage.ThreadId, reply.ThreadId);
        Assert.Equal(2, thread.Messages.Count);
    }

    [Fact]
    public async Task MigrationHostedService_ShouldRejectReply_WhenMailboxDoesNotOwnThread()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        await using var serviceProvider = BuildServiceProvider(databasePath);

        var hostedService = new DatabaseMigrationHostedService(
            serviceProvider.GetRequiredService<IDatabaseMigrationRunner>(),
            NullLogger<DatabaseMigrationHostedService>.Instance);

        await hostedService.StartAsync(CancellationToken.None);

        using var scope = serviceProvider.CreateScope();
        var mailboxService = scope.ServiceProvider.GetRequiredService<IMailboxApplicationService>();
        var sendService = scope.ServiceProvider.GetRequiredService<IMailSendApplicationService>();

        var ownerMailbox = await mailboxService.CreateMailboxAsync(new CreateMailboxRequest
        {
            Address = "support-agent@local.ai",
            DisplayName = "Support Agent"
        });

        var otherMailbox = await mailboxService.CreateMailboxAsync(new CreateMailboxRequest
        {
            Address = "sales-agent@local.ai",
            DisplayName = "Sales Agent"
        });

        var originalMessage = await sendService.SendMailAsync(new SendMailRequest
        {
            MailboxAddress = ownerMailbox.Address,
            ToAddress = "user@example.com",
            Subject = "Hello",
            BodyText = "Welcome"
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => sendService.ReplyAsync(
            originalMessage.MessageId,
            new ReplyMailRequest
            {
                MailboxAddress = otherMailbox.Address,
                BodyText = "This should fail"
            }));

        Assert.Contains("cannot reply", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Repository_ShouldRejectReplyMessage_WhenThreadDoesNotExist()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        await using var serviceProvider = BuildServiceProvider(databasePath);

        var hostedService = new DatabaseMigrationHostedService(
            serviceProvider.GetRequiredService<IDatabaseMigrationRunner>(),
            NullLogger<DatabaseMigrationHostedService>.Instance);

        await hostedService.StartAsync(CancellationToken.None);

        using var scope = serviceProvider.CreateScope();
        var mailboxService = scope.ServiceProvider.GetRequiredService<IMailboxApplicationService>();
        var mailMessageRepository = scope.ServiceProvider.GetRequiredService<IMailMessageRepository>();

        var mailbox = await mailboxService.CreateMailboxAsync(new CreateMailboxRequest
        {
            Address = "support-agent@local.ai",
            DisplayName = "Support Agent"
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => mailMessageRepository.CreateOutboundReplyMessageAsync(
            new AgentMailbox.Core.Models.MailMessageRecord
            {
                MessageId = Guid.NewGuid().ToString("N"),
                ThreadId = "missing-thread",
                MailboxId = mailbox.MailboxId,
                Direction = "outbound",
                Subject = "Hello",
                FromAddress = mailbox.Address,
                ToAddress = "user@example.com",
                BodyText = "Welcome",
                OutboundStatus = AgentMailbox.Core.Models.OutboundMailStatus.Pending,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                InReplyToMessageId = "missing-parent"
            }));

        Assert.Contains("was not found", exception.Message, StringComparison.OrdinalIgnoreCase);
        var threadMessages = await mailMessageRepository.ListThreadMessagesAsync("missing-thread");
        Assert.Empty(threadMessages);
    }

    [Fact]
    public async Task MigrationHostedService_ShouldPropagateRunnerFailures()
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IDatabaseMigrationRunner, ThrowingDatabaseMigrationRunner>()
            .BuildServiceProvider();

        var hostedService = new DatabaseMigrationHostedService(
            serviceProvider.GetRequiredService<IDatabaseMigrationRunner>(),
            NullLogger<DatabaseMigrationHostedService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => hostedService.StartAsync(CancellationToken.None));
    }

    private static ServiceProvider BuildServiceProvider(string databasePath)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:DatabasePath"] = databasePath,
                ["Retry:InitialDelaySeconds"] = "0"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAgentMailboxApplicationServices(configuration);
        return services.BuildServiceProvider();
    }

    private sealed class ThrowingDatabaseMigrationRunner : IDatabaseMigrationRunner
    {
        public Task MigrateUpAsync(CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Migration failed.");
        }
    }
}
