namespace MailClient.Models;

public sealed class OperationLogEntry
{
    public string Protocol { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public LogLevel Level { get; set; } = LogLevel.Info;
    public DateTimeOffset CreateTime { get; set; } = DateTimeOffset.Now;
}

public enum LogLevel
{
    Info,
    Send,
    Receive,
    Success,
    Error
}
