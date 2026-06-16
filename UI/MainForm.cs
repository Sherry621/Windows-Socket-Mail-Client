using MailClient.Contracts;
using MailClient.Data;
using MailClient.Models;
using MailClient.Protocols;
using MailClient.Services;
using System.Drawing.Drawing2D;

namespace MailClient.UI;

public sealed class MainForm : Form
{
    private static readonly Color AppBackground = Color.FromArgb(245, 247, 250);
    private static readonly Color Surface = Color.White;
    private static readonly Color Border = Color.FromArgb(218, 225, 233);
    private static readonly Color Primary = Color.FromArgb(24, 102, 189);
    private static readonly Color PrimaryDark = Color.FromArgb(16, 78, 146);
    private static readonly Color TextMain = Color.FromArgb(31, 41, 55);
    private static readonly Color TextMuted = Color.FromArgb(100, 116, 139);
    private static readonly Color HeaderBackground = Color.FromArgb(30, 41, 59);
    private static readonly Color HeaderAccent = Color.FromArgb(14, 116, 144);

    private readonly ILogManager logManager;
    private readonly IMailParser mailParser;
    private readonly IDatabaseManager databaseManager;
    private readonly ISmtpClientSocket smtpClient;
    private readonly IPop3ClientSocket pop3Client;

    private readonly TextBox emailTextBox = new();
    private readonly TextBox authCodeTextBox = new();
    private readonly TextBox smtpServerTextBox = new();
    private readonly NumericUpDown smtpPortInput = new();
    private readonly TextBox pop3ServerTextBox = new();
    private readonly NumericUpDown pop3PortInput = new();

    private readonly TextBox receiverTextBox = new();
    private readonly TextBox subjectTextBox = new();
    private readonly TextBox bodyTextBox = new();

    private readonly DataGridView inboxGrid = new();
    private readonly TextBox detailFromTextBox = new();
    private readonly TextBox detailToTextBox = new();
    private readonly TextBox detailSubjectTextBox = new();
    private readonly TextBox detailDateTextBox = new();
    private readonly TextBox detailBodyTextBox = new();

    private readonly DataGridView sentGrid = new();
    private readonly TextBox logTextBox = new();
    private readonly ToolStripStatusLabel statusLabel = new("就绪");

