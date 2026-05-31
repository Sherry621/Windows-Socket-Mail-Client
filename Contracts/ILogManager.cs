using MailClient.Models;

namespace MailClient.Contracts;

public interface ILogManager
{
    event EventHandler<OperationLogEntry>? LogAdded;

    void Info(string protocol, string operation, string content);
    void Send(string protocol, string content);
    void Receive(string protocol, string content);
    void Success(string protocol, string operation, string content);
    void Error(string protocol, string operation, string content);
    IReadOnlyList<OperationLogEntry> GetAll();
    void Clear();
}
