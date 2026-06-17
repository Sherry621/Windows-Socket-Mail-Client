using System.Net;
using System.Net.Mail;
using System.Text;
using MailClient.Contracts;
using MailClient.Models;
using MailClient.Networking;

namespace MailClient.Protocols;

public sealed class SmtpClientSocket(ILogManager logManager, IMailParser mailParser) : ISmtpClientSocket, IDisposable
{
    private const string ProtocolName = "SMTP";

    private readonly ILogManager logManager = logManager;
    private readonly IMailParser mailParser = mailParser;
    private SocketConnection? connection;

    public async Task ConnectAsync(AccountConfig config, CancellationToken cancellationToken = default)
    {
        DisposeConnection();

        connection = new SocketConnection();
        logManager.Info(ProtocolName, "Connect", $"Connecting to {config.SmtpServer}:{config.SmtpPort}");

        try
        {
            await connection.ConnectAsync(config.SmtpServer, config.SmtpPort, cancellationToken);
            SmtpResponse response = await ReadResponseAsync("Connect", cancellationToken);
            EnsureExpectedCode("Connect", response, 220);
            logManager.Success(ProtocolName, "Connect", response.Summary);
        }
        catch
        {
            DisposeConnection();
            throw;
        }
    }

    public async Task<SmtpSendResult> SendMailAsync(AccountConfig config, MailMessageModel mail, CancellationToken cancellationToken = default)
    {
        try
        {
            MailMessageModel normalizedMail = NormalizeMail(config, mail);
            await ConnectAsync(config, cancellationToken);
            await SendGreetingAsync(config, cancellationToken);
            await AuthenticateAsync(config, cancellationToken);
            await SendEnvelopeAsync(normalizedMail, cancellationToken);

            string content = mailParser.BuildSmtpContent(normalizedMail);
            SmtpResponse sendResult = await SendDataAsync(content, cancellationToken);

            await QuitAsync(cancellationToken);

            string message = sendResult.Summary;
            logManager.Success(ProtocolName, "SendMail", message);

            return new SmtpSendResult
            {
                Success = true,
                Message = message
            };
        }
        catch (OperationCanceledException)
        {
            logManager.Error(ProtocolName, "SendMail", "SMTP send was canceled.");
            throw;
        }
        catch (Exception ex)
        {
            logManager.Error(ProtocolName, "SendMail", ex.Message);

            return new SmtpSendResult
            {
                Success = false,
                Message = ex.Message
            };
        }
        finally
        {
            DisposeConnection();
        }
    }

    public async Task QuitAsync(CancellationToken cancellationToken = default)
    {
        if (connection is null || !connection.IsConnected)
        {
            DisposeConnection();
            return;
        }

        try
        {
            SmtpResponse response = await SendCommandAsync("QUIT", "Quit", cancellationToken);
            EnsureExpectedCode("Quit", response, 221);
            logManager.Success(ProtocolName, "Quit", response.Summary);
        }
        finally
        {
            DisposeConnection();
        }
    }

    public void Dispose()
    {
        DisposeConnection();
    }

    private async Task SendGreetingAsync(AccountConfig config, CancellationToken cancellationToken)
    {
        string clientIdentity = GetClientIdentity(config);
        SmtpResponse ehloResponse = await SendCommandAsync($"EHLO {clientIdentity}", "EHLO", cancellationToken);

        if (ehloResponse.StatusCode == 250)
        {
            logManager.Success(ProtocolName, "EHLO", ehloResponse.Summary);
            return;
        }

        logManager.Info(ProtocolName, "EHLO", $"EHLO was rejected with {ehloResponse.StatusCode}. Falling back to HELO.");

        SmtpResponse heloResponse = await SendCommandAsync($"HELO {clientIdentity}", "HELO", cancellationToken);
        EnsureExpectedCode("HELO", heloResponse, 250);
        logManager.Success(ProtocolName, "HELO", heloResponse.Summary);
    }

    private async Task AuthenticateAsync(AccountConfig config, CancellationToken cancellationToken)
    {
        SmtpResponse authStartResponse = await SendCommandAsync("AUTH LOGIN", "AUTH LOGIN", cancellationToken);
        EnsureExpectedCode("AUTH LOGIN", authStartResponse, 334);

        string encodedUser = Convert.ToBase64String(Encoding.UTF8.GetBytes(config.Email));
        SmtpResponse userResponse = await SendSensitiveCommandAsync(
            encodedUser,
            "<base64 username>",
            "AUTH LOGIN Username",
            cancellationToken);
        EnsureExpectedCode("AUTH LOGIN Username", userResponse, 334);

        string encodedPassword = Convert.ToBase64String(Encoding.UTF8.GetBytes(config.PasswordOrAuthCode));
        SmtpResponse passwordResponse = await SendSensitiveCommandAsync(
            encodedPassword,
            "<base64 auth code>",
            "AUTH LOGIN Password",
            cancellationToken);
        EnsureExpectedCode("AUTH LOGIN Password", passwordResponse, 235);

        logManager.Success(ProtocolName, "AUTH LOGIN", passwordResponse.Summary);
    }

    private async Task SendEnvelopeAsync(MailMessageModel mail, CancellationToken cancellationToken)
    {
        SmtpResponse mailFromResponse = await SendCommandAsync($"MAIL FROM:<{mail.Sender}>", "MAIL FROM", cancellationToken);
        EnsureExpectedCode("MAIL FROM", mailFromResponse, 250);

        SmtpResponse rcptToResponse = await SendCommandAsync($"RCPT TO:<{mail.Receiver}>", "RCPT TO", cancellationToken);
        EnsureExpectedCode("RCPT TO", rcptToResponse, 250, 251);

        logManager.Success(ProtocolName, "Envelope", $"Sender: {mail.Sender}; Receiver: {mail.Receiver}");
    }

