using AgentMailbox.Core.Contracts;
using AgentMailbox.WebApis.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgentMailbox.WebApis.Controllers;

[ApiController]
[Route("api/messages")]
public sealed class MessagesController : ControllerBase
{
    private readonly IMailQueryApplicationService _mailQueryApplicationService;
    private readonly IMailSendApplicationService _mailSendApplicationService;

    public MessagesController(
        IMailQueryApplicationService mailQueryApplicationService,
        IMailSendApplicationService mailSendApplicationService)
    {
        _mailQueryApplicationService = mailQueryApplicationService;
        _mailSendApplicationService = mailSendApplicationService;
    }

    [HttpGet]
    public async Task<IActionResult> ListMessages([FromQuery] string? mailboxAddress, CancellationToken cancellationToken)
    {
        var response = await _mailQueryApplicationService.ListMessagesAsync(mailboxAddress, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{messageId}")]
    public async Task<IActionResult> GetMessage(string messageId, CancellationToken cancellationToken)
    {
        var response = await _mailQueryApplicationService.GetMessageAsync(messageId, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMail([FromBody] SendMailRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _mailSendApplicationService.SendMailAsync(request, cancellationToken);
            return Accepted($"/api/messages/{response.MessageId}", response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Send request is invalid",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    [HttpPost("{messageId}/reply")]
    public async Task<IActionResult> Reply(string messageId, [FromBody] ReplyMailRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _mailSendApplicationService.ReplyAsync(messageId, request, cancellationToken);
            return response is null ? NotFound() : Accepted($"/api/messages/{response.MessageId}", response);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Reply request is invalid",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }
}
