namespace MailClient.Data;

public static class DatabaseSchema
{
    public const string AccountConfigTable = """
        CREATE TABLE IF NOT EXISTS account_config (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            email TEXT NOT NULL,
            smtp_server TEXT NOT NULL,
            smtp_port INTEGER NOT NULL,
            pop3_server TEXT NOT NULL,
            pop3_port INTEGER NOT NULL,
            remember_account INTEGER DEFAULT 1,
            create_time TEXT,
            update_time TEXT
        );
        """;

    public const string MailSummaryTable = """
        CREATE TABLE IF NOT EXISTS mail_summary (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            mail_no INTEGER,
            sender TEXT,
            receiver TEXT,
            subject TEXT,
            mail_date TEXT,
            size INTEGER,
            status TEXT,
            receive_time TEXT
        );
        """;

    public const string SentMailTable = """
        CREATE TABLE IF NOT EXISTS sent_mail (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            sender TEXT,
            receiver TEXT,
            subject TEXT,
            body TEXT,
            send_status TEXT,
            send_time TEXT,
            error_message TEXT
        );
        """;

    public const string OperationLogTable = """
        CREATE TABLE IF NOT EXISTS operation_log (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            protocol TEXT,
            operation TEXT,
            content TEXT,
            level TEXT,
            create_time TEXT
        );
        """;
}
