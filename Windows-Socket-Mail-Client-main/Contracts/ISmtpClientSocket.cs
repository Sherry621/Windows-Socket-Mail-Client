using MailClient.Models;

namespace MailClient.Contracts;

// SMTP 邮件发送接口
public interface ISmtpClientSocket
{
    // 连接 SMTP 服务器
    Task ConnectAsync(AccountConfig config, CancellationToken cancellationToken = default);
    // 发送邮件
    Task<SmtpSendResult> SendMailAsync(AccountConfig config, MailMessageModel mail, CancellationToken cancellationToken = default);
    // 断开连接
    Task QuitAsync(CancellationToken cancellationToken = default);
}