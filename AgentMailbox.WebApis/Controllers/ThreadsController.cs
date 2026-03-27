using AgentMailbox.WebApis.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgentMailbox.WebApis.Controllers;

[ApiController]
[Route("api/threads")]
public sealed class ThreadsController : ControllerBase
{
    private readonly IMailQueryApplicationService _mailQueryApplicationService;

    public ThreadsController(IMailQueryApplicationService mailQueryApplicationService)
    {
        _mailQueryApplicationService = mailQueryApplicationService;
    }

    [HttpGet("{threadId}")]
    public async Task<IActionResult> GetThread(string threadId, CancellationToken cancellationToken)
    {
        var response = await _mailQueryApplicationService.GetThreadAsync(threadId, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }
}
