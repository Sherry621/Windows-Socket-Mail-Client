using MailClient.Models;

namespace MailClient.Contracts;

public interface ISmtpClientSocket
{
    Task ConnectAsync(AccountConfig config, CancellationToken cancellationToken = default);
    Task<SmtpSendResult> SendMailAsync(AccountConfig config, MailMessageModel mail, CancellationToken cancellationToken = default);
    Task QuitAsync(CancellationToken cancellationToken = default);
}
