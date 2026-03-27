using AgentMailbox.Core.Models;

namespace AgentMailbox.Repositories.Abstractions;

public interface IOutboundTaskRepository
{
    Task<OutboundTaskRecord> EnqueueAsync(OutboundTaskRecord outboundTask, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutboundTaskRecord>> ListPendingAsync(int take, CancellationToken cancellationToken = default);

    Task MarkAsSendingAsync(string outboundTaskId, int attemptCount, DateTimeOffset updatedAtUtc, CancellationToken cancellationToken = default);

    Task MarkAsSentAsync(string outboundTaskId, DateTimeOffset sentAtUtc, CancellationToken cancellationToken = default);

    Task MarkAsFailedAsync(
        string outboundTaskId,
        int attemptCount,
        string? lastError,
        DateTimeOffset nextAttemptAtUtc,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken = default);
}
