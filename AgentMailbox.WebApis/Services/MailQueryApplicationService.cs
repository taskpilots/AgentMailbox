using AgentMailbox.Core.Contracts;
using AgentMailbox.Repositories.Abstractions;

namespace AgentMailbox.WebApis.Services;

public sealed class MailQueryApplicationService : IMailQueryApplicationService
{
    private readonly IMailMessageRepository _mailMessageRepository;

    public MailQueryApplicationService(IMailMessageRepository mailMessageRepository)
    {
        _mailMessageRepository = mailMessageRepository;
    }

    public async Task<IReadOnlyList<MailSummaryDto>> ListMessagesAsync(string? mailboxAddress, CancellationToken cancellationToken = default)
    {
        var messages = await _mailMessageRepository.ListAsync(mailboxAddress, cancellationToken);
        return messages.Select(static message => message.ToSummaryDto()).ToList();
    }

    public async Task<MailDetailDto?> GetMessageAsync(string messageId, CancellationToken cancellationToken = default)
    {
        var message = await _mailMessageRepository.GetByIdAsync(messageId, cancellationToken);
        return message?.ToDetailDto();
    }

    public async Task<MailThreadDto?> GetThreadAsync(string threadId, CancellationToken cancellationToken = default)
    {
        var thread = await _mailMessageRepository.GetThreadAsync(threadId, cancellationToken);
        if (thread is null)
        {
            return null;
        }

        var messages = await _mailMessageRepository.ListThreadMessagesAsync(threadId, cancellationToken);
        return new MailThreadDto
        {
            ThreadId = thread.ThreadId,
            MailboxId = thread.MailboxId,
            Subject = thread.Subject,
            CreatedAtUtc = thread.CreatedAtUtc,
            Messages = messages.Select(static message => message.ToDetailDto()).ToList()
        };
    }
}
