using AgentMailbox.Core.Models;

namespace AgentMailbox.Repositories.Abstractions;

public interface IMailboxRepository
{
    Task<MailboxRecord> CreateAsync(MailboxRecord mailbox, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MailboxRecord>> ListAsync(CancellationToken cancellationToken = default);

    Task<MailboxRecord?> GetByAddressAsync(string address, CancellationToken cancellationToken = default);
}
