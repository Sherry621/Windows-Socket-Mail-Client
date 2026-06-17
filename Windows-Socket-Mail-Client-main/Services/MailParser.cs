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
        builder.AppendLine($"Subject: {EncodeHeader(mail.Subject)}");
        builder.AppendLine($"Date: {mail.Date:R}");
        builder.AppendLine("MIME-Version: 1.0");
        builder.AppendLine("Content-Type: text/plain; charset=\"utf-8\"");
        builder.AppendLine("Content-Transfer-Encoding: base64");
        builder.AppendLine();
        AppendBody(builder, mail.Body);
        return builder.ToString();
    }

    private static string EncodeHeader(string value)
    {
        if (string.IsNullOrEmpty(value) || IsAscii(value))
        {
            return value;
        }

        return $"=?utf-8?B?{Convert.ToBase64String(Encoding.UTF8.GetBytes(value))}?=";
    }

    private static void AppendBody(StringBuilder builder, string body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return;
        }

        string normalizedBody = body.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", "\r\n");
        string encodedBody = Convert.ToBase64String(Encoding.UTF8.GetBytes(normalizedBody));

        for (int index = 0; index < encodedBody.Length; index += 76)
        {
            int chunkLength = Math.Min(76, encodedBody.Length - index);
            builder.AppendLine(encodedBody.Substring(index, chunkLength));
        }
    }

    private static bool IsAscii(string value)
    {
        foreach (char character in value)
        {
            if (character > 127)
            {
                return false;
            }
        }

        return true;
    }
}
