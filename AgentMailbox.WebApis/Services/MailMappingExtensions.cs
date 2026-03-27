using AgentMailbox.Core.Contracts;
using AgentMailbox.Core.Models;

namespace AgentMailbox.WebApis.Services;

internal static class MailMappingExtensions
{
    public static MailboxDto ToDto(this MailboxRecord mailbox)
    {
        return new MailboxDto
        {
            MailboxId = mailbox.MailboxId,
            Address = mailbox.Address,
            DisplayName = mailbox.DisplayName,
            IsActive = mailbox.IsActive,
            CreatedAtUtc = mailbox.CreatedAtUtc
        };
    }

    public static MailSummaryDto ToSummaryDto(this MailMessageRecord message)
    {
        return new MailSummaryDto
        {
            MessageId = message.MessageId,
            ThreadId = message.ThreadId,
            MailboxId = message.MailboxId,
            Direction = message.Direction,
            Subject = message.Subject,
            FromAddress = message.FromAddress,
            ToAddress = message.ToAddress,
            InboundStatus = message.InboundStatus,
            OutboundStatus = message.OutboundStatus,
            CreatedAtUtc = message.CreatedAtUtc,
            ReceivedAtUtc = message.ReceivedAtUtc,
            SentAtUtc = message.SentAtUtc
        };
    }

    public static MailDetailDto ToDetailDto(this MailMessageRecord message)
    {
        return new MailDetailDto
        {
            MessageId = message.MessageId,
            ThreadId = message.ThreadId,
            MailboxId = message.MailboxId,
            Direction = message.Direction,
            Subject = message.Subject,
            FromAddress = message.FromAddress,
            ToAddress = message.ToAddress,
            InboundStatus = message.InboundStatus,
            OutboundStatus = message.OutboundStatus,
            CreatedAtUtc = message.CreatedAtUtc,
            ReceivedAtUtc = message.ReceivedAtUtc,
            SentAtUtc = message.SentAtUtc,
            BodyText = message.BodyText,
            RawContentPath = message.RawContentPath,
            InReplyToMessageId = message.InReplyToMessageId
        };
    }
}
