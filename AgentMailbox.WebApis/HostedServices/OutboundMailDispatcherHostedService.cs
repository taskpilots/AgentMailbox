using AgentMailbox.Core.Options;
using AgentMailbox.Repositories.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgentMailbox.WebApis.HostedServices;

public sealed class OutboundMailDispatcherHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly OutboundSmtpOptions _smtpOptions;
    private readonly RetryOptions _retryOptions;
    private readonly ILogger<OutboundMailDispatcherHostedService> _logger;

    public OutboundMailDispatcherHostedService(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<OutboundSmtpOptions> smtpOptions,
        IOptions<RetryOptions> retryOptions,
        ILogger<OutboundMailDispatcherHostedService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _smtpOptions = smtpOptions.Value;
        _retryOptions = retryOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_smtpOptions.Enabled)
        {
            _logger.LogInformation("Outbound mail dispatcher is disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var outboundTaskRepository = scope.ServiceProvider.GetRequiredService<IOutboundTaskRepository>();
                var pendingTasks = await outboundTaskRepository.ListPendingAsync(20, stoppingToken);

                if (pendingTasks.Count > 0)
                {
                    _logger.LogInformation(
                        "Found {TaskCount} pending outbound tasks. Placeholder dispatcher does not send yet.",
                        pendingTasks.Count);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Outbound dispatcher placeholder loop failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Max(5, _retryOptions.InitialDelaySeconds)), stoppingToken);
        }
    }
}
