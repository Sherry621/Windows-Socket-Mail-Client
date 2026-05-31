using MailClient.Contracts;
using MailClient.Models;
using MailClient.Networking;

namespace MailClient.Protocols;

public sealed class Pop3ClientSocket(ILogManager logManager, IMailParser mailParser) : IPop3ClientSocket, IDisposable
{
    private readonly ILogManager logManager = logManager;
    private readonly IMailParser mailParser = mailParser;
    private SocketConnection? connection;

    public async Task ConnectAsync(AccountConfig config, CancellationToken cancellationToken = default)
    {
        connection = new SocketConnection();
        logManager.Info("POP3", "Connect", $"Connecting to {config.Pop3Server}:{config.Pop3Port}");
        await connection.ConnectAsync(config.Pop3Server, config.Pop3Port, cancellationToken);
    }

    public Task LoginAsync(AccountConfig config, CancellationToken cancellationToken = default)
    {
        logManager.Info("POP3", "Login", $"Login placeholder for {config.Email}");
        return Task.CompletedTask;
    }

    public Task<Pop3MailboxStat> GetStatAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new Pop3MailboxStat());
    }

    public Task<IReadOnlyList<MailSummary>> ListMailsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<MailSummary>>([]);
    }

    public Task<MailMessageModel> RetrieveMailAsync(int mailNo, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(mailParser.Parse(string.Empty));
    }

    public Task DeleteMailAsync(int mailNo, CancellationToken cancellationToken = default)
    {
        logManager.Info("POP3", "Delete", $"Delete placeholder for mail #{mailNo}");
        return Task.CompletedTask;
    }

    public async Task QuitAsync(CancellationToken cancellationToken = default)
    {
        if (connection is null)
        {
            return;
        }

        await connection.SendLineAsync("QUIT", cancellationToken);
        logManager.Send("POP3", "QUIT");
    }

    public void Dispose()
    {
        connection?.Dispose();
    }
}
