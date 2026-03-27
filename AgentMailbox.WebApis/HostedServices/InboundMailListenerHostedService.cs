using AgentMailbox.Core.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Sockets;

namespace AgentMailbox.WebApis.HostedServices;

public sealed class InboundMailListenerHostedService : BackgroundService
{
    private readonly InboundListenerOptions _options;
    private readonly ILogger<InboundMailListenerHostedService> _logger;

    public InboundMailListenerHostedService(
        IOptions<InboundListenerOptions> options,
        ILogger<InboundMailListenerHostedService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Inbound mail listener is disabled.");
            return;
        }

        var ipAddress = IPAddress.TryParse(_options.Host, out var parsedIpAddress)
            ? parsedIpAddress
            : IPAddress.Loopback;

        using var listener = new TcpListener(ipAddress, _options.Port);
        listener.Start();
        _logger.LogInformation("Inbound mail listener started on {Host}:{Port}.", _options.Host, _options.Port);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var acceptTask = listener.AcceptTcpClientAsync(stoppingToken).AsTask();
                var completedTask = await Task.WhenAny(acceptTask, Task.Delay(TimeSpan.FromSeconds(1), stoppingToken));
                if (completedTask == acceptTask)
                {
                    using var client = await acceptTask;
                    _logger.LogDebug("Accepted placeholder inbound mail connection from {RemoteEndPoint}.", client.Client.RemoteEndPoint);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            listener.Stop();
            _logger.LogInformation("Inbound mail listener stopped.");
        }
    }
}
