using MailClient.Contracts;
using MailClient.Models;
using MailClient.Networking;

namespace MailClient.Protocols;

/// <summary>
/// POP3 协议客户端，通过原始 Socket 实现 RFC 1939 的核心命令流程。
/// 负责成员：成员三
/// 覆盖命令：USER / PASS / STAT / LIST / RETR / DELE / QUIT
/// </summary>
public sealed class Pop3ClientSocket(ILogManager logManager, IMailParser mailParser) : IPop3ClientSocket, IDisposable
{
    private const string ProtocolName = "POP3";

    private readonly ILogManager logManager = logManager;
    private readonly IMailParser mailParser = mailParser;
    private SocketConnection? connection;

    // ------------------------------------------------------------------ //
    //  公开接口实现
    // ------------------------------------------------------------------ //

    /// <summary>
    /// 建立 TCP 连接并读取服务器的欢迎行（+OK ...）。
    /// </summary>
    public async Task ConnectAsync(AccountConfig config, CancellationToken cancellationToken = default)
    {
        DisposeConnection();
        connection = new SocketConnection();
        logManager.Info(ProtocolName, "Connect", $"正在连接 {config.Pop3Server}:{config.Pop3Port}");

        try
        {
            await connection.ConnectAsync(config.Pop3Server, config.Pop3Port, cancellationToken);

            // 服务器在 TCP 握手后立即发送欢迎行
            string greeting = await ReadSingleLineAsync(cancellationToken);
            EnsurePositive("Connect", greeting);
            logManager.Success(ProtocolName, "Connect", greeting);
        }
        catch
        {
            DisposeConnection();
            throw;
        }
    }

    /// <summary>
    /// 依次发送 USER 和 PASS 命令完成身份认证。
    /// 授权码不会写入日志明文。
    /// </summary>
    public async Task LoginAsync(AccountConfig config, CancellationToken cancellationToken = default)
    {
        // USER <username>
        string userResp = await SendCommandAsync($"USER {config.Email}", cancellationToken);
        EnsurePositive("USER", userResp);

        // PASS — 实际命令含授权码，日志中只记录占位符
        await SendLineAsync(
            actualLine: $"PASS {config.PasswordOrAuthCode}",
            logContent: "PASS <auth code>",
            cancellationToken);
        string passResp = await ReadSingleLineAsync(cancellationToken);
        EnsurePositive("PASS", passResp);

        logManager.Success(ProtocolName, "Login", $"账号 {config.Email} 登录成功");
    }

    /// <summary>
    /// 发送 STAT 命令，返回邮箱中的邮件总数和总字节数。
    /// 响应格式：+OK &lt;count&gt; &lt;totalsize&gt;
    /// </summary>
    public async Task<Pop3MailboxStat> GetStatAsync(CancellationToken cancellationToken = default)
    {
        string response = await SendCommandAsync("STAT", cancellationToken);
        EnsurePositive("STAT", response);

        Pop3MailboxStat stat = ParseStatResponse(response);
        logManager.Success(ProtocolName, "STAT",
            $"邮件总数：{stat.MailCount} 封，总大小：{stat.TotalSize} 字节");
        return stat;
    }

    /// <summary>
    /// 发送 LIST 命令，获取每封邮件的编号与大小。
    /// 响应为多行，每行格式：&lt;mailno&gt; &lt;size&gt;，以单独一行 "." 结束。
    /// </summary>
    public async Task<IReadOnlyList<MailSummary>> ListMailsAsync(CancellationToken cancellationToken = default)
    {
        string statusLine = await SendCommandAsync("LIST", cancellationToken);
        EnsurePositive("LIST", statusLine);

        // 读取多行正文（LIST 是多行响应）
        IReadOnlyList<string> lines = await ReadMultilineBodyAsync(cancellationToken);

        List<MailSummary> summaries = [];
        foreach (string line in lines)
        {
            MailSummary? entry = ParseListLine(line);
            if (entry is not null)
            {
                summaries.Add(entry);
            }
        }

        logManager.Success(ProtocolName, "LIST", $"已获取邮件列表，共 {summaries.Count} 封");
        return summaries;
    }

    /// <summary>
    /// 发送 RETR &lt;mailno&gt; 命令，读取指定邮件的完整原始内容，
    /// 并通过 IMailParser 解析成结构化的 MailMessageModel。
    /// 响应为多行，以单独一行 "." 结束。
    /// </summary>
    public async Task<MailMessageModel> RetrieveMailAsync(int mailNo, CancellationToken cancellationToken = default)
    {
        string statusLine = await SendCommandAsync($"RETR {mailNo}", cancellationToken);
        EnsurePositive("RETR", statusLine);

        IReadOnlyList<string> lines = await ReadMultilineBodyAsync(cancellationToken);

        // 按邮件标准以 CRLF 拼接各行
        string rawContent = string.Join("\r\n", lines);

        logManager.Success(ProtocolName, "RETR",
            $"已读取邮件 #{mailNo}，共 {lines.Count} 行，原始大小 {rawContent.Length} 字节");

        return mailParser.Parse(rawContent);
    }

    /// <summary>
    /// 发送 DELE &lt;mailno&gt; 命令，标记删除指定邮件。
    /// 删除操作在 QUIT 后才真正生效；若在 QUIT 前断开连接则取消标记。
    /// </summary>
    public async Task DeleteMailAsync(int mailNo, CancellationToken cancellationToken = default)
    {
        string response = await SendCommandAsync($"DELE {mailNo}", cancellationToken);
        EnsurePositive("DELE", response);
        logManager.Success(ProtocolName, "DELE", $"已标记删除邮件 #{mailNo}（QUIT 后正式生效）");
    }

