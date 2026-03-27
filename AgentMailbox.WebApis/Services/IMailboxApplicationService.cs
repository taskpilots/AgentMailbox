using AgentMailbox.Core.Contracts;

namespace AgentMailbox.WebApis.Services;

public interface IMailboxApplicationService
{
    Task<MailboxDto> CreateMailboxAsync(CreateMailboxRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MailboxDto>> ListMailboxesAsync(CancellationToken cancellationToken = default);
}
