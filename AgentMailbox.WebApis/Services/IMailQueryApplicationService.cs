using AgentMailbox.Core.Contracts;

namespace AgentMailbox.WebApis.Services;

public interface IMailQueryApplicationService
{
    Task<IReadOnlyList<MailSummaryDto>> ListMessagesAsync(string? mailboxAddress, CancellationToken cancellationToken = default);

    Task<MailDetailDto?> GetMessageAsync(string messageId, CancellationToken cancellationToken = default);

    Task<MailThreadDto?> GetThreadAsync(string threadId, CancellationToken cancellationToken = default);
}
