using MailClient.Contracts;
using MailClient.Models;

namespace MailClient.Data;

public sealed class SqliteDatabaseManager : IDatabaseManager
{
    public string DatabasePath { get; }

    public SqliteDatabaseManager(string databasePath)
    {
        DatabasePath = databasePath;
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SaveAccountConfigAsync(AccountConfig config, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<AccountConfig?> LoadAccountConfigAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<AccountConfig?>(null);
    }

    public Task SaveMailSummaryAsync(MailSummary summary, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SaveSentMailAsync(MailMessageModel mail, SmtpSendResult result, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SaveOperationLogAsync(OperationLogEntry entry, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
