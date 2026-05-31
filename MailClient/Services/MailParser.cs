using System.Text;
using MailClient.Contracts;
using MailClient.Models;

namespace MailClient.Services;

public sealed class MailParser : IMailParser
{
    public MailMessageModel Parse(string rawContent)
    {
        return new MailMessageModel
        {
            RawContent = rawContent,
            Body = rawContent
        };
    }

    public string BuildSmtpContent(MailMessageModel mail)
    {
        StringBuilder builder = new();
        builder.AppendLine($"From: {mail.Sender}");
        builder.AppendLine($"To: {mail.Receiver}");
        builder.AppendLine($"Subject: {mail.Subject}");
        builder.AppendLine($"Date: {mail.Date:R}");
        builder.AppendLine("Content-Type: text/plain; charset=\"utf-8\"");
        builder.AppendLine("Content-Transfer-Encoding: 8bit");
        builder.AppendLine();
        builder.AppendLine(mail.Body);
        builder.AppendLine(".");
        return builder.ToString();
    }
}
