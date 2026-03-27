using AgentMailbox.Core.Contracts;
using AgentMailbox.Core.Models;
using AgentMailbox.Repositories.Abstractions;

namespace AgentMailbox.WebApis.Services;

public sealed class MailboxApplicationService : IMailboxApplicationService
{
    private readonly IMailboxRepository _mailboxRepository;

    public MailboxApplicationService(IMailboxRepository mailboxRepository)
    {
        _mailboxRepository = mailboxRepository;
    }

    public async Task<MailboxDto> CreateMailboxAsync(CreateMailboxRequest request, CancellationToken cancellationToken = default)
    {
        var existingMailbox = await _mailboxRepository.GetByAddressAsync(request.Address, cancellationToken);
        if (existingMailbox is not null)
        {
            throw new InvalidOperationException($"Mailbox '{request.Address}' already exists.");
        }

        var mailbox = new MailboxRecord
        {
            MailboxId = Guid.NewGuid().ToString("N"),
            Address = request.Address.Trim().ToLowerInvariant(),
            DisplayName = request.DisplayName.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var created = await _mailboxRepository.CreateAsync(mailbox, cancellationToken);
        return created.ToDto();
    }

    public async Task<IReadOnlyList<MailboxDto>> ListMailboxesAsync(CancellationToken cancellationToken = default)
    {
        var mailboxes = await _mailboxRepository.ListAsync(cancellationToken);
        return mailboxes.Select(static mailbox => mailbox.ToDto()).ToList();
    }
}
