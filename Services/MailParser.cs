using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using MailClient.Contracts;
using MailClient.Models;

namespace MailClient.Services;

/// <summary>
/// 邮件构造与解析服务。
/// BuildSmtpContent：将 MailMessageModel 构造成符合 RFC 5322 格式的 SMTP DATA 内容。
/// Parse：将 POP3 RETR 收到的原始邮件文本解析成结构化的 MailMessageModel。
/// 负责成员：成员六（BuildSmtpContent 原始实现）/ 成员三（Parse 实现，POP3 收件所需）
/// </summary>
public sealed class MailParser : IMailParser
{
    // ------------------------------------------------------------------ //
    //  Parse：原始邮件文本 → MailMessageModel
    // ------------------------------------------------------------------ //

    /// <summary>
    /// 解析 POP3 RETR 命令返回的原始邮件内容，提取头部字段和邮件正文。
    /// 支持：
    ///   - 折叠头部（Folded Header）
    ///   - RFC 2047 编码词（=?utf-8?B?...?= / =?utf-8?Q?...?=）
    ///   - Content-Transfer-Encoding: base64 / quoted-printable / 7bit / 8bit
    ///   - Content-Type charset（utf-8 / gbk / gb2312 / gb18030 等）
    ///   - multipart/alternative 和 multipart/mixed（提取 text/plain 部分）
    /// </summary>
    public MailMessageModel Parse(string rawContent)
    {
        if (string.IsNullOrEmpty(rawContent))
        {
            return new MailMessageModel { RawContent = rawContent };
        }

        // 统一换行符为 CRLF，方便后续处理
        string content = NormalizeLineEndings(rawContent);

        (string headerSection, string bodySection) = SplitHeadersAndBody(content);
        Dictionary<string, string> headers = ParseHeaders(headerSection);

        string contentType        = GetHeaderValue(headers, "content-type");
        string transferEncoding   = GetHeaderValue(headers, "content-transfer-encoding").Trim().ToLowerInvariant();
        string charset            = ExtractCharset(contentType);

        string body;
        if (contentType.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase))
        {
            string boundary = ExtractBoundary(contentType);
            body = string.IsNullOrEmpty(boundary)
                ? bodySection
                : ExtractTextFromMultipart(bodySection, boundary);
        }
        else
        {
            body = DecodeBody(bodySection, transferEncoding, charset);
        }