    private readonly BindingSource inboxBindingSource = new();
    private readonly BindingSource sentBindingSource = new();
    private readonly List<SentMailViewModel> sentMails = [];

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
        MinimumSize = new Size(1180, 820);
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = AppBackground;

        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(14),
            BackColor = AppBackground
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 144));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        root.Controls.Add(CreateHeaderPanel(), 0, 0);
        root.Controls.Add(CreateAccountPanel(), 0, 1);
        root.Controls.Add(CreateTabs(), 0, 2);
        root.Controls.Add(CreateStatusBar(), 0, 3);
        Controls.Add(root);
    }

    private Control CreateHeaderPanel()
    {
        Panel panel = new()
        {
            Dock = DockStyle.Fill,
            BackColor = HeaderBackground,
            Padding = new Padding(20, 14, 20, 14),
            Margin = new Padding(0, 0, 0, 12)
        };
        panel.Paint += (_, e) =>
        {
            using LinearGradientBrush brush = new(
                panel.ClientRectangle,
                HeaderBackground,
                HeaderAccent,
                LinearGradientMode.Horizontal);
            e.Graphics.FillRectangle(brush, panel.ClientRectangle);
        };

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360));

        TableLayoutPanel textLayout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent
        };
        textLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        textLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        textLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        Label title = new()
        {
            Text = "Windows Socket Mail Client",
            ForeColor = Color.White,
            Font = new Font("Microsoft YaHei UI", 17F, FontStyle.Bold),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        Label subtitle = new()
        {
            Text = "SMTP / POP3 协议实验客户端 · WinForms 界面模块",
            ForeColor = Color.FromArgb(203, 213, 225),
            Font = new Font("Microsoft YaHei UI", 9.5F),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        Label caption = new()
        {
            Text = "Socket command logs, account configuration and mail operations are organized in one desktop workspace.",
            ForeColor = Color.FromArgb(148, 163, 184),
            Font = new Font("Segoe UI", 8.5F),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        textLayout.Controls.Add(title, 0, 0);
        textLayout.Controls.Add(subtitle, 0, 1);
        textLayout.Controls.Add(caption, 0, 2);

        FlowLayoutPanel chips = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 12, 0, 0)
        };
        chips.Controls.Add(CreateHeaderChip("SQLite"));
        chips.Controls.Add(CreateHeaderChip("Socket"));
        chips.Controls.Add(CreateHeaderChip("POP3"));
        chips.Controls.Add(CreateHeaderChip("SMTP"));

        layout.Controls.Add(textLayout, 0, 0);
        layout.Controls.Add(chips, 1, 0);
        panel.Controls.Add(layout);
        return panel;
    }

    private Control CreateAccountPanel()
    {
        GroupBox group = CreateSectionGroup("账号配置");

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 3,
            Padding = new Padding(16, 12, 16, 10),
            BackColor = Surface
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        AddLabel(layout, "邮箱", 0, 0);
        StyleTextBox(emailTextBox);
        layout.Controls.Add(emailTextBox, 1, 0);

        AddLabel(layout, "授权码", 2, 0);
        StyleTextBox(authCodeTextBox);
        authCodeTextBox.UseSystemPasswordChar = true;
        layout.Controls.Add(authCodeTextBox, 3, 0);

        AddLabel(layout, "SMTP服务器", 0, 1);
        StyleTextBox(smtpServerTextBox);
        layout.Controls.Add(smtpServerTextBox, 1, 1);

        AddLabel(layout, "SMTP端口", 2, 1);
        ConfigurePortInput(smtpPortInput, 25);
        layout.Controls.Add(smtpPortInput, 3, 1);

        AddLabel(layout, "POP3服务器", 0, 2);
        StyleTextBox(pop3ServerTextBox);
        layout.Controls.Add(pop3ServerTextBox, 1, 2);

        AddLabel(layout, "POP3端口", 2, 2);
        ConfigurePortInput(pop3PortInput, 110);
        layout.Controls.Add(pop3PortInput, 3, 2);

        FlowLayoutPanel buttons = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Surface
        };

        buttons.Controls.Add(CreateButton("保存配置", OnSaveConfigClicked, 96));
        buttons.Controls.Add(CreateButton("测试SMTP", OnTestSmtpClicked, 96));
        buttons.Controls.Add(CreateButton("测试POP3", OnTestPop3Clicked, 96));
        layout.Controls.Add(buttons, 4, 1);
        layout.SetColumnSpan(buttons, 2);

        group.Controls.Add(layout);
        return group;
    }

    private Control CreateTabs()
    {
        TabControl tabs = new()
        {
            Dock = DockStyle.Fill,
            Font = new Font("Microsoft YaHei UI", 9.5F),
            Margin = new Padding(0, 10, 0, 8)
        };

        tabs.TabPages.Add(CreateComposePage());
        tabs.TabPages.Add(CreateInboxPage());
        tabs.TabPages.Add(CreateSentPage());
        tabs.TabPages.Add(CreateLogPage());
        return tabs;
    }

    private TabPage CreateComposePage()
    {
        TabPage page = CreateTabPage("写邮件");

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(18),
            BackColor = Surface
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

        AddLabel(layout, "收件人", 0, 0);
        StyleTextBox(receiverTextBox);
        layout.Controls.Add(receiverTextBox, 1, 0);

        AddLabel(layout, "主题", 0, 1);
        StyleTextBox(subjectTextBox);
        layout.Controls.Add(subjectTextBox, 1, 1);

        AddLabel(layout, "正文", 0, 2);
        StyleTextBox(bodyTextBox);
        bodyTextBox.Multiline = true;
        bodyTextBox.ScrollBars = ScrollBars.Vertical;
        bodyTextBox.Font = new Font("Microsoft YaHei UI", 10F);
        layout.Controls.Add(bodyTextBox, 1, 2);

        FlowLayoutPanel buttons = CreateButtonRow();
        buttons.Controls.Add(CreateButton("发送", OnSendMailClicked, 90));
        buttons.Controls.Add(CreateSecondaryButton("清空", OnClearComposeClicked, 90));
        layout.Controls.Add(buttons, 1, 3);

        page.Controls.Add(layout);
        return page;
    }

    private TabPage CreateInboxPage()
    {
        TabPage page = CreateTabPage("收件箱");

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(18),
            BackColor = Surface
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 52));

        FlowLayoutPanel buttons = CreateButtonRow();
        buttons.Controls.Add(CreateButton("刷新收件箱", OnRefreshInboxClicked, 112));
        buttons.Controls.Add(CreateSecondaryButton("阅读", OnReadMailClicked, 90));
        buttons.Controls.Add(CreateDangerButton("删除", OnDeleteMailClicked, 90));
        layout.Controls.Add(buttons, 0, 0);

        ConfigureGrid(inboxGrid);
        inboxGrid.DataSource = inboxBindingSource;
        AddInboxColumns();
        layout.Controls.Add(inboxGrid, 0, 1);

        layout.Controls.Add(CreateMailDetailPanel(), 0, 2);
        page.Controls.Add(layout);
        return page;
    }

    private TabPage CreateSentPage()
    {
        TabPage page = CreateTabPage("已发送");

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(18),
            BackColor = Surface
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        FlowLayoutPanel buttons = CreateButtonRow();
        buttons.Controls.Add(CreateSecondaryButton("刷新", OnRefreshSentClicked, 90));
        layout.Controls.Add(buttons, 0, 0);

        ConfigureGrid(sentGrid);
        sentGrid.DataSource = sentBindingSource;
        AddSentColumns();
        layout.Controls.Add(sentGrid, 0, 1);

        page.Controls.Add(layout);
        return page;
    }

    private TabPage CreateLogPage()
    {
        TabPage page = CreateTabPage("日志");

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(18),
            BackColor = Surface
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        FlowLayoutPanel buttons = CreateButtonRow();
        buttons.Controls.Add(CreateSecondaryButton("清空日志", OnClearLogClicked, 100));
        layout.Controls.Add(buttons, 0, 0);

        StyleTextBox(logTextBox);
        logTextBox.Dock = DockStyle.Fill;
        logTextBox.Multiline = true;
        logTextBox.ReadOnly = true;
        logTextBox.ScrollBars = ScrollBars.Vertical;
        logTextBox.Font = new Font("Consolas", 9.5F);
        logTextBox.BackColor = Color.FromArgb(15, 23, 42);
        logTextBox.ForeColor = Color.FromArgb(226, 232, 240);
        layout.Controls.Add(logTextBox, 0, 1);

        page.Controls.Add(layout);
        return page;
    }

    private Control CreateMailDetailPanel()
    {
        GroupBox group = CreateSectionGroup("邮件详情");

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 4,
            Padding = new Padding(14),
            BackColor = Surface
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        AddReadOnlyField(layout, "发件人", detailFromTextBox, 0, 0);
        AddReadOnlyField(layout, "收件人", detailToTextBox, 2, 0);
        AddReadOnlyField(layout, "主题", detailSubjectTextBox, 0, 1, columnSpan: 3);
        AddReadOnlyField(layout, "日期", detailDateTextBox, 0, 2, columnSpan: 3);

        AddLabel(layout, "正文", 0, 3);
        StyleTextBox(detailBodyTextBox);
        detailBodyTextBox.Dock = DockStyle.Fill;
        detailBodyTextBox.Multiline = true;
        detailBodyTextBox.ReadOnly = true;
        detailBodyTextBox.ScrollBars = ScrollBars.Vertical;
        detailBodyTextBox.BackColor = Color.FromArgb(248, 250, 252);
        layout.Controls.Add(detailBodyTextBox, 1, 3);
        layout.SetColumnSpan(detailBodyTextBox, 3);

        group.Controls.Add(layout);
        return group;
    }

    private Control CreateStatusBar()
    {
        StatusStrip statusStrip = new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(234, 239, 246),
            SizingGrip = false
        };
        statusLabel.Spring = true;
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        statusLabel.ForeColor = TextMain;
        statusStrip.Items.Add(statusLabel);
        return statusStrip;
    }

    private void BindEvents()
    {
        Load += OnFormLoaded;
        logManager.LogAdded += OnLogAdded;
        logManager.Info("SYSTEM", "Startup", "界面初始化完成");
    }

    private async void OnFormLoaded(object? sender, EventArgs e)
    {
        try
        {
            await databaseManager.InitializeAsync();
            AccountConfig? config = await databaseManager.LoadAccountConfigAsync();
            if (config is not null)
            {
                ApplyConfig(config);
                SetStatus("已加载账号配置");
            }
        }
        catch (Exception ex)
        {
            SetStatus("初始化数据库失败");
            logManager.Error("SYSTEM", "Initialize", ex.Message);
        }
    }

    private async void OnSaveConfigClicked(object? sender, EventArgs e)
    {
        if (!TryGetConfig(out AccountConfig config))
        {
            return;
        }

        try
        {
            await databaseManager.SaveAccountConfigAsync(config);
            SetStatus("账号配置已保存");
            logManager.Success("UI", "SaveConfig", "账号配置已保存");
        }
        catch (Exception ex)
        {
            SetStatus("保存账号配置失败");
            logManager.Error("UI", "SaveConfig", ex.Message);
        }
    }

    private async void OnTestSmtpClicked(object? sender, EventArgs e)
    {
        if (!TryGetConfig(out AccountConfig config))
        {
            return;
        }

        await RunUiActionAsync("正在测试 SMTP 连接...", "SMTP 连接测试完成", async () =>
        {
            await smtpClient.ConnectAsync(config);
            await smtpClient.QuitAsync();
        });
    }

    private async void OnTestPop3Clicked(object? sender, EventArgs e)
    {
        if (!TryGetConfig(out AccountConfig config))
        {
            return;
        }

        await RunUiActionAsync("正在测试 POP3 连接...", "POP3 连接测试完成", async () =>
        {
            await pop3Client.ConnectAsync(config);
            await pop3Client.LoginAsync(config);
            await pop3Client.QuitAsync();
        });
    }

    private async void OnSendMailClicked(object? sender, EventArgs e)
    {
        if (!TryGetConfig(out AccountConfig config) || !TryGetComposeMail(config, out MailMessageModel mail))
        {
            return;
        }

        SetStatus("正在发送邮件...");
        SmtpSendResult result;

        try
        {
            result = await smtpClient.SendMailAsync(config, mail);
            await databaseManager.SaveSentMailAsync(mail, result);
            AddSentMail(mail, result);

            if (result.Success)
            {
                SetStatus("邮件发送成功");
                logManager.Success("SMTP", "SendMail", result.Message);
            }
            else
            {
                SetStatus($"邮件发送失败：{result.Message}");
                logManager.Error("SMTP", "SendMail", result.Message);
            }
        }
        catch (Exception ex)
        {
            result = new SmtpSendResult { Success = false, Message = ex.Message };
            await databaseManager.SaveSentMailAsync(mail, result);
            AddSentMail(mail, result);
            SetStatus("邮件发送异常");
            logManager.Error("SMTP", "SendMail", ex.Message);
        }
    }

    private void OnClearComposeClicked(object? sender, EventArgs e)
    {
        receiverTextBox.Clear();
        subjectTextBox.Clear();
        bodyTextBox.Clear();
        SetStatus("写邮件内容已清空");
    }

    private async void OnRefreshInboxClicked(object? sender, EventArgs e)
    {
        if (!TryGetConfig(out AccountConfig config))
        {
            return;
        }

        SetStatus("正在刷新收件箱...");

        try
        {
            await pop3Client.ConnectAsync(config);
            await pop3Client.LoginAsync(config);
            Pop3MailboxStat stat = await pop3Client.GetStatAsync();
            IReadOnlyList<MailSummary> mails = await pop3Client.ListMailsAsync();
            await pop3Client.QuitAsync();

            inboxBindingSource.DataSource = mails.ToList();
            SetStatus($"收件箱刷新完成，共 {stat.MailCount} 封邮件");
            logManager.Success("POP3", "RefreshInbox", $"邮件数量：{stat.MailCount}，总大小：{stat.TotalSize} 字节");
        }
        catch (Exception ex)
        {
            SetStatus("刷新收件箱失败");
            logManager.Error("POP3", "RefreshInbox", ex.Message);
        }
    }

    private async void OnReadMailClicked(object? sender, EventArgs e)
    {
        if (!TryGetSelectedMailNo(out int mailNo))
        {
            return;
        }

        SetStatus("正在读取邮件...");

        try
        {
            MailMessageModel mail = await pop3Client.RetrieveMailAsync(mailNo);
            ShowMailDetail(mail);
            SetStatus($"已读取邮件 #{mailNo}");
            logManager.Success("POP3", "RetrieveMail", $"已读取邮件 #{mailNo}");
        }
        catch (Exception ex)
        {
            SetStatus("读取邮件失败");
            logManager.Error("POP3", "RetrieveMail", ex.Message);
        }
    }

    private async void OnDeleteMailClicked(object? sender, EventArgs e)
    {
        if (!TryGetSelectedMailNo(out int mailNo))
        {
            return;
        }

        DialogResult result = MessageBox.Show(
            $"确定要删除服务器上的第 {mailNo} 封邮件吗？",
            "确认删除",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
        {
            return;
        }

        try
        {
            await pop3Client.DeleteMailAsync(mailNo);
            SetStatus($"已发送删除邮件 #{mailNo} 的请求");
            logManager.Success("POP3", "DeleteMail", $"已删除邮件 #{mailNo}");
        }
        catch (Exception ex)
        {
            SetStatus("删除邮件失败");
            logManager.Error("POP3", "DeleteMail", ex.Message);
        }
    }

    private void OnRefreshSentClicked(object? sender, EventArgs e)
    {
        sentBindingSource.ResetBindings(false);
        SetStatus("已发送列表已刷新");
    }

    private void OnClearLogClicked(object? sender, EventArgs e)
    {
        logManager.Clear();
        logTextBox.Clear();
        SetStatus("日志已清空");
    }

    private void OnLogAdded(object? sender, OperationLogEntry entry)
    {
        string line = $"[{entry.CreateTime:HH:mm:ss}] [{entry.Protocol}] [{entry.Level}] {entry.Operation} - {entry.Content}{Environment.NewLine}";

        if (InvokeRequired)
        {
            BeginInvoke(() => logTextBox.AppendText(line));
            return;
        }

        logTextBox.AppendText(line);
    }

    private bool TryGetConfig(out AccountConfig config)
    {
        config = new AccountConfig
        {
            Email = emailTextBox.Text.Trim(),
            PasswordOrAuthCode = authCodeTextBox.Text,
            SmtpServer = smtpServerTextBox.Text.Trim(),
            SmtpPort = (int)smtpPortInput.Value,
            Pop3Server = pop3ServerTextBox.Text.Trim(),
            Pop3Port = (int)pop3PortInput.Value
        };

        if (string.IsNullOrWhiteSpace(config.Email))
        {
            ShowValidationError("请填写邮箱地址。");
            return false;
        }

        if (string.IsNullOrWhiteSpace(config.PasswordOrAuthCode))
        {
            ShowValidationError("请填写密码或授权码。");
            return false;
        }

        if (string.IsNullOrWhiteSpace(config.SmtpServer))
        {
            ShowValidationError("请填写 SMTP 服务器。");
            return false;
        }

        if (string.IsNullOrWhiteSpace(config.Pop3Server))
        {
            ShowValidationError("请填写 POP3 服务器。");
            return false;
        }

        return true;
    }

    private bool TryGetComposeMail(AccountConfig config, out MailMessageModel mail)
    {
        mail = new MailMessageModel
        {
            Sender = config.Email,
            Receiver = receiverTextBox.Text.Trim(),
            Subject = subjectTextBox.Text.Trim(),
            Body = bodyTextBox.Text,
            Date = DateTimeOffset.Now
        };

        if (string.IsNullOrWhiteSpace(mail.Receiver))
        {
            ShowValidationError("请填写收件人。");
            return false;
        }

        if (string.IsNullOrWhiteSpace(mail.Subject))
        {
            ShowValidationError("请填写邮件主题。");
            return false;
        }

        if (string.IsNullOrWhiteSpace(mail.Body))
        {
            DialogResult result = MessageBox.Show(
                "邮件正文为空，是否继续发送？",
                "确认发送",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            return result == DialogResult.Yes;
        }

        return true;
    }

    private void ApplyConfig(AccountConfig config)
    {
        emailTextBox.Text = config.Email;
        authCodeTextBox.Text = config.PasswordOrAuthCode;
        smtpServerTextBox.Text = config.SmtpServer;
        smtpPortInput.Value = ClampPort(config.SmtpPort);
        pop3ServerTextBox.Text = config.Pop3Server;
        pop3PortInput.Value = ClampPort(config.Pop3Port);
    }

    private void ShowMailDetail(MailMessageModel mail)
    {
        detailFromTextBox.Text = mail.Sender;
        detailToTextBox.Text = mail.Receiver;
        detailSubjectTextBox.Text = mail.Subject;
        detailDateTextBox.Text = mail.Date.ToString("yyyy-MM-dd HH:mm:ss zzz");
        detailBodyTextBox.Text = string.IsNullOrWhiteSpace(mail.Body) ? mail.RawContent : mail.Body;
    }

    private void AddSentMail(MailMessageModel mail, SmtpSendResult result)
    {
        sentMails.Add(new SentMailViewModel
        {
            Receiver = mail.Receiver,
            Subject = mail.Subject,
            SendTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Status = result.Success ? "成功" : "失败",
            Message = result.Message
        });

        sentBindingSource.DataSource = null;
        sentBindingSource.DataSource = sentMails;
    }

    private bool TryGetSelectedMailNo(out int mailNo)
    {
        mailNo = 0;

        if (inboxGrid.CurrentRow?.DataBoundItem is not MailSummary summary)
        {
            ShowValidationError("请先在收件箱中选择一封邮件。");
            return false;
        }

        mailNo = summary.MailNo;
        return true;
    }

    private async Task RunUiActionAsync(string runningMessage, string successMessage, Func<Task> action)
    {
        SetStatus(runningMessage);

        try
        {
            await action();
            SetStatus(successMessage);
            logManager.Success("UI", "Action", successMessage);
        }
        catch (Exception ex)
        {
            SetStatus("操作失败");
            logManager.Error("UI", "Action", ex.Message);
        }
    }

    private void ShowValidationError(string message)
    {
        SetStatus(message);
        MessageBox.Show(message, "输入检查", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void SetStatus(string message)
    {
        statusLabel.Text = message;
    }

    private static GroupBox CreateSectionGroup(string title)
    {
        return new GroupBox
        {
            Text = title,
            Dock = DockStyle.Fill,
            BackColor = Surface,
            ForeColor = TextMain,
            Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold),
            Padding = new Padding(8),
            Margin = new Padding(0, 0, 0, 8)
        };
    }

    private static TabPage CreateTabPage(string title)
    {
        return new TabPage(title)
        {
            BackColor = Surface,
            Padding = new Padding(0)
        };
    }

    private static Label CreateHeaderChip(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = false,
            Width = 72,
            Height = 28,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(46, 125, 154),
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            Margin = new Padding(8, 0, 0, 8)
        };
    }

    private static FlowLayoutPanel CreateButtonRow()
    {
        return new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Surface,
            Padding = new Padding(0, 4, 0, 0)
        };
    }

    private static void AddLabel(TableLayoutPanel layout, string text, int column, int row)
    {
        layout.Controls.Add(new Label
        {
            Text = text,
            Anchor = AnchorStyles.Left,
            AutoSize = true,
            ForeColor = TextMuted,
            Font = new Font("Microsoft YaHei UI", 9F)
        }, column, row);
    }

    private static void AddReadOnlyField(TableLayoutPanel layout, string label, TextBox textBox, int column, int row, int columnSpan = 1)
    {
        AddLabel(layout, label, column, row);
        StyleTextBox(textBox);
        textBox.ReadOnly = true;
        textBox.BackColor = Color.FromArgb(248, 250, 252);
        layout.Controls.Add(textBox, column + 1, row);
        layout.SetColumnSpan(textBox, columnSpan);
    }

    private static void StyleTextBox(TextBox textBox)
    {
        textBox.Dock = DockStyle.Fill;
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.ForeColor = TextMain;
        textBox.BackColor = Surface;
        textBox.Margin = new Padding(0, 3, 12, 5);
    }

    private static Button CreateButton(string text, EventHandler handler, int width = 88)
    {
        return CreateStyledButton(text, handler, width, Primary, Color.White);
    }

    private static Button CreateSecondaryButton(string text, EventHandler handler, int width = 88)
    {
        return CreateStyledButton(text, handler, width, Color.FromArgb(226, 232, 240), TextMain);
    }

    private static Button CreateDangerButton(string text, EventHandler handler, int width = 88)
    {
        return CreateStyledButton(text, handler, width, Color.FromArgb(220, 38, 38), Color.White);
    }

    private static Button CreateStyledButton(string text, EventHandler handler, int width, Color backColor, Color foreColor)
    {
        Button button = new()
        {
            Text = text,
            Width = width,
            Height = 32,
            BackColor = backColor,
            ForeColor = foreColor,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 3, 10, 3)
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = backColor == Primary ? PrimaryDark : ControlPaint.Dark(backColor, 0.06f);
        button.Click += handler;
        return button;
    }

    private static void ConfigurePortInput(NumericUpDown input, int defaultValue)
    {
        input.Dock = DockStyle.Left;
        input.Width = 120;
        input.Minimum = 1;
        input.Maximum = 65535;
        input.Value = defaultValue;
        input.BorderStyle = BorderStyle.FixedSingle;
        input.ForeColor = TextMain;
        input.BackColor = Surface;
        input.Margin = new Padding(0, 3, 12, 5);
    }

    private static void ConfigureGrid(DataGridView grid)
    {
        grid.Dock = DockStyle.Fill;
        grid.AutoGenerateColumns = false;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.ReadOnly = true;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.BorderStyle = BorderStyle.None;
        grid.BackgroundColor = Surface;
        grid.GridColor = Border;
        grid.RowHeadersVisible = false;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = TextMain;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        grid.ColumnHeadersHeight = 34;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
        grid.DefaultCellStyle.SelectionForeColor = TextMain;
        grid.DefaultCellStyle.ForeColor = TextMain;
        grid.DefaultCellStyle.BackColor = Surface;
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
        grid.RowTemplate.Height = 30;
    }

    private static decimal ClampPort(int port)
    {
        if (port < 1)
        {
            return 1;
        }

        if (port > 65535)
        {
            return 65535;
        }

        return port;
    }

    private void AddInboxColumns()
    {
        inboxGrid.Columns.Add(CreateTextColumn(nameof(MailSummary.MailNo), "编号", 70));
        inboxGrid.Columns.Add(CreateTextColumn(nameof(MailSummary.Sender), "发件人", 180));
        inboxGrid.Columns.Add(CreateTextColumn(nameof(MailSummary.Subject), "主题", 280));
        inboxGrid.Columns.Add(CreateTextColumn(nameof(MailSummary.MailDate), "日期", 160));
        inboxGrid.Columns.Add(CreateTextColumn(nameof(MailSummary.Size), "大小", 90));
        inboxGrid.Columns.Add(CreateTextColumn(nameof(MailSummary.Status), "状态", 90));
    }

    private void AddSentColumns()
    {
        sentGrid.Columns.Add(CreateTextColumn(nameof(SentMailViewModel.Receiver), "收件人", 220));
        sentGrid.Columns.Add(CreateTextColumn(nameof(SentMailViewModel.Subject), "主题", 300));
        sentGrid.Columns.Add(CreateTextColumn(nameof(SentMailViewModel.SendTime), "发送时间", 170));
        sentGrid.Columns.Add(CreateTextColumn(nameof(SentMailViewModel.Status), "状态", 90));
        sentGrid.Columns.Add(CreateTextColumn(nameof(SentMailViewModel.Message), "结果", 280));
    }

    private static DataGridViewTextBoxColumn CreateTextColumn(string propertyName, string headerText, int width)
    {
        return new DataGridViewTextBoxColumn
        {
            DataPropertyName = propertyName,
            HeaderText = headerText,
            Width = width
        };
    }

    private sealed class SentMailViewModel
    {
        public string Receiver { get; init; } = string.Empty;
        public string Subject { get; init; } = string.Empty;
        public string SendTime { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }
}
