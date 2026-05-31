using MailClient.Contracts;
using MailClient.Models;
using MailClient.Networking;

namespace MailClient.Protocols;

public sealed class SmtpClientSocket(ILogManager logManager, IMailParser mailParser) : ISmtpClientSocket, IDisposable
{
    private readonly ILogManager logManager = logManager;
    private readonly IMailParser mailParser = mailParser;
    private SocketConnection? connection;

    public async Task ConnectAsync(AccountConfig config, CancellationToken cancellationToken = default)
    {
        connection = new SocketConnection();
        logManager.Info("SMTP", "Connect", $"Connecting to {config.SmtpServer}:{config.SmtpPort}");
        await connection.ConnectAsync(config.SmtpServer, config.SmtpPort, cancellationToken);
    }

    public Task<SmtpSendResult> SendMailAsync(AccountConfig config, MailMessageModel mail, CancellationToken cancellationToken = default)
    {
        _ = mailParser.BuildSmtpContent(mail);

        return Task.FromResult(new SmtpSendResult
        {
            Success = false,
            Message = "SMTP command flow is not implemented yet."
        });
    }

    public async Task QuitAsync(CancellationToken cancellationToken = default)
    {
        if (connection is null)
        {
            return;
        }

        await connection.SendLineAsync("QUIT", cancellationToken);
        logManager.Send("SMTP", "QUIT");
    }

    public void Dispose()
    {
        connection?.Dispose();
    }
}
