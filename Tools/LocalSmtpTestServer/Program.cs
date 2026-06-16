using System.Net;
using System.Net.Sockets;
using System.Text;

LocalSmtpServerOptions options = LocalSmtpServerOptions.Parse(args);

Console.WriteLine("Local SMTP Test Server");
Console.WriteLine($"Mode      : {options.Mode}");
Console.WriteLine($"Endpoint  : {options.Host}:{options.Port}");
Console.WriteLine($"Output Dir: {Path.GetFullPath(options.OutputDirectory)}");
Console.WriteLine("Press Ctrl+C to stop.");
Console.WriteLine();

using CancellationTokenSource cts = new();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cts.Cancel();
};

TcpListener listener = new(IPAddress.Parse(options.Host), options.Port);
listener.Start();

try
{
    while (!cts.IsCancellationRequested)
    {
        TcpClient client = await listener.AcceptTcpClientAsync(cts.Token);
        _ = HandleClientAsync(client, options, cts.Token);
    }
}
catch (OperationCanceledException)
{
}
finally
{
    listener.Stop();
}

return;

static async Task HandleClientAsync(TcpClient client, LocalSmtpServerOptions options, CancellationToken cancellationToken)
{
    using (client)
    {
        using NetworkStream stream = client.GetStream();
        using StreamReader reader = new(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        using StreamWriter writer = new(stream, Encoding.UTF8, leaveOpen: true)
        {
            NewLine = "\r\n",
            AutoFlush = true
        };

        SessionContext context = new();

        Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss}] Client connected: {client.Client.RemoteEndPoint}");

        await SendLineAsync(writer, "220 localhost Local SMTP Test Server Ready", cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            string? line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                break;
            }

            switch (context.State)
            {
                case SessionState.ExpectUsername:
                    context.Username = TryDecodeBase64(line);
                    Console.WriteLine($"C: <base64 username> ({context.Username})");
                    context.State = SessionState.ExpectPassword;
                    await SendLineAsync(writer, "334 UGFzc3dvcmQ6", cancellationToken);
                    continue;

                case SessionState.ExpectPassword:
                    Console.WriteLine("C: <base64 auth code> (redacted)");
                    context.State = SessionState.Command;

                    if (options.Mode == ServerMode.AuthFail)
                    {
                        await SendLineAsync(writer, "535 5.7.8 Authentication credentials invalid", cancellationToken);
                    }
                    else
                    {
                        context.Authenticated = true;
                        await SendLineAsync(writer, "235 2.7.0 Authentication successful", cancellationToken);
                    }

                    continue;

                case SessionState.Data:
                    if (line == ".")
                    {
                        string filePath = await SaveMessageAsync(context, options, cancellationToken);
                        await SendLineAsync(writer, "250 2.0.0 Message accepted for delivery", cancellationToken);
                        Console.WriteLine($"Saved message: {filePath}");
                        context.State = SessionState.Command;
                        context.DataLines.Clear();
                        continue;
                    }

                    if (line.StartsWith("..", StringComparison.Ordinal))
                    {
                        line = line[1..];
                    }

                    Console.WriteLine($"C: {line}");
                    context.DataLines.Add(line);
                    continue;
            }

            Console.WriteLine($"C: {line}");
            string upperLine = line.ToUpperInvariant();

            if (upperLine.StartsWith("EHLO ", StringComparison.Ordinal))
            {
                await SendLineAsync(writer, "250-localhost greets you", cancellationToken);
                await SendLineAsync(writer, "250-AUTH LOGIN", cancellationToken);
                await SendLineAsync(writer, "250 OK", cancellationToken);
                continue;
            }

            if (upperLine.StartsWith("HELO ", StringComparison.Ordinal))
            {
                await SendLineAsync(writer, "250 localhost", cancellationToken);
                continue;
            }

            if (upperLine == "AUTH LOGIN")
            {
                context.State = SessionState.ExpectUsername;
                await SendLineAsync(writer, "334 VXNlcm5hbWU6", cancellationToken);
                continue;
            }

            if (upperLine.StartsWith("MAIL FROM:", StringComparison.Ordinal))
            {
                context.MailFrom = line["MAIL FROM:".Length..].Trim();
                await SendLineAsync(writer, "250 2.1.0 Sender OK", cancellationToken);
                continue;
            }

            if (upperLine.StartsWith("RCPT TO:", StringComparison.Ordinal))
            {
                context.RcptTo = line["RCPT TO:".Length..].Trim();

                if (options.Mode == ServerMode.RcptFail)
                {
                    await SendLineAsync(writer, "550 5.1.1 Recipient rejected", cancellationToken);
                }
                else
                {
                    await SendLineAsync(writer, "250 2.1.5 Recipient OK", cancellationToken);
                }

                continue;
            }

            if (upperLine == "DATA")
            {
                if (!context.Authenticated && options.Mode != ServerMode.Success && options.Mode != ServerMode.RcptFail)
                {
                    await SendLineAsync(writer, "530 5.7.0 Authentication required", cancellationToken);
                    continue;
                }

                context.State = SessionState.Data;
                context.DataLines.Clear();
                await SendLineAsync(writer, "354 End data with <CR><LF>.<CR><LF>", cancellationToken);
                continue;
            }

            if (upperLine == "RSET")
            {
                context = new SessionContext();
                await SendLineAsync(writer, "250 2.0.0 Reset state", cancellationToken);
                continue;
            }

            if (upperLine == "NOOP")
            {
                await SendLineAsync(writer, "250 2.0.0 OK", cancellationToken);
                continue;
            }

            if (upperLine == "QUIT")
            {
                await SendLineAsync(writer, "221 2.0.0 Bye", cancellationToken);
                break;
            }

            await SendLineAsync(writer, "502 5.5.2 Command not implemented", cancellationToken);
        }

        Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss}] Client disconnected");
        Console.WriteLine();
    }
}

