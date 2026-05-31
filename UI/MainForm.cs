using MailClient.Contracts;
using MailClient.Data;
using MailClient.Protocols;
using MailClient.Services;

namespace MailClient.UI;

public sealed class MainForm : Form
{
    private readonly ILogManager logManager;
    private readonly IMailParser mailParser;
    private readonly IDatabaseManager databaseManager;
    private readonly ISmtpClientSocket smtpClient;
    private readonly IPop3ClientSocket pop3Client;
    private readonly TextBox logTextBox = new();

    public MainForm()
    {
        logManager = new LogManager();
        mailParser = new MailParser();
        databaseManager = new SqliteDatabaseManager("mail_client.db");
        smtpClient = new SmtpClientSocket(logManager, mailParser);
        pop3Client = new Pop3ClientSocket(logManager, mailParser);

        InitializeComponent();
        BindEvents();
    }

    private void InitializeComponent()
    {
        Text = "基于 Socket 的邮件客户端";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(980, 680);

        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(CreateAccountPanel(), 0, 0);
        root.Controls.Add(CreateTabs(), 0, 1);
        Controls.Add(root);
    }

    private Control CreateAccountPanel()
    {
        GroupBox group = new()
        {
            Text = "账号配置",
            Dock = DockStyle.Fill
        };

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 3,
            Padding = new Padding(12)
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));

        AddLabeledText(layout, "邮箱", 0, 0);
        AddLabeledText(layout, "授权码", 2, 0, password: true);
        AddLabeledText(layout, "SMTP服务器", 0, 1);
        AddLabeledText(layout, "SMTP端口", 2, 1);
        AddLabeledText(layout, "POP3服务器", 0, 2);
        AddLabeledText(layout, "POP3端口", 2, 2);

        FlowLayoutPanel buttons = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight
        };
        buttons.Controls.Add(new Button { Text = "保存配置", Width = 96 });
        buttons.Controls.Add(new Button { Text = "测试SMTP", Width = 96 });
        buttons.Controls.Add(new Button { Text = "测试POP3", Width = 96 });
        layout.Controls.Add(buttons, 4, 2);
        layout.SetColumnSpan(buttons, 2);

        group.Controls.Add(layout);
        return group;
    }

    private static void AddLabeledText(TableLayoutPanel layout, string labelText, int column, int row, bool password = false)
    {
        layout.Controls.Add(new Label { Text = labelText, Anchor = AnchorStyles.Left, AutoSize = true }, column, row);
        layout.Controls.Add(new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = password }, column + 1, row);
    }

    private Control CreateTabs()
    {
        TabControl tabs = new()
        {
            Dock = DockStyle.Fill
        };

        tabs.TabPages.Add(CreateComposePage());
        tabs.TabPages.Add(CreateInboxPage());
        tabs.TabPages.Add(CreateSentPage());
        tabs.TabPages.Add(CreateLogPage());
        return tabs;
    }

    private static TabPage CreateComposePage()
    {
        TabPage page = new("写邮件");
        page.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "写邮件界面占位：后续接入收件人、主题、正文和发送按钮。",
            TextAlign = ContentAlignment.MiddleCenter
        });
        return page;
    }

    private static TabPage CreateInboxPage()
    {
        TabPage page = new("收件箱");
        page.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "收件箱界面占位：后续接入刷新、阅读、删除和邮件列表。",
            TextAlign = ContentAlignment.MiddleCenter
        });
        return page;
    }

    private static TabPage CreateSentPage()
    {
        TabPage page = new("已发送");
        page.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "已发送界面占位：后续从 SQLite 加载发送记录。",
            TextAlign = ContentAlignment.MiddleCenter
        });
        return page;
    }

    private TabPage CreateLogPage()
    {
        TabPage page = new("日志");
        logTextBox.Dock = DockStyle.Fill;
        logTextBox.Multiline = true;
        logTextBox.ReadOnly = true;
        logTextBox.ScrollBars = ScrollBars.Vertical;
        page.Controls.Add(logTextBox);
        return page;
    }

    private void BindEvents()
    {
        _ = databaseManager;
        _ = smtpClient;
        _ = pop3Client;

        logManager.LogAdded += (_, entry) =>
        {
            string line = $"[{entry.CreateTime:HH:mm:ss}] [{entry.Protocol}] [{entry.Level}] {entry.Content}{Environment.NewLine}";
            if (InvokeRequired)
            {
                BeginInvoke(() => logTextBox.AppendText(line));
                return;
            }

            logTextBox.AppendText(line);
        };

        logManager.Info("SYSTEM", "Startup", "System framework initialized.");
    }
}
