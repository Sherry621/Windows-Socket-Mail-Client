using MailClient.Models;

namespace MailClient.Contracts;

public interface IMailParser
{
    MailMessageModel Parse(string rawContent);
    string BuildSmtpContent(MailMessageModel mail);
}
