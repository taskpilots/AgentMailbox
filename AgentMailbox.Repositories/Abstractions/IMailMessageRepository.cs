using AgentMailbox.Core.Models;

namespace AgentMailbox.Repositories.Abstractions;

public interface IMailMessageRepository
{
    Task<IReadOnlyList<MailMessageRecord>> ListAsync(string? mailboxAddress = null, CancellationToken cancellationToken = default);

    Task<MailMessageRecord?> GetByIdAsync(string messageId, CancellationToken cancellationToken = default);

    Task<MailThreadRecord?> GetThreadAsync(string threadId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MailMessageRecord>> ListThreadMessagesAsync(string threadId, CancellationToken cancellationToken = default);

    Task<MailMessageRecord> CreateOutboundThreadMessageAsync(
        MailThreadRecord thread,
        MailMessageRecord message,
        CancellationToken cancellationToken = default);

    Task<MailMessageRecord> CreateOutboundReplyMessageAsync(
        MailMessageRecord message,
        CancellationToken cancellationToken = default);
}
