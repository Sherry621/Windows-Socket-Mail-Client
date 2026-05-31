namespace MailClient.Models;

public sealed class SmtpSendResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
}

public sealed class Pop3MailboxStat
{
    public int MailCount { get; init; }
    public int TotalSize { get; init; }
}
