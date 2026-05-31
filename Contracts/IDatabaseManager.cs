using MailClient.Models;

namespace MailClient.Contracts;

public interface IDatabaseManager
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task SaveAccountConfigAsync(AccountConfig config, CancellationToken cancellationToken = default);
    Task<AccountConfig?> LoadAccountConfigAsync(CancellationToken cancellationToken = default);
    Task SaveMailSummaryAsync(MailSummary summary, CancellationToken cancellationToken = default);
    Task SaveSentMailAsync(MailMessageModel mail, SmtpSendResult result, CancellationToken cancellationToken = default);
    Task SaveOperationLogAsync(OperationLogEntry entry, CancellationToken cancellationToken = default);
}
