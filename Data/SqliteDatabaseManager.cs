using MailClient.Contracts;
using MailClient.Models;
using Microsoft.Data.Sqlite;

namespace MailClient.Data;

public sealed class SqliteDatabaseManager : IDatabaseManager
{
    public string DatabasePath { get; }

    public SqliteDatabaseManager(string databasePath)
    {
        DatabasePath = databasePath;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        string? directory = Path.GetDirectoryName(Path.GetFullPath(DatabasePath));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using SqliteConnection connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await ExecuteNonQueryAsync(connection, DatabaseSchema.AccountConfigTable, cancellationToken);
        await ExecuteNonQueryAsync(connection, DatabaseSchema.MailSummaryTable, cancellationToken);
        await ExecuteNonQueryAsync(connection, DatabaseSchema.SentMailTable, cancellationToken);
        await ExecuteNonQueryAsync(connection, DatabaseSchema.OperationLogTable, cancellationToken);
    }

    public async Task SaveAccountConfigAsync(AccountConfig config, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);

        await using SqliteConnection connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using SqliteTransaction transaction = connection.BeginTransaction();

        await using (SqliteCommand deleteCommand = connection.CreateCommand())
        {
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = "DELETE FROM account_config;";
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (SqliteCommand insertCommand = connection.CreateCommand())
        {
            insertCommand.Transaction = transaction;
            insertCommand.CommandText = """
                INSERT INTO account_config (
                    email,
                    smtp_server,
                    smtp_port,
                    pop3_server,
                    pop3_port,
                    remember_account,
                    create_time,
                    update_time
                )
                VALUES (
                    $email,
                    $smtp_server,
                    $smtp_port,
                    $pop3_server,
                    $pop3_port,
                    $remember_account,
                    $create_time,
                    $update_time
                );
                """;
            AddParameter(insertCommand, "$email", config.Email);
            AddParameter(insertCommand, "$smtp_server", config.SmtpServer);
            AddParameter(insertCommand, "$smtp_port", config.SmtpPort);
            AddParameter(insertCommand, "$pop3_server", config.Pop3Server);
            AddParameter(insertCommand, "$pop3_port", config.Pop3Port);
            AddParameter(insertCommand, "$remember_account", config.RememberAccount ? 1 : 0);
            AddParameter(insertCommand, "$create_time", DateTimeOffset.Now.ToString("O"));
            AddParameter(insertCommand, "$update_time", DateTimeOffset.Now.ToString("O"));

            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<AccountConfig?> LoadAccountConfigAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);

        await using SqliteConnection connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT email, smtp_server, smtp_port, pop3_server, pop3_port, remember_account
            FROM account_config
            ORDER BY update_time DESC, id DESC
            LIMIT 1;
            """;

        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new AccountConfig
        {
            Email = reader.GetString(0),
            PasswordOrAuthCode = string.Empty,
            SmtpServer = reader.GetString(1),
            SmtpPort = reader.GetInt32(2),
            Pop3Server = reader.GetString(3),
            Pop3Port = reader.GetInt32(4),
            RememberAccount = reader.GetInt32(5) == 1
        };
    }

    public async Task SaveMailSummaryAsync(MailSummary summary, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);

        await using SqliteConnection connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO mail_summary (
                mail_no,
                sender,
                receiver,
                subject,
                mail_date,
                size,
                status,
                receive_time
            )
            VALUES (
                $mail_no,
                $sender,
                $receiver,
                $subject,
                $mail_date,
                $size,
                $status,
                $receive_time
            );
            """;
        AddParameter(command, "$mail_no", summary.MailNo);
        AddParameter(command, "$sender", summary.Sender);
        AddParameter(command, "$receiver", summary.Receiver);
        AddParameter(command, "$subject", summary.Subject);
        AddParameter(command, "$mail_date", summary.MailDate);
        AddParameter(command, "$size", summary.Size);
        AddParameter(command, "$status", summary.Status.ToString().ToLowerInvariant());
        AddParameter(command, "$receive_time", DateTimeOffset.Now.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task SaveSentMailAsync(MailMessageModel mail, SmtpSendResult result, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);

        await using SqliteConnection connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO sent_mail (
                sender,
                receiver,
                subject,
                body,
                send_status,
                send_time,
                error_message
            )
            VALUES (
                $sender,
                $receiver,
                $subject,
                $body,
                $send_status,
                $send_time,
                $error_message
            );
            """;
        AddParameter(command, "$sender", mail.Sender);
        AddParameter(command, "$receiver", mail.Receiver);
        AddParameter(command, "$subject", mail.Subject);
        AddParameter(command, "$body", mail.Body);
        AddParameter(command, "$send_status", result.Success ? "success" : "failed");
        AddParameter(command, "$send_time", mail.Date.ToString("O"));
        AddParameter(command, "$error_message", result.Success ? string.Empty : result.Message);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task SaveOperationLogAsync(OperationLogEntry entry, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);

        await using SqliteConnection connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO operation_log (
                protocol,
                operation,
                content,
                level,
                create_time
            )
            VALUES (
                $protocol,
                $operation,
                $content,
                $level,
                $create_time
            );
            """;
        AddParameter(command, "$protocol", entry.Protocol);
        AddParameter(command, "$operation", entry.Operation);
        AddParameter(command, "$content", entry.Content);
        AddParameter(command, "$level", entry.Level.ToString().ToUpperInvariant());
        AddParameter(command, "$create_time", entry.CreateTime.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private SqliteConnection CreateConnection()
    {
        SqliteConnectionStringBuilder builder = new()
        {
            DataSource = DatabasePath
        };

        return new SqliteConnection(builder.ToString());
    }

    private static async Task ExecuteNonQueryAsync(
        SqliteConnection connection,
        string commandText,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = commandText;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameter(SqliteCommand command, string name, object? value)
    {
        command.Parameters.AddWithValue(name, value ?? DBNull.Value);
    }
}
