using AgentMailbox.Core.Contracts;

namespace AgentMailbox.WebApis.Services;

public interface IMailSendApplicationService
{
    Task<MailDetailDto> SendMailAsync(SendMailRequest request, CancellationToken cancellationToken = default);

    Task<MailDetailDto?> ReplyAsync(string repliedMessageId, ReplyMailRequest request, CancellationToken cancellationToken = default);
}
