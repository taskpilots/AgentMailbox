using AgentMailbox.Core.Contracts;
using AgentMailbox.Core.Models;
using AgentMailbox.Core.Options;
using AgentMailbox.Repositories.Abstractions;
using Microsoft.Extensions.Options;

namespace AgentMailbox.WebApis.Services;

public sealed class MailSendApplicationService : IMailSendApplicationService
{
    private readonly IMailboxRepository _mailboxRepository;
    private readonly IMailMessageRepository _mailMessageRepository;
    private readonly IOutboundTaskRepository _outboundTaskRepository;
    private readonly RetryOptions _retryOptions;

    public MailSendApplicationService(
        IMailboxRepository mailboxRepository,
        IMailMessageRepository mailMessageRepository,
        IOutboundTaskRepository outboundTaskRepository,
        IOptions<RetryOptions> retryOptions)
    {
        _mailboxRepository = mailboxRepository;
        _mailMessageRepository = mailMessageRepository;
        _outboundTaskRepository = outboundTaskRepository;
        _retryOptions = retryOptions.Value;
    }

    public async Task<MailDetailDto> SendMailAsync(SendMailRequest request, CancellationToken cancellationToken = default)
    {
        var mailbox = await RequireMailboxAsync(request.MailboxAddress, cancellationToken);
        var nowUtc = DateTimeOffset.UtcNow;

        var thread = new MailThreadRecord
        {
            ThreadId = Guid.NewGuid().ToString("N"),
            MailboxId = mailbox.MailboxId,
            Subject = request.Subject.Trim(),
            CreatedAtUtc = nowUtc
        };

        var message = new MailMessageRecord
        {
            MessageId = Guid.NewGuid().ToString("N"),
            ThreadId = thread.ThreadId,
            MailboxId = mailbox.MailboxId,
            Direction = "outbound",
            Subject = request.Subject.Trim(),
            FromAddress = mailbox.Address,
            ToAddress = request.ToAddress.Trim().ToLowerInvariant(),
            BodyText = request.BodyText.Trim(),
            OutboundStatus = OutboundMailStatus.Pending,
            CreatedAtUtc = nowUtc
        };

        await _mailMessageRepository.CreateOutboundThreadMessageAsync(thread, message, cancellationToken);
        await _outboundTaskRepository.EnqueueAsync(CreatePendingTask(message.MessageId, nowUtc), cancellationToken);
        return message.ToDetailDto();
    }

    public async Task<MailDetailDto?> ReplyAsync(string repliedMessageId, ReplyMailRequest request, CancellationToken cancellationToken = default)
    {
        var parentMessage = await _mailMessageRepository.GetByIdAsync(repliedMessageId, cancellationToken);
        if (parentMessage is null)
        {
            return null;
        }

        var mailbox = await RequireMailboxAsync(request.MailboxAddress, cancellationToken);
        var thread = await _mailMessageRepository.GetThreadAsync(parentMessage.ThreadId, cancellationToken);
        if (thread is null)
        {
            throw new InvalidOperationException($"Thread '{parentMessage.ThreadId}' was not found.");
        }

        if (!string.Equals(thread.MailboxId, mailbox.MailboxId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Mailbox '{mailbox.Address}' cannot reply to thread '{parentMessage.ThreadId}'.");
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var subject = request.Subject?.Trim();

        if (string.IsNullOrWhiteSpace(subject))
        {
            subject = parentMessage.Subject.StartsWith("Re:", StringComparison.OrdinalIgnoreCase)
                ? parentMessage.Subject
                : $"Re: {parentMessage.Subject}";
        }

        var message = new MailMessageRecord
        {
            MessageId = Guid.NewGuid().ToString("N"),
            ThreadId = parentMessage.ThreadId,
            MailboxId = mailbox.MailboxId,
            Direction = "outbound",
            Subject = subject,
            FromAddress = mailbox.Address,
            ToAddress = parentMessage.FromAddress,
            BodyText = request.BodyText.Trim(),
            OutboundStatus = OutboundMailStatus.Pending,
            CreatedAtUtc = nowUtc,
            InReplyToMessageId = parentMessage.MessageId
        };

        await _mailMessageRepository.CreateOutboundReplyMessageAsync(message, cancellationToken);
        await _outboundTaskRepository.EnqueueAsync(CreatePendingTask(message.MessageId, nowUtc), cancellationToken);
        return message.ToDetailDto();
    }

    private async Task<MailboxRecord> RequireMailboxAsync(string mailboxAddress, CancellationToken cancellationToken)
    {
        var normalizedAddress = mailboxAddress.Trim().ToLowerInvariant();
        var mailbox = await _mailboxRepository.GetByAddressAsync(normalizedAddress, cancellationToken);
        if (mailbox is null)
        {
            throw new InvalidOperationException($"Mailbox '{normalizedAddress}' was not found.");
        }

        return mailbox;
    }

    private OutboundTaskRecord CreatePendingTask(string messageId, DateTimeOffset nowUtc)
    {
        return new OutboundTaskRecord
        {
            OutboundTaskId = Guid.NewGuid().ToString("N"),
            MessageId = messageId,
            Status = OutboundMailStatus.Pending,
            AttemptCount = 0,
            NextAttemptAtUtc = nowUtc.AddSeconds(Math.Max(0, _retryOptions.InitialDelaySeconds)),
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };
    }
}
