namespace AgentMailbox.Core.Options;

public sealed class StorageOptions
{
    public string DatabasePath { get; set; } = "app_data/agentmailbox.db";

    public string RawMailRootPath { get; set; } = "app_data/raw-mails";

    public string AttachmentRootPath { get; set; } = "app_data/attachments";
}

public sealed class InboundListenerOptions
{
    public bool Enabled { get; set; }

    public string Host { get; set; } = "127.0.0.1";

    public int Port { get; set; } = 2525;
}

public sealed class OutboundSmtpOptions
{
    public bool Enabled { get; set; }

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 25;

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? FromDomain { get; set; }

    public bool UseSsl { get; set; }
}

public sealed class RetryOptions
{
    public int MaxAttempts { get; set; } = 3;

    public int InitialDelaySeconds { get; set; } = 30;
}
