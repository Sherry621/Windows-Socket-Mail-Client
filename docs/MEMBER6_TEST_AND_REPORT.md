# 成员六 邮件解析、日志、测试与报告材料

本文档用于记录成员六负责内容的完成情况，并汇总对前五位成员模块的检查结论，便于后续撰写实验报告和准备答辩说明。

## 对前五位成员完成情况的检查

### 成员一：总体架构与接口

检查范围：

```text
Program.cs
Contracts/
docs/FRAMEWORK.md
docs/TEAM_DIVISION_6.md
```

检查结论：

```text
1. 项目已按 UI、Protocols、Networking、Data、Services、Models、Contracts 分层。
2. SMTP、POP3、数据库、日志、邮件解析均有独立接口。
3. UI 层主要通过接口调用功能模块，没有直接编写 SMTP/POP3 命令。
4. Program.cs 能启动 WinForms 主窗口。
```

评价：总体架构清晰，满足小组协作和实验报告中“模块划分”的要求。

### 成员二：SMTP 邮件发送模块

检查范围：

```text
Protocols/SmtpClientSocket.cs
Networking/SocketConnection.cs
Contracts/ISmtpClientSocket.cs
docs/SMTP_MODULE_MEMBER2.md
docs/SMTP_TEST_CHECKLIST.md
```

检查结论：

```text
1. 已使用 TcpClient 从 TCP 连接开始实现 SMTP 通信。
2. 已实现 EHLO/HELO、AUTH LOGIN、MAIL FROM、RCPT TO、DATA、QUIT。
3. 已实现 SMTP 多行响应读取和响应码校验。
4. 已对 DATA 阶段以 "." 开头的行进行 dot-stuffing。
5. AUTH LOGIN 阶段日志使用 <base64 username> 和 <base64 auth code> 脱敏显示。
```

评价：SMTP 基本发送流程完整，能支撑课程要求中的“邮件编写、发送”功能。当前限制是未实现 SSL/TLS、附件、多收件人等扩展功能，但不影响基础实验要求。

### 成员三：POP3 收件、阅读和删除模块

检查范围：

```text
Protocols/Pop3ClientSocket.cs
Contracts/IPop3ClientSocket.cs
docs/MEMBER3_DEV_LOG.md
```

检查结论：

```text
1. 已使用 TcpClient 从 TCP 连接开始实现 POP3 通信。
2. 已实现 USER、PASS、STAT、LIST、RETR、DELE、QUIT。
3. 已按 POP3 多行响应规则读取到单独一行 "." 结束。
4. 已实现 dot-unstuffing，能还原以 "." 开头的邮件正文行。
5. PASS 命令日志显示为 PASS <auth code>，不会输出授权码明文。
```

评价：POP3 协议核心命令完整，能支撑课程要求中的“接收、阅读、删除”功能。原 UI 阅读和删除按钮没有重新建立 POP3 会话，已在成员六联调时修正。

### 成员四：WinForms 图形界面模块

检查范围：

```text
UI/MainForm.cs
docs/UI_MODULE_MEMBER4.md
```

检查结论：

```text
1. 已实现账号配置区、写邮件页、收件箱页、已发送页和日志页。
2. 已绑定保存配置、测试 SMTP、测试 POP3、发送、刷新收件箱、阅读、删除等按钮事件。
3. 已实现基础输入校验和忙碌状态控制。
4. 已通过日志事件实时显示 SMTP/POP3 协议交互过程。
```

联调修正：

```text
阅读和删除邮件前现在会重新执行 POP3 Connect + Login，操作完成后执行 Quit。
```

评价：界面功能覆盖实验要求，适合截图展示。当前已发送历史只显示本次运行内存记录，数据库历史查询可作为扩展说明。

### 成员五：SQLite 数据库和本地存储模块

检查范围：

```text
Data/DatabaseSchema.cs
Data/SqliteDatabaseManager.cs
Contracts/IDatabaseManager.cs
docs/DATABASE_MODULE_MEMBER5.md
```

检查结论：

```text
1. 已引入 Microsoft.Data.Sqlite。
2. 已实现数据库初始化和四张表创建：account_config、mail_summary、sent_mail、operation_log。
3. 已实现账号配置保存/加载、邮件摘要保存、已发送记录保存、操作日志保存。
4. SQL 写入使用参数化命令。
5. 账号配置表不保存 password 或 auth_code 字段，符合敏感信息不明文保存要求。
```

评价：数据库基础读写功能完整，可支撑实验报告中的数据库设计和运行结果截图。

## 成员六完成内容

### 邮件构造

负责文件：

```text
Services/MailParser.cs
Contracts/IMailParser.cs
```

已完成内容：

```text
1. BuildSmtpContent 将 MailMessageModel 构造成 SMTP DATA 内容。
2. 生成 From、To、Subject、Date、MIME-Version、Content-Type、Content-Transfer-Encoding 等邮件头。
3. Subject 含中文或其他非 ASCII 字符时，使用 RFC 2047 Base64 编码。
4. 正文统一按 UTF-8 Base64 编码，每 76 个字符换行。
5. 构造邮件头时过滤 CR/LF，避免用户输入造成邮件头注入。
```

### 邮件解析

已完成内容：

