using System.ComponentModel.DataAnnotations;
using AgentMailbox.Core.Models;

namespace AgentMailbox.Core.Contracts;

public sealed class CreateMailboxRequest
{
    [Required]
    [StringLength(120, MinimumLength = 3)]
    [EmailAddress]
    public string Address { get; set; } = string.Empty;

    [Required]
    [StringLength(80, MinimumLength = 1)]
    public string DisplayName { get; set; } = string.Empty;
}

public sealed class MailboxDto
{
    public string MailboxId { get; init; } = string.Empty;

    public string Address { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }
}

public sealed class SendMailRequest
{
    [Required]
    [StringLength(120, MinimumLength = 3)]
    [EmailAddress]
    public string MailboxAddress { get; set; } = string.Empty;

    [Required]
    [StringLength(120, MinimumLength = 3)]
    [EmailAddress]
    public string ToAddress { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [StringLength(20000, MinimumLength = 1)]
    public string BodyText { get; set; } = string.Empty;
}

public sealed class ReplyMailRequest
{
    [Required]
    [StringLength(120, MinimumLength = 3)]
    [EmailAddress]
    public string MailboxAddress { get; set; } = string.Empty;

    [StringLength(200, MinimumLength = 1)]
    public string? Subject { get; set; }

    [Required]
    [StringLength(20000, MinimumLength = 1)]
    public string BodyText { get; set; } = string.Empty;
}

public class MailSummaryDto
{
    public string MessageId { get; init; } = string.Empty;

    public string ThreadId { get; init; } = string.Empty;

    public string MailboxId { get; init; } = string.Empty;

    public string Direction { get; init; } = string.Empty;

    public string Subject { get; init; } = string.Empty;

    public string FromAddress { get; init; } = string.Empty;

    public string ToAddress { get; init; } = string.Empty;

    public InboundMailStatus? InboundStatus { get; init; }

    public OutboundMailStatus? OutboundStatus { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? ReceivedAtUtc { get; init; }

    public DateTimeOffset? SentAtUtc { get; init; }
}

public sealed class MailDetailDto : MailSummaryDto
{
    public string BodyText { get; init; } = string.Empty;

    public string? RawContentPath { get; init; }

    public string? InReplyToMessageId { get; init; }
}

public sealed class MailThreadDto
{
    public string ThreadId { get; init; } = string.Empty;

    public string MailboxId { get; init; } = string.Empty;

    public string Subject { get; init; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; init; }

    public IReadOnlyList<MailDetailDto> Messages { get; init; } = Array.Empty<MailDetailDto>();
}

public sealed class OutboundTaskDto
{
    public string OutboundTaskId { get; init; } = string.Empty;

    public string MessageId { get; init; } = string.Empty;

    public OutboundMailStatus Status { get; init; }

    public int AttemptCount { get; init; }

    public DateTimeOffset NextAttemptAtUtc { get; init; }

    public string? LastError { get; init; }
}
