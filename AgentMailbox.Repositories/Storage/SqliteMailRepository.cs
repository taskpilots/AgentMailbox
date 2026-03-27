using AgentMailbox.Core.Models;
using AgentMailbox.Repositories.Abstractions;
using Dapper;

namespace AgentMailbox.Repositories.Storage;

public sealed class SqliteMailRepository :
    IMailboxRepository,
    IMailMessageRepository,
    IOutboundTaskRepository
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    public SqliteMailRepository(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<MailboxRecord> CreateAsync(MailboxRecord mailbox, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            INSERT INTO mailboxes (mailbox_id, address, display_name, is_active, created_at_utc)
            VALUES (@MailboxId, @Address, @DisplayName, @IsActive, @CreatedAtUtc);
            """;

        await connection.ExecuteAsync(new CommandDefinition(sql, mailbox, cancellationToken: cancellationToken));
        return mailbox;
    }

    public async Task<IReadOnlyList<MailboxRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT mailbox_id, address, display_name, is_active, created_at_utc
            FROM mailboxes
            ORDER BY created_at_utc DESC;
            """;

        var records = await connection.QueryAsync<MailboxRecord>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return records.ToList();
    }

    public async Task<MailboxRecord?> GetByAddressAsync(string address, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT mailbox_id, address, display_name, is_active, created_at_utc
            FROM mailboxes
            WHERE address = @Address
            LIMIT 1;
            """;

        return await connection.QuerySingleOrDefaultAsync<MailboxRecord>(
            new CommandDefinition(sql, new { Address = address }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<MailMessageRecord>> ListAsync(string? mailboxAddress = null, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT
                mm.message_id,
                mm.thread_id,
                mm.mailbox_id,
                mm.direction,
                mm.subject,
                mm.from_address,
                mm.to_address,
                mm.body_text,
                mm.raw_content_path,
                mm.inbound_status,
                mm.outbound_status,
                mm.created_at_utc,
                mm.received_at_utc,
                mm.sent_at_utc,
                mm.in_reply_to_message_id
            FROM mail_messages mm
            INNER JOIN mailboxes mb ON mb.mailbox_id = mm.mailbox_id
            WHERE (@MailboxAddress IS NULL OR mb.address = @MailboxAddress)
            ORDER BY mm.created_at_utc DESC;
            """;

        var records = await connection.QueryAsync<MailMessageRecord>(
            new CommandDefinition(sql, new { MailboxAddress = mailboxAddress }, cancellationToken: cancellationToken));
        return records.ToList();
    }

    public async Task<MailMessageRecord?> GetByIdAsync(string messageId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT
                message_id,
                thread_id,
                mailbox_id,
                direction,
                subject,
                from_address,
                to_address,
                body_text,
                raw_content_path,
                inbound_status,
                outbound_status,
                created_at_utc,
                received_at_utc,
                sent_at_utc,
                in_reply_to_message_id
            FROM mail_messages
            WHERE message_id = @MessageId
            LIMIT 1;
            """;

        return await connection.QuerySingleOrDefaultAsync<MailMessageRecord>(
            new CommandDefinition(sql, new { MessageId = messageId }, cancellationToken: cancellationToken));
    }

    public async Task<MailThreadRecord?> GetThreadAsync(string threadId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT thread_id, subject, mailbox_id, created_at_utc
            FROM mail_threads
            WHERE thread_id = @ThreadId
            LIMIT 1;
            """;

        return await connection.QuerySingleOrDefaultAsync<MailThreadRecord>(
            new CommandDefinition(sql, new { ThreadId = threadId }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<MailMessageRecord>> ListThreadMessagesAsync(string threadId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT
                message_id,
                thread_id,
                mailbox_id,
                direction,
                subject,
                from_address,
                to_address,
                body_text,
                raw_content_path,
                inbound_status,
                outbound_status,
                created_at_utc,
                received_at_utc,
                sent_at_utc,
                in_reply_to_message_id
            FROM mail_messages
            WHERE thread_id = @ThreadId
            ORDER BY created_at_utc ASC;
            """;

        var records = await connection.QueryAsync<MailMessageRecord>(
            new CommandDefinition(sql, new { ThreadId = threadId }, cancellationToken: cancellationToken));
        return records.ToList();
    }

    public async Task<MailMessageRecord> CreateOutboundThreadMessageAsync(
        MailThreadRecord thread,
        MailMessageRecord message,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string insertThreadSql = """
            INSERT INTO mail_threads (thread_id, subject, mailbox_id, created_at_utc)
            VALUES (@ThreadId, @Subject, @MailboxId, @CreatedAtUtc);
            """;

        const string insertMessageSql = """
            INSERT INTO mail_messages (
                message_id,
                thread_id,
                mailbox_id,
                direction,
                subject,
                from_address,
                to_address,
                body_text,
                raw_content_path,
                inbound_status,
                outbound_status,
                created_at_utc,
                received_at_utc,
                sent_at_utc,
                in_reply_to_message_id)
            VALUES (
                @MessageId,
                @ThreadId,
                @MailboxId,
                @Direction,
                @Subject,
                @FromAddress,
                @ToAddress,
                @BodyText,
                @RawContentPath,
                @InboundStatus,
                @OutboundStatus,
                @CreatedAtUtc,
                @ReceivedAtUtc,
                @SentAtUtc,
                @InReplyToMessageId);
            """;

        await connection.ExecuteAsync(new CommandDefinition(insertThreadSql, thread, transaction, cancellationToken: cancellationToken));
        await connection.ExecuteAsync(new CommandDefinition(
            insertMessageSql,
            ToMessageParameters(message),
            transaction,
            cancellationToken: cancellationToken));
        await transaction.CommitAsync(cancellationToken);
        return message;
    }

    public async Task<MailMessageRecord> CreateOutboundReplyMessageAsync(
        MailMessageRecord message,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string getThreadSql = """
            SELECT thread_id, subject, mailbox_id, created_at_utc
            FROM mail_threads
            WHERE thread_id = @ThreadId
            LIMIT 1;
            """;

        const string insertMessageSql = """
            INSERT INTO mail_messages (
                message_id,
                thread_id,
                mailbox_id,
                direction,
                subject,
                from_address,
                to_address,
                body_text,
                raw_content_path,
                inbound_status,
                outbound_status,
                created_at_utc,
                received_at_utc,
                sent_at_utc,
                in_reply_to_message_id)
            VALUES (
                @MessageId,
                @ThreadId,
                @MailboxId,
                @Direction,
                @Subject,
                @FromAddress,
                @ToAddress,
                @BodyText,
                @RawContentPath,
                @InboundStatus,
                @OutboundStatus,
                @CreatedAtUtc,
                @ReceivedAtUtc,
                @SentAtUtc,
                @InReplyToMessageId);
            """;

        var thread = await connection.QuerySingleOrDefaultAsync<MailThreadRecord>(
            new CommandDefinition(getThreadSql, new { message.ThreadId }, transaction, cancellationToken: cancellationToken));

        if (thread is null)
        {
            throw new InvalidOperationException($"Thread '{message.ThreadId}' was not found.");
        }

        if (!string.Equals(thread.MailboxId, message.MailboxId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Thread '{message.ThreadId}' does not belong to mailbox '{message.MailboxId}'.");
        }

        await connection.ExecuteAsync(new CommandDefinition(
            insertMessageSql,
            ToMessageParameters(message),
            transaction,
            cancellationToken: cancellationToken));
        await transaction.CommitAsync(cancellationToken);
        return message;
    }

    public async Task<OutboundTaskRecord> EnqueueAsync(OutboundTaskRecord outboundTask, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            INSERT INTO outbound_tasks (
                outbound_task_id,
                message_id,
                status,
                attempt_count,
                next_attempt_at_utc,
                last_error,
                created_at_utc,
                updated_at_utc)
            VALUES (
                @OutboundTaskId,
                @MessageId,
                @Status,
                @AttemptCount,
                @NextAttemptAtUtc,
                @LastError,
                @CreatedAtUtc,
                @UpdatedAtUtc);
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                outboundTask.OutboundTaskId,
                outboundTask.MessageId,
                Status = outboundTask.Status.ToString(),
                outboundTask.AttemptCount,
                outboundTask.NextAttemptAtUtc,
                outboundTask.LastError,
                outboundTask.CreatedAtUtc,
                outboundTask.UpdatedAtUtc
            },
            cancellationToken: cancellationToken));
        return outboundTask;
    }

    public async Task<IReadOnlyList<OutboundTaskRecord>> ListPendingAsync(int take, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT
                outbound_task_id,
                message_id,
                status,
                attempt_count,
                next_attempt_at_utc,
                last_error,
                created_at_utc,
                updated_at_utc
            FROM outbound_tasks
            WHERE status IN (@PendingStatus, @FailedStatus)
              AND next_attempt_at_utc <= @NowUtc
            ORDER BY next_attempt_at_utc ASC
            LIMIT @Take;
            """;

        var records = await connection.QueryAsync<OutboundTaskRecord>(
            new CommandDefinition(
                sql,
                new
                {
                    Take = take,
                    NowUtc = DateTimeOffset.UtcNow,
                    PendingStatus = OutboundMailStatus.Pending.ToString(),
                    FailedStatus = OutboundMailStatus.Failed.ToString()
                },
                cancellationToken: cancellationToken));
        return records.ToList();
    }

    public async Task MarkAsSendingAsync(string outboundTaskId, int attemptCount, DateTimeOffset updatedAtUtc, CancellationToken cancellationToken = default)
    {
        await UpdateTaskAsync(
            outboundTaskId,
            OutboundMailStatus.Sending,
            attemptCount,
            null,
            updatedAtUtc,
            updatedAtUtc,
            cancellationToken);
    }

    public async Task MarkAsSentAsync(string outboundTaskId, DateTimeOffset sentAtUtc, CancellationToken cancellationToken = default)
    {
        await UpdateTaskAsync(
            outboundTaskId,
            OutboundMailStatus.Sent,
            null,
            null,
            sentAtUtc,
            sentAtUtc,
            cancellationToken);
    }

    public async Task MarkAsFailedAsync(
        string outboundTaskId,
        int attemptCount,
        string? lastError,
        DateTimeOffset nextAttemptAtUtc,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await UpdateTaskAsync(
            outboundTaskId,
            OutboundMailStatus.Failed,
            attemptCount,
            lastError,
            nextAttemptAtUtc,
            updatedAtUtc,
            cancellationToken);
    }

    private async Task UpdateTaskAsync(
        string outboundTaskId,
        OutboundMailStatus status,
        int? attemptCount,
        string? lastError,
        DateTimeOffset nextAttemptAtUtc,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        const string sql = """
            UPDATE outbound_tasks
            SET status = @Status,
                attempt_count = COALESCE(@AttemptCount, attempt_count),
                last_error = @LastError,
                next_attempt_at_utc = @NextAttemptAtUtc,
                updated_at_utc = @UpdatedAtUtc
            WHERE outbound_task_id = @OutboundTaskId;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                OutboundTaskId = outboundTaskId,
                Status = status.ToString(),
                AttemptCount = attemptCount,
                LastError = lastError,
                NextAttemptAtUtc = nextAttemptAtUtc,
                UpdatedAtUtc = updatedAtUtc
            },
            cancellationToken: cancellationToken));
    }

    private static object ToMessageParameters(MailMessageRecord message)
    {
        return new
        {
            message.MessageId,
            message.ThreadId,
            message.MailboxId,
            message.Direction,
            message.Subject,
            message.FromAddress,
            message.ToAddress,
            message.BodyText,
            message.RawContentPath,
            InboundStatus = message.InboundStatus?.ToString(),
            OutboundStatus = message.OutboundStatus?.ToString(),
            message.CreatedAtUtc,
            message.ReceivedAtUtc,
            message.SentAtUtc,
            message.InReplyToMessageId
        };
    }
}
