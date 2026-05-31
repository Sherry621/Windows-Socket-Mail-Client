namespace MailClient.Models;

public sealed class MailSummary
{
    public int MailNo { get; set; }
    public string Sender { get; set; } = string.Empty;
    public string Receiver { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string MailDate { get; set; } = string.Empty;
    public int Size { get; set; }
    public MailStatus Status { get; set; } = MailStatus.Unread;
}

public enum MailStatus
{
    Unread,
    Read,
    Deleted
}
