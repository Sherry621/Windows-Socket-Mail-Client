using MailClient.Models;

namespace MailClient.Contracts;

// 邮件内容解析接口
public interface IMailParser
{
    // 把收到的字符串解析成邮件对象
    MailMessageModel Parse(string rawContent);
    // 把邮件对象拼成要发送的字符串
    string BuildSmtpContent(MailMessageModel mail);
}