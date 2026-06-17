using System.Net.Sockets;
using System.Text;

namespace MailClient.Networking;

public sealed class SocketConnection : IDisposable
{
    private TcpClient? client;
    private StreamReader? reader;
    private StreamWriter? writer;

    public bool IsConnected => client is not null && client.Connected && reader is not null && writer is not null;

    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        client = new TcpClient();
        await client.ConnectAsync(host, port, cancellationToken);

        NetworkStream stream = client.GetStream();
        reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true)
        {
            NewLine = "\r\n",
            AutoFlush = true
        };
    }

    public async Task SendLineAsync(string line, CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        await writer!.WriteLineAsync(line.AsMemory(), cancellationToken);
    }

    public async Task<string> ReadLineAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnected();
        string? line = await reader!.ReadLineAsync(cancellationToken);
        return line ?? string.Empty;
    }

    public async Task<IReadOnlyList<string>> ReadMultilineAsync(string endMark = ".", CancellationToken cancellationToken = default)
    {
        List<string> lines = [];

        while (true)
        {
            string line = await ReadLineAsync(cancellationToken);
            if (line == endMark)
            {
                break;
            }

            lines.Add(line);
        }

        return lines;
    }

    private void EnsureConnected()
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Socket connection has not been established.");
        }
    }

    public void Dispose()
    {
        writer?.Dispose();
        reader?.Dispose();
        client?.Dispose();
        writer = null;
        reader = null;
        client = null;
    }
}
