namespace MailClient.Models;

public sealed class AccountConfig
{
    public string Email { get; set; } = string.Empty;
    public string PasswordOrAuthCode { get; set; } = string.Empty;
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 25;
    public string Pop3Server { get; set; } = string.Empty;
    public int Pop3Port { get; set; } = 110;
    public bool RememberAccount { get; set; } = true;
}
