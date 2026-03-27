namespace AgentMailbox.Core.Models;

public sealed class MailboxRecord
{
    public string MailboxId { get; init; } = string.Empty;

    public string Address { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }
}

public sealed class MailThreadRecord
{
    public string ThreadId { get; init; } = string.Empty;

    public string MailboxId { get; init; } = string.Empty;

    public string Subject { get; init; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; init; }
}

public sealed class MailMessageRecord
{
    public string MessageId { get; init; } = string.Empty;

    public string ThreadId { get; init; } = string.Empty;

    public string MailboxId { get; init; } = string.Empty;

    public string Direction { get; init; } = string.Empty;

    public string Subject { get; init; } = string.Empty;

    public string FromAddress { get; init; } = string.Empty;

    public string ToAddress { get; init; } = string.Empty;

    public string BodyText { get; init; } = string.Empty;

    public string? RawContentPath { get; init; }

    public InboundMailStatus? InboundStatus { get; init; }

    public OutboundMailStatus? OutboundStatus { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? ReceivedAtUtc { get; init; }

    public DateTimeOffset? SentAtUtc { get; init; }

    public string? InReplyToMessageId { get; init; }
}

public sealed class OutboundTaskRecord
{
    public string OutboundTaskId { get; init; } = string.Empty;

    public string MessageId { get; init; } = string.Empty;

    public OutboundMailStatus Status { get; init; }

    public int AttemptCount { get; init; }

    public DateTimeOffset NextAttemptAtUtc { get; init; }

    public string? LastError { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset UpdatedAtUtc { get; init; }
}

public enum InboundMailStatus
{
    Unprocessed = 0,
    Processing = 1,
    Processed = 2,
    Failed = 3,
    Ignored = 4
}

public enum OutboundMailStatus
{
    Pending = 0,
    Sending = 1,
    Sent = 2,
    Failed = 3
}
