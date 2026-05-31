using MailClient.Contracts;
using MailClient.Models;

namespace MailClient.Services;

public sealed class LogManager : ILogManager
{
    private readonly List<OperationLogEntry> entries = [];

    public event EventHandler<OperationLogEntry>? LogAdded;

    public void Info(string protocol, string operation, string content)
    {
        Add(protocol, operation, content, LogLevel.Info);
    }

    public void Send(string protocol, string content)
    {
        Add(protocol, "Send", content, LogLevel.Send);
    }

    public void Receive(string protocol, string content)
    {
        Add(protocol, "Receive", content, LogLevel.Receive);
    }

    public void Success(string protocol, string operation, string content)
    {
        Add(protocol, operation, content, LogLevel.Success);
    }

    public void Error(string protocol, string operation, string content)
    {
        Add(protocol, operation, content, LogLevel.Error);
    }

    public IReadOnlyList<OperationLogEntry> GetAll()
    {
        return entries.AsReadOnly();
    }

    public void Clear()
    {
        entries.Clear();
    }

    private void Add(string protocol, string operation, string content, LogLevel level)
    {
        OperationLogEntry entry = new()
        {
            Protocol = protocol,
            Operation = operation,
            Content = content,
            Level = level,
            CreateTime = DateTimeOffset.Now
        };

        entries.Add(entry);
        LogAdded?.Invoke(this, entry);
    }
}