```text
1. Parse 将 POP3 RETR 得到的原始邮件内容解析为 MailMessageModel。
2. 支持 From、To、Subject、Date 头部字段解析。
3. 支持折叠头部展开。
4. 支持 RFC 2047 编码词解析，包括 B(Base64) 和 Q(Quoted-Printable)。
5. 支持 utf-8、gbk、gb2312、gb18030 等常见字符集。
6. 支持正文 Content-Transfer-Encoding：base64、quoted-printable、7bit、8bit。
7. 支持 multipart/alternative、multipart/mixed 中 text/plain 正文提取。
8. 无 text/plain 时可提取 text/html 并去除 HTML 标签。
```

### 日志模块

负责文件：

```text
Services/LogManager.cs
Contracts/ILogManager.cs
Models/OperationLogEntry.cs
```

已完成内容：

```text
1. 统一记录 Info、Send、Receive、Success、Error 五类日志。
2. 通过 LogAdded 事件通知 UI 实时显示日志。
3. 支持 GetAll 获取全部日志。
4. 支持 Clear 清空日志。
5. 在日志入口增加兜底脱敏：PASS 命令和密码/授权码相关内容不会直接保存。
```

## 功能测试用例

| 编号 | 测试目标 | 操作步骤 | 预期结果 |
| --- | --- | --- | --- |
| T01 | SMTP 邮件内容构造 | 输入中文主题和正文，点击发送 | DATA 内容含 MIME 头，Subject 编码，正文为 Base64 |
| T02 | SMTP 敏感日志脱敏 | 执行发送邮件 | 日志中出现 AUTH LOGIN、`<base64 username>`、`<base64 auth code>`，不出现授权码明文 |
| T03 | POP3 登录脱敏 | 点击测试 POP3 或刷新收件箱 | 日志中 PASS 显示为 `PASS <auth code>` |
| T04 | POP3 收件列表 | 点击刷新收件箱 | 显示邮件编号和大小，日志中出现 STAT、LIST |
| T05 | POP3 阅读邮件 | 选择邮件后点击阅读 | 自动连接并登录 POP3，执行 RETR，详情区显示发件人、收件人、主题、日期、正文 |
| T06 | POP3 删除邮件 | 选择邮件后点击删除并确认 | 自动连接并登录 POP3，执行 DELE 和 QUIT |
| T07 | Base64 正文解析 | 准备 Content-Transfer-Encoding 为 base64 的邮件 | 正文被解析为可读文本 |
| T08 | Quoted-Printable 正文解析 | 准备 quoted-printable 编码邮件 | 中文和软换行能正常还原 |
| T09 | Multipart 邮件解析 | 准备 multipart/alternative 邮件 | 优先显示 text/plain；无 text/plain 时显示去标签后的 HTML 文本 |
| T10 | 数据库日志保存 | 执行 SMTP/POP3 操作后查看 operation_log 表 | 操作日志被保存，敏感信息已脱敏 |

## 异常测试用例

| 编号 | 测试目标 | 操作步骤 | 预期结果 |
| --- | --- | --- | --- |
| E01 | SMTP 认证失败 | 输入错误授权码并发送 | 发送失败，状态栏和日志显示认证失败原因 |
| E02 | POP3 认证失败 | 输入错误授权码并测试 POP3 | 登录失败，日志显示错误响应，不泄露授权码 |
| E03 | 邮件头缺失 | 解析缺少 Subject 或 Date 的邮件 | 缺失字段为空或使用当前时间，不导致程序崩溃 |
| E04 | 编码无法识别 | 解析未知 charset 邮件 | 自动回退 UTF-8，保留可读内容或原文 |
| E05 | 多行响应异常结束 | POP3 多行响应未以 "." 结束 | 抛出异常并由 UI 显示操作失败 |

## 实验报告可用文字

### 测试方案

本系统采用功能测试和异常测试相结合的方式进行验证。功能测试重点验证 SMTP 发送流程、POP3 收件流程、邮件解析、日志记录和 SQLite 持久化是否符合实验要求；异常测试重点验证认证失败、服务器返回错误、编码异常和网络连接异常时程序是否能够给出明确提示，并保持界面和日志状态一致。

### 运行结果分析

测试结果表明，系统能够通过 Socket 建立 TCP 连接并完成 SMTP、POP3 协议的核心命令交互。SMTP 模块可以完成普通文本邮件发送，POP3 模块可以完成邮件列表获取、邮件读取和删除标记。邮件解析模块能够处理 UTF-8、GBK 等常见编码以及 Base64、Quoted-Printable 等正文编码，日志模块能够记录协议交互过程并对授权码进行脱敏。SQLite 模块能够保存账号配置、邮件摘要、发送记录和操作日志，满足实验对数据库集成的要求。

### 当前限制

```text
1. 当前 SocketConnection 使用明文 TCP，暂未实现 SSL/TLS 或 STARTTLS。
2. SMTP 当前主要支持单收件人普通文本邮件，不支持附件、抄送、密送。
3. POP3 收件列表的发件人、主题等详细字段需要 RETR 后才能解析。
4. 数据库已保存历史发送记录和日志，但 UI 暂未提供历史查询页面。
5. 邮件解析以 text/plain 为主，对复杂附件和富文本排版只做基础处理。
```

这些限制属于扩展功能，不影响本实验对“邮件编写、发送、接收、阅读、删除”和 Socket 协议实现过程的基本要求。

## 编译验证

已执行：

```powershell
$env:NUGET_SCRATCH = Join-Path (Get-Location) ".nuget-scratch"
dotnet build MailClient.csproj --configfile NuGet.Config
```

验证结果：

```text
0 个警告
0 个错误
```