        return new MailMessageModel
        {
            Sender     = DecodeHeaderValue(GetHeaderValue(headers, "from")),
            Receiver   = DecodeHeaderValue(GetHeaderValue(headers, "to")),
            Subject    = DecodeHeaderValue(GetHeaderValue(headers, "subject")),
            Date       = ParseDateHeader(GetHeaderValue(headers, "date")),
            Body       = body,
            RawContent = rawContent
        };
    }

    // ------------------------------------------------------------------ //
    //  BuildSmtpContent：MailMessageModel → SMTP DATA 字符串
    // ------------------------------------------------------------------ //

    /// <summary>
    /// 将邮件对象构造为符合 RFC 5322 / MIME 规范的 SMTP DATA 内容。
    /// 正文使用 base64 编码，Subject 使用 =?utf-8?B?...?= 编码（若含非 ASCII 字符）。
    /// </summary>
    public string BuildSmtpContent(MailMessageModel mail)
    {
        StringBuilder builder = new();
        builder.AppendLine($"From: {SanitizeHeaderValue(mail.Sender)}");
        builder.AppendLine($"To: {SanitizeHeaderValue(mail.Receiver)}");
        builder.AppendLine($"Subject: {EncodeHeader(SanitizeHeaderValue(mail.Subject))}");
        builder.AppendLine($"Date: {mail.Date:R}");
        builder.AppendLine("MIME-Version: 1.0");
        builder.AppendLine("Content-Type: text/plain; charset=\"utf-8\"");
        builder.AppendLine("Content-Transfer-Encoding: base64");
        builder.AppendLine();
        AppendBase64Body(builder, mail.Body);
        return builder.ToString();
    }

    // ------------------------------------------------------------------ //
    //  头部解析
    // ------------------------------------------------------------------ //

    /// <summary>
    /// 将邮件文本按"第一个空行"分割为头部段和正文段。
    /// </summary>
    private static (string headers, string body) SplitHeadersAndBody(string content)
    {
        int idx = content.IndexOf("\r\n\r\n", StringComparison.Ordinal);
        if (idx >= 0) return (content[..idx], content[(idx + 4)..]);

        idx = content.IndexOf("\n\n", StringComparison.Ordinal);
        if (idx >= 0) return (content[..idx], content[(idx + 2)..]);

        return (content, string.Empty);
    }

    /// <summary>
    /// 解析头部段，处理折叠头部（续行以空白字符开头），返回键值对字典（大小写不敏感）。
    /// </summary>
    private static Dictionary<string, string> ParseHeaders(string headerSection)
    {
        Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase);

        // 展开折叠头部：CRLF 后跟空白字符的行与上一行合并
        string unfolded = Regex.Replace(headerSection, @"\r?\n[ \t]+", " ");

        foreach (string line in unfolded.Split(['\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            int colonIndex = line.IndexOf(':');
            if (colonIndex <= 0) continue;

            string key   = line[..colonIndex].Trim().ToLowerInvariant();
            string value = line[(colonIndex + 1)..].Trim();

            // 同一字段名只取第一次出现的值
            if (!headers.ContainsKey(key))
            {
                headers[key] = value;
            }
        }

        return headers;
    }

    private static string GetHeaderValue(Dictionary<string, string> headers, string key)
    {
        return headers.TryGetValue(key, out string? value) ? value : string.Empty;
    }

    // ------------------------------------------------------------------ //
    //  RFC 2047 编码词解码
    // ------------------------------------------------------------------ //

    /// <summary>
    /// 解码头部字段中的 RFC 2047 编码词（=?charset?B|Q?text?=）。
    /// 支持 B（Base64）和 Q（Quoted-Printable）两种编码方式。
    /// </summary>
    private static string DecodeHeaderValue(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        string normalized = Regex.Replace(value, @"\?=\s+=\?", "?==?");

        return Regex.Replace(
            normalized,
            @"=\?([^?]+)\?([BbQq])\?([^?]*)\?=",
            match =>
            {
                string charsetName = match.Groups[1].Value;
                string encoding    = match.Groups[2].Value.ToUpperInvariant();
                string encoded     = match.Groups[3].Value;

                try
                {
                    Encoding enc = GetEncoding(charsetName);
                    return encoding == "B"
                        ? enc.GetString(Convert.FromBase64String(encoded))
                        : DecodeQEncodedHeader(encoded, enc);
                }
                catch
                {
                    return match.Value;
                }
            });
    }

    /// <summary>
    /// 解码 Q-encoded 头部（RFC 2047 Quoted-Printable 变体）。
    /// 下划线 '_' 代表空格。
    /// </summary>
    private static string DecodeQEncodedHeader(string text, Encoding encoding)
    {
        text = text.Replace('_', ' ');
        List<byte> bytes = [];

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '=' && i + 2 < text.Length &&
                byte.TryParse(text.AsSpan(i + 1, 2), NumberStyles.HexNumber, null, out byte b))
            {
                bytes.Add(b);
                i += 2;
            }
            else
            {
                bytes.AddRange(Encoding.ASCII.GetBytes(text[i].ToString()));
            }
        }

        return encoding.GetString(bytes.ToArray());
    }

    // ------------------------------------------------------------------ //
    //  正文解码
    // ------------------------------------------------------------------ //

    private static string DecodeBody(string body, string transferEncoding, string charset)
    {
        return transferEncoding switch
        {
            "base64"            => DecodeBase64Body(body, charset),
            "quoted-printable"  => DecodeQuotedPrintableBody(body, charset),
            _                   => body
        };
    }

    private static string DecodeBase64Body(string body, string charset)
    {
        try
        {
            // 去除换行符后再解码
            string cleaned = Regex.Replace(body, @"\s", "");
            if (string.IsNullOrEmpty(cleaned)) return string.Empty;
            byte[] bytes = Convert.FromBase64String(cleaned);
            return GetEncoding(charset).GetString(bytes);
        }
        catch
        {
            return body;
        }
    }

    private static string DecodeQuotedPrintableBody(string body, string charset)
    {
        try
        {
            Encoding enc = GetEncoding(charset);

            // 处理软换行（行尾 "=" 表示续行）
            body = Regex.Replace(body, @"=\r?\n", "");

            List<byte> bytes = [];
            for (int i = 0; i < body.Length; i++)
            {
                if (body[i] == '='
                    && i + 2 < body.Length
                    && body[i + 1] != '\r' && body[i + 1] != '\n'
                    && byte.TryParse(body.AsSpan(i + 1, 2), NumberStyles.HexNumber, null, out byte b))
                {
                    bytes.Add(b);
                    i += 2;
                }
                else if (body[i] == '\r' && i + 1 < body.Length && body[i + 1] == '\n')
                {
                    bytes.AddRange(enc.GetBytes("\r\n"));
                    i++;
                }
                else
                {
                    bytes.AddRange(enc.GetBytes(body[i].ToString()));
                }
            }

            return enc.GetString(bytes.ToArray());
        }
        catch
        {
            return body;
        }
    }

    // ------------------------------------------------------------------ //
    //  多段邮件（Multipart）处理
    // ------------------------------------------------------------------ //

    /// <summary>
    /// 从 multipart 邮件中提取可读文本。
    /// 优先选取 text/plain 部分；若无则选取 text/html 并剥除标签；
    /// 若存在嵌套 multipart/alternative，递归处理。
    /// </summary>
    private static string ExtractTextFromMultipart(string body, string boundary)
    {
        string delimiter = "--" + boundary;

        // 按边界切割
        string[] parts = body.Split([delimiter], StringSplitOptions.RemoveEmptyEntries);

        string? textPlain = null;
        string? textHtml  = null;

        foreach (string part in parts)
        {
            string trimmedPart = part.TrimStart('\r', '\n');

            // 结束边界 "--" 开头，跳过
            if (trimmedPart.StartsWith("--", StringComparison.Ordinal)) continue;

            (string partHeaders, string partBody) = SplitHeadersAndBody(trimmedPart);
            Dictionary<string, string> headers = ParseHeaders(partHeaders);

            string contentType       = GetHeaderValue(headers, "content-type");
            string transferEncoding  = GetHeaderValue(headers, "content-transfer-encoding").Trim().ToLowerInvariant();
            string charset           = ExtractCharset(contentType);

            if (contentType.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase))
            {
                // 嵌套 multipart，递归提取
                string nestedBoundary = ExtractBoundary(contentType);
                if (!string.IsNullOrEmpty(nestedBoundary))
                {
                    string nested = ExtractTextFromMultipart(partBody, nestedBoundary);
                    if (!string.IsNullOrWhiteSpace(nested))
                    {
                        textPlain ??= nested;
                    }
                }
            }
            else if (contentType.StartsWith("text/plain", StringComparison.OrdinalIgnoreCase) && textPlain is null)
            {
                textPlain = DecodeBody(partBody, transferEncoding, charset);
            }
            else if (contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) && textHtml is null)
            {
                string decoded = DecodeBody(partBody, transferEncoding, charset);
                textHtml = StripHtmlTags(decoded);
            }
        }

        return textPlain ?? textHtml ?? body;
    }

    private static string StripHtmlTags(string html)
    {
        if (string.IsNullOrEmpty(html)) return html;

        // 将常见块级标签替换为换行，再去除所有剩余标签
        html = Regex.Replace(html, @"<(br|BR|p|P|div|DIV)[^>]*>", "\n");
        html = Regex.Replace(html, @"<[^>]+>", string.Empty);

        // 解码常见 HTML 实体
        html = html
            .Replace("&amp;",  "&")
            .Replace("&lt;",   "<")
            .Replace("&gt;",   ">")
            .Replace("&quot;", "\"")
            .Replace("&nbsp;", " ");

        return html.Trim();
    }

    // ------------------------------------------------------------------ //
    //  工具方法
    // ------------------------------------------------------------------ //

    private static string ExtractCharset(string contentType)
    {
        Match m = Regex.Match(contentType, @"charset\s*=\s*""?([^""\s;]+)""?", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : "utf-8";
    }

    private static string ExtractBoundary(string contentType)
    {
        Match m = Regex.Match(contentType, @"boundary\s*=\s*""?([^""\s;]+)""?", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : string.Empty;
    }

    private static Encoding GetEncoding(string charset)
    {
        // 注册编码提供程序，支持 GBK / GB2312 / GB18030 等
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        try
        {
            return Encoding.GetEncoding(charset);
        }
        catch
        {
            return Encoding.UTF8;
        }
    }

    private static DateTimeOffset ParseDateHeader(string dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr)) return DateTimeOffset.Now;

        // RFC 2822 日期格式：Mon, 16 Jun 2026 15:30:00 +0800
        // DateTimeOffset.TryParse 能处理大多数合规格式
        if (DateTimeOffset.TryParse(dateStr,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTimeOffset result))
        {
            return result;
        }

        // 去掉星期前缀再试一次
        string stripped = Regex.Replace(dateStr, @"^\s*\w+\s*,\s*", "");
        if (DateTimeOffset.TryParse(stripped,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTimeOffset result2))
        {
            return result2;
        }

        return DateTimeOffset.Now;
    }

    private static string NormalizeLineEndings(string content)
    {
        return content.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", "\r\n");
    }

    // ------------------------------------------------------------------ //
    //  BuildSmtpContent 辅助方法
    // ------------------------------------------------------------------ //

    private static string EncodeHeader(string value)
    {
        if (string.IsNullOrEmpty(value) || IsAsciiOnly(value))
        {
            return value;
        }

        return $"=?utf-8?B?{Convert.ToBase64String(Encoding.UTF8.GetBytes(value))}?=";
    }

    private static string SanitizeHeaderValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Replace("\r", " ").Replace("\n", " ").Trim();
    }

    private static void AppendBase64Body(StringBuilder builder, string body)
    {
        if (string.IsNullOrEmpty(body)) return;

        string normalized = body.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", "\r\n");
        string encoded    = Convert.ToBase64String(Encoding.UTF8.GetBytes(normalized));

        for (int i = 0; i < encoded.Length; i += 76)
        {
            int chunkLen = Math.Min(76, encoded.Length - i);
            builder.AppendLine(encoded.Substring(i, chunkLen));
        }
    }

    private static bool IsAsciiOnly(string value)
    {
        foreach (char c in value)
        {
            if (c > 127) return false;
        }
        return true;
    }
}
