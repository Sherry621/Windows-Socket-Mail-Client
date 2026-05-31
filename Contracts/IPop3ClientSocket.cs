using MailClient.Models;

namespace MailClient.Contracts;

public interface IPop3ClientSocket
{
    Task ConnectAsync(AccountConfig config, CancellationToken cancellationToken = default);
    Task LoginAsync(AccountConfig config, CancellationToken cancellationToken = default);
    Task<Pop3MailboxStat> GetStatAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MailSummary>> ListMailsAsync(CancellationToken cancellationToken = default);
    Task<MailMessageModel> RetrieveMailAsync(int mailNo, CancellationToken cancellationToken = default);
    Task DeleteMailAsync(int mailNo, CancellationToken cancellationToken = default);
    Task QuitAsync(CancellationToken cancellationToken = default);
}
