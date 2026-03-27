using AgentMailbox.Core.Contracts;
using AgentMailbox.WebApis.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgentMailbox.WebApis.Controllers;

[ApiController]
[Route("api/mailboxes")]
public sealed class MailboxesController : ControllerBase
{
    private readonly IMailboxApplicationService _mailboxApplicationService;

    public MailboxesController(IMailboxApplicationService mailboxApplicationService)
    {
        _mailboxApplicationService = mailboxApplicationService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateMailbox([FromBody] CreateMailboxRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var mailbox = await _mailboxApplicationService.CreateMailboxAsync(request, cancellationToken);
            return Created($"/api/mailboxes/{mailbox.MailboxId}", mailbox);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Mailbox already exists",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ListMailboxes(CancellationToken cancellationToken)
    {
        var response = await _mailboxApplicationService.ListMailboxesAsync(cancellationToken);
        return Ok(response);
    }
}