static async Task<string> SaveMessageAsync(SessionContext context, LocalSmtpServerOptions options, CancellationToken cancellationToken)
{
    Directory.CreateDirectory(options.OutputDirectory);

    string timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");
    string filePath = Path.Combine(options.OutputDirectory, $"{timestamp}.eml");

    List<string> lines =
    [
        $"X-Local-SMTP-Mode: {options.Mode}",
        $"X-Local-SMTP-User: {context.Username ?? "unknown"}",
        $"X-Local-SMTP-From: {context.MailFrom ?? "unknown"}",
        $"X-Local-SMTP-To: {context.RcptTo ?? "unknown"}",
        .. context.DataLines
    ];

    await File.WriteAllTextAsync(filePath, string.Join(Environment.NewLine, lines), cancellationToken);
    return Path.GetFullPath(filePath);
}

static async Task SendLineAsync(StreamWriter writer, string line, CancellationToken cancellationToken)
{
    Console.WriteLine($"S: {line}");
    await writer.WriteLineAsync(line.AsMemory(), cancellationToken);
}

static string TryDecodeBase64(string value)
{
    try
    {
        return Encoding.UTF8.GetString(Convert.FromBase64String(value));
    }
    catch
    {
        return value;
    }
}

sealed class SessionContext
{
    public SessionState State { get; set; } = SessionState.Command;
    public bool Authenticated { get; set; }
    public string? Username { get; set; }
    public string? MailFrom { get; set; }
    public string? RcptTo { get; set; }
    public List<string> DataLines { get; } = [];
}

enum SessionState
{
    Command,
    ExpectUsername,
    ExpectPassword,
    Data
}

enum ServerMode
{
    Success,
    AuthFail,
    RcptFail
}

sealed class LocalSmtpServerOptions
{
    public required string Host { get; init; }
    public required int Port { get; init; }
    public required ServerMode Mode { get; init; }
    public required string OutputDirectory { get; init; }

    public static LocalSmtpServerOptions Parse(string[] args)
    {
        string host = "127.0.0.1";
        int port = 2525;
        ServerMode mode = ServerMode.Success;
        string outputDirectory = Path.Combine("artifacts", "local-smtp-mails");

        for (int index = 0; index < args.Length; index++)
        {
            string argument = args[index];

            switch (argument)
            {
                case "--host":
                    host = GetValue(args, ref index, argument);
                    break;

                case "--port":
                    port = int.Parse(GetValue(args, ref index, argument));
                    break;

                case "--mode":
                    mode = ParseMode(GetValue(args, ref index, argument));
                    break;

                case "--output-dir":
                    outputDirectory = GetValue(args, ref index, argument);
                    break;

                case "--help":
                case "-h":
                    PrintHelpAndExit();
                    break;

                default:
                    throw new ArgumentException($"Unknown argument: {argument}");
            }
        }

        return new LocalSmtpServerOptions
        {
            Host = host,
            Port = port,
            Mode = mode,
            OutputDirectory = outputDirectory
        };
    }

    private static string GetValue(string[] args, ref int index, string argument)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for {argument}");
        }

        index++;
        return args[index];
    }

    private static ServerMode ParseMode(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "success" => ServerMode.Success,
            "auth-fail" => ServerMode.AuthFail,
            "rcpt-fail" => ServerMode.RcptFail,
            _ => throw new ArgumentException($"Unsupported mode: {value}")
        };
    }

    private static void PrintHelpAndExit()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project Tools/LocalSmtpTestServer/LocalSmtpTestServer.csproj -- --mode success");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --mode success|auth-fail|rcpt-fail");
        Console.WriteLine("  --host 127.0.0.1");
        Console.WriteLine("  --port 2525");
        Console.WriteLine("  --output-dir artifacts/local-smtp-mails");
        Environment.Exit(0);
    }
}