    /// <summary>
    /// 发送 QUIT 命令，提交本次会话中的删除标记并关闭连接。
    /// </summary>
    public async Task QuitAsync(CancellationToken cancellationToken = default)
    {
        if (connection is null || !connection.IsConnected)
        {
            DisposeConnection();
            return;
        }

        try
        {
            await SendLineAsync("QUIT", "QUIT", cancellationToken);
            string response = await ReadSingleLineAsync(cancellationToken);
            logManager.Success(ProtocolName, "QUIT", response);
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

    // ------------------------------------------------------------------ //
    //  私有通信辅助方法
    // ------------------------------------------------------------------ //

    /// <summary>
    /// 发送一行命令，然后读取并返回服务器的单行响应。
    /// </summary>
    private async Task<string> SendCommandAsync(string command, CancellationToken cancellationToken)
    {
        await SendLineAsync(command, command, cancellationToken);
        return await ReadSingleLineAsync(cancellationToken);
    }

    /// <summary>
    /// 向服务器发送一行文本。
    /// actualLine 是真实发送的内容，logContent 是写入日志的内容（屏蔽敏感信息）。
    /// </summary>
    private async Task SendLineAsync(string actualLine, string logContent, CancellationToken cancellationToken)
    {
        SocketConnection conn = GetRequiredConnection();
        await conn.SendLineAsync(actualLine, cancellationToken);
        logManager.Send(ProtocolName, logContent);
    }

    /// <summary>
    /// 读取一行服务器响应，记录日志并返回。
    /// 若服务器意外关闭连接则抛出 IOException。
    /// </summary>
    private async Task<string> ReadSingleLineAsync(CancellationToken cancellationToken)
    {
        SocketConnection conn = GetRequiredConnection();
        string line = await conn.ReadLineAsync(cancellationToken);

        if (string.IsNullOrEmpty(line))
        {
            throw new IOException("POP3 服务器意外关闭了连接。");
        }

        logManager.Receive(ProtocolName, line);
        return line;
    }

    /// <summary>
    /// 读取多行响应正文，直到遇到单独一行 "." 为止。
    /// 按 RFC 1939 §3 规定执行 dot-unstuffing：
    /// 若正文行以 ".." 开头，则去掉首个 "." 还原原始内容。
    /// 每行均记录到协议日志。
    /// </summary>
    private async Task<IReadOnlyList<string>> ReadMultilineBodyAsync(CancellationToken cancellationToken)
    {
        SocketConnection conn = GetRequiredConnection();
        List<string> lines = [];

        while (true)
        {
            string line = await conn.ReadLineAsync(cancellationToken);

            // 连接在 "." 结束标记之前意外关闭
            if (string.IsNullOrEmpty(line))
            {
                throw new IOException("POP3 服务器意外关闭了连接（多行响应未正常结束）。");
            }

            logManager.Receive(ProtocolName, line);

            // 单独一行 "." 是多行响应的结束标记
            if (line == ".")
            {
                break;
            }

            // RFC 1939 dot-unstuffing：行首 ".." → "."
            lines.Add(line.StartsWith("..", StringComparison.Ordinal) ? line[1..] : line);
        }

        return lines;
    }

    // ------------------------------------------------------------------ //
    //  私有解析辅助方法
    // ------------------------------------------------------------------ //

    /// <summary>
    /// 确保响应以 "+OK" 开头，否则抛出异常。
    /// </summary>
    private static void EnsurePositive(string operation, string response)
    {
        if (!response.StartsWith("+OK", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"POP3 {operation} 命令失败：{response}");
        }
    }

    /// <summary>
    /// 解析 STAT 响应，提取邮件总数和总字节数。
    /// 格式：+OK &lt;count&gt; &lt;totalsize&gt;
    /// </summary>
    private static Pop3MailboxStat ParseStatResponse(string response)
    {
        // 示例："+OK 5 14200"
        string[] parts = response.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int count = parts.Length > 1 && int.TryParse(parts[1], out int c) ? c : 0;
        int size  = parts.Length > 2 && int.TryParse(parts[2], out int s) ? s : 0;
        return new Pop3MailboxStat { MailCount = count, TotalSize = size };
    }

    /// <summary>
    /// 解析 LIST 多行响应中的单行，提取邮件编号和大小。
    /// 格式：&lt;mailno&gt; &lt;size&gt;
    /// </summary>
    private static MailSummary? ParseListLine(string line)
    {
        // 示例："1 1820"
        string[] parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return null;
        if (!int.TryParse(parts[0], out int mailNo)) return null;
        if (!int.TryParse(parts[1], out int size))   return null;

        return new MailSummary
        {
            MailNo = mailNo,
            Size   = size,
            Status = MailStatus.Unread
        };
    }

    /// <summary>
    /// 获取已建立的连接，若连接不存在则抛出异常。
    /// </summary>
    private SocketConnection GetRequiredConnection()
    {
        if (connection is null || !connection.IsConnected)
        {
            throw new InvalidOperationException(
                "POP3 Socket 连接尚未建立，请先调用 ConnectAsync。");
        }

        return connection;
    }

    private void DisposeConnection()
    {
        connection?.Dispose();
        connection = null;
    }
}
