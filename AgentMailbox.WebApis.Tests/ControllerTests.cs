using AgentMailbox.Core.Contracts;
using AgentMailbox.WebApis.Controllers;
using AgentMailbox.WebApis.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AgentMailbox.WebApis.Tests;

public sealed class ControllerTests
{
    [Fact]
    public async Task MailboxesController_CreateMailbox_ShouldReturnCreatedResult()
    {
        var service = new FakeMailboxApplicationService();
        var controller = new MailboxesController(service);

        var result = await controller.CreateMailbox(
            new CreateMailboxRequest
            {
                Address = "support-agent@local.ai",
                DisplayName = "Support Agent"
            },
            CancellationToken.None);

        var createdResult = Assert.IsType<CreatedResult>(result);
        var payload = Assert.IsType<MailboxDto>(createdResult.Value);
        Assert.Equal("support-agent@local.ai", payload.Address);
    }

    [Fact]
    public async Task MessagesController_GetMessage_ShouldReturnOk_WhenMessageExists()
    {
        var controller = new MessagesController(new FakeMailQueryApplicationService(), new FakeMailSendApplicationService());

        var result = await controller.GetMessage("message-1", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<MailDetailDto>(okResult.Value);
        Assert.Equal("message-1", payload.MessageId);
    }

    [Fact]
    public async Task MessagesController_SendMail_ShouldReturnBadRequest_WhenMailboxIsMissing()
    {
        var controller = new MessagesController(new FakeMailQueryApplicationService(), new FakeMailSendApplicationService(throwOnSend: true));

        var result = await controller.SendMail(
            new SendMailRequest
            {
                MailboxAddress = "missing@local.ai",
                ToAddress = "user@example.com",
                Subject = "Hi",
                BodyText = "Hello"
            },
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
    }

    [Fact]
    public async Task MessagesController_Reply_ShouldReturnBadRequest_WhenReplyIsInvalid()
    {
        var controller = new MessagesController(
            new FakeMailQueryApplicationService(),
            new FakeMailSendApplicationService(throwOnReply: true));

        var result = await controller.Reply(
            "message-1",
            new ReplyMailRequest
            {
                MailboxAddress = "other-agent@local.ai",
                BodyText = "Hello again"
            },
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
    }

    private sealed class FakeMailboxApplicationService : IMailboxApplicationService
    {
        public Task<MailboxDto> CreateMailboxAsync(CreateMailboxRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new MailboxDto
            {
                MailboxId = "mailbox-1",
                Address = request.Address,
                DisplayName = request.DisplayName,
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }

        public Task<IReadOnlyList<MailboxDto>> ListMailboxesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<MailboxDto>>(Array.Empty<MailboxDto>());
        }
    }

    private sealed class FakeMailQueryApplicationService : IMailQueryApplicationService
    {
        public Task<MailDetailDto?> GetMessageAsync(string messageId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<MailDetailDto?>(new MailDetailDto
            {
                MessageId = messageId,
                ThreadId = "thread-1",
                MailboxId = "mailbox-1",
                Direction = "outbound",
                Subject = "Subject",
                FromAddress = "support-agent@local.ai",
                ToAddress = "user@example.com",
                BodyText = "Body",
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }

        public Task<MailThreadDto?> GetThreadAsync(string threadId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<MailThreadDto?>(null);
        }

        public Task<IReadOnlyList<MailSummaryDto>> ListMessagesAsync(string? mailboxAddress, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<MailSummaryDto>>(Array.Empty<MailSummaryDto>());
        }
    }

    private sealed class FakeMailSendApplicationService : IMailSendApplicationService
    {
        private readonly bool _throwOnSend;
        private readonly bool _throwOnReply;

        public FakeMailSendApplicationService(bool throwOnSend = false, bool throwOnReply = false)
        {
            _throwOnSend = throwOnSend;
            _throwOnReply = throwOnReply;
        }

        public Task<MailDetailDto?> ReplyAsync(string repliedMessageId, ReplyMailRequest request, CancellationToken cancellationToken = default)
        {
            if (_throwOnReply)
            {
                throw new InvalidOperationException("Reply is invalid.");
            }

            return Task.FromResult<MailDetailDto?>(null);
        }

        public Task<MailDetailDto> SendMailAsync(SendMailRequest request, CancellationToken cancellationToken = default)
        {
            if (_throwOnSend)
            {
                throw new InvalidOperationException("Mailbox was not found.");
            }

            return Task.FromResult(new MailDetailDto
            {
                MessageId = "message-1",
                ThreadId = "thread-1",
                MailboxId = "mailbox-1",
                Direction = "outbound",
                Subject = request.Subject,
                FromAddress = request.MailboxAddress,
                ToAddress = request.ToAddress,
                BodyText = request.BodyText,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }
    }
}