    private async Task<SmtpResponse> SendDataAsync(string content, CancellationToken cancellationToken)
    {
        SmtpResponse dataResponse = await SendCommandAsync("DATA", "DATA", cancellationToken);
        EnsureExpectedCode("DATA", dataResponse, 354);

        foreach (string line in EnumerateSmtpDataLines(content))
        {
            await SendLineAsync(line, line, cancellationToken);
        }

        SmtpResponse sendResult = await SendCommandAsync(".", "DATA END", cancellationToken);
        EnsureExpectedCode("DATA END", sendResult, 250);
        return sendResult;
    }

    private async Task<SmtpResponse> SendCommandAsync(string command, string operation, CancellationToken cancellationToken)
    {
        await SendLineAsync(command, command, cancellationToken);
        return await ReadResponseAsync(operation, cancellationToken);
    }

    private async Task<SmtpResponse> SendSensitiveCommandAsync(
        string command,
        string logContent,
        string operation,
        CancellationToken cancellationToken)
    {
        await SendLineAsync(command, logContent, cancellationToken);
        return await ReadResponseAsync(operation, cancellationToken);
    }

    private async Task SendLineAsync(string line, string logContent, CancellationToken cancellationToken)
    {
        SocketConnection activeConnection = GetRequiredConnection();
        await activeConnection.SendLineAsync(line, cancellationToken);
        logManager.Send(ProtocolName, logContent);
    }

    private async Task<SmtpResponse> ReadResponseAsync(string operation, CancellationToken cancellationToken)
    {
        string firstLine = await ReadRequiredLineAsync(cancellationToken);
        int statusCode = ParseStatusCode(firstLine, operation);

        List<string> lines = [firstLine];
        logManager.Receive(ProtocolName, firstLine);

        if (IsMultiline(firstLine))
        {
            string expectedPrefix = $"{statusCode:D3} ";

            while (true)
            {
                string line = await ReadRequiredLineAsync(cancellationToken);
                lines.Add(line);
                logManager.Receive(ProtocolName, line);

                if (line.StartsWith(expectedPrefix, StringComparison.Ordinal))
                {
                    break;
                }
            }
        }

        return new SmtpResponse(statusCode, lines);
    }

    private async Task<string> ReadRequiredLineAsync(CancellationToken cancellationToken)
    {
        SocketConnection activeConnection = GetRequiredConnection();
        string line = await activeConnection.ReadLineAsync(cancellationToken);

        if (string.IsNullOrEmpty(line))
        {
            throw new IOException("SMTP server closed the connection unexpectedly.");
        }

        return line;
    }

    private static int ParseStatusCode(string responseLine, string operation)
    {
        if (responseLine.Length < 3 || !int.TryParse(responseLine[..3], out int statusCode))
        {
            throw new InvalidOperationException($"SMTP response for {operation} does not contain a valid status code: {responseLine}");
        }

        return statusCode;
    }

    private static bool IsMultiline(string responseLine)
    {
        return responseLine.Length > 3 && responseLine[3] == '-';
    }

    private static void EnsureExpectedCode(string operation, SmtpResponse response, params int[] expectedCodes)
    {
        if (expectedCodes.Contains(response.StatusCode))
        {
            return;
        }

        throw new InvalidOperationException(
            $"{operation} failed. Expected [{string.Join(", ", expectedCodes)}], got {response.StatusCode}: {response.Summary}");
    }

    private static IEnumerable<string> EnumerateSmtpDataLines(string content)
    {
        string normalizedContent = content.Replace("\r\n", "\n").Replace('\r', '\n');
        string[] lines = normalizedContent.Split('\n');

        foreach (string line in lines)
        {
            if (line.Length > 0 && line[0] == '.')
            {
                yield return $".{line}";
                continue;
            }

            yield return line;
        }
    }

    private static MailMessageModel NormalizeMail(AccountConfig config, MailMessageModel mail)
    {
        string sender = NormalizeAddress(string.IsNullOrWhiteSpace(mail.Sender) ? config.Email : mail.Sender);
        string receiver = NormalizeAddress(mail.Receiver);

        return new MailMessageModel
        {
            Sender = sender,
            Receiver = receiver,
            Subject = mail.Subject,
            Body = mail.Body,
            Date = mail.Date == default ? DateTimeOffset.Now : mail.Date,
            RawContent = mail.RawContent
        };
    }

    private static string NormalizeAddress(string address)
    {
        return new MailAddress(address).Address;
    }

    private static string GetClientIdentity(AccountConfig config)
    {
        string hostName = Dns.GetHostName();

        if (!string.IsNullOrWhiteSpace(hostName))
        {
            return hostName;
        }

        int atIndex = config.Email.IndexOf('@');
        if (atIndex >= 0 && atIndex < config.Email.Length - 1)
        {
            return config.Email[(atIndex + 1)..];
        }

        return "localhost";
    }

    private SocketConnection GetRequiredConnection()
    {
        if (connection is null || !connection.IsConnected)
        {
            throw new InvalidOperationException("SMTP socket connection has not been established.");
        }

        return connection;
    }

    private void DisposeConnection()
    {
        connection?.Dispose();
        connection = null;
    }

    private sealed record SmtpResponse(int StatusCode, IReadOnlyList<string> Lines)
    {
        public string Summary => string.Join(" | ", Lines);
    }
}
