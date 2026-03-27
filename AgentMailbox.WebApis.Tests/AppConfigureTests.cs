using AgentMailbox.Core.Options;
using AgentMailbox.Repositories.Abstractions;
using AgentMailbox.WebApis;
using FluentMigrator.Runner;
using AgentMailbox.WebApis.HostedServices;
using AgentMailbox.WebApis.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AgentMailbox.WebApis.Tests;

public sealed class AppConfigureTests
{
    [Fact]
    public void ConfigureServices_ShouldRegisterCoreServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:DatabasePath"] = "App_Data/test.db"
            })
            .Build();

        services.AddAgentMailboxApplicationServices(configuration);

        var serviceProvider = services.BuildServiceProvider();
        Assert.NotNull(serviceProvider.GetService<IMailboxApplicationService>());
        Assert.NotNull(serviceProvider.GetService<IMailQueryApplicationService>());
        Assert.NotNull(serviceProvider.GetService<IMailSendApplicationService>());
        Assert.NotNull(serviceProvider.GetService<IMailboxRepository>());
        Assert.NotNull(serviceProvider.GetService<IMigrationRunner>());
        var hostedServices = serviceProvider.GetServices<IHostedService>().ToList();
        Assert.Contains(hostedServices, service => service is DatabaseMigrationHostedService);
        Assert.Contains(hostedServices, service => service is InboundMailListenerHostedService);
        Assert.Contains(hostedServices, service => service is OutboundMailDispatcherHostedService);
    }

    [Fact]
    public async Task HostedServices_ShouldStartAndStop_WhenDisabledByDefault()
    {
        using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(_ => { });

        var inboundService = new InboundMailListenerHostedService(
            Microsoft.Extensions.Options.Options.Create(new InboundListenerOptions { Enabled = false }),
            loggerFactory.CreateLogger<InboundMailListenerHostedService>());

        var outboundService = new OutboundMailDispatcherHostedService(
            new ServiceCollection()
                .AddLogging()
                .AddSingleton<IOutboundTaskRepository, FakeOutboundTaskRepository>()
                .BuildServiceProvider()
                .GetRequiredService<IServiceScopeFactory>(),
            Microsoft.Extensions.Options.Options.Create(new OutboundSmtpOptions { Enabled = false }),
            Microsoft.Extensions.Options.Options.Create(new RetryOptions()),
            loggerFactory.CreateLogger<OutboundMailDispatcherHostedService>());

        await inboundService.StartAsync(CancellationToken.None);
        await outboundService.StartAsync(CancellationToken.None);
        await inboundService.StopAsync(CancellationToken.None);
        await outboundService.StopAsync(CancellationToken.None);
    }
    private sealed class FakeOutboundTaskRepository : IOutboundTaskRepository
    {
        public Task<AgentMailbox.Core.Models.OutboundTaskRecord> EnqueueAsync(
            AgentMailbox.Core.Models.OutboundTaskRecord outboundTask,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(outboundTask);
        }

        public Task<IReadOnlyList<AgentMailbox.Core.Models.OutboundTaskRecord>> ListPendingAsync(int take, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AgentMailbox.Core.Models.OutboundTaskRecord>>(Array.Empty<AgentMailbox.Core.Models.OutboundTaskRecord>());
        }

        public Task MarkAsFailedAsync(string outboundTaskId, int attemptCount, string? lastError, DateTimeOffset nextAttemptAtUtc, DateTimeOffset updatedAtUtc, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task MarkAsSendingAsync(string outboundTaskId, int attemptCount, DateTimeOffset updatedAtUtc, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task MarkAsSentAsync(string outboundTaskId, DateTimeOffset sentAtUtc, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
