# 邮件客户端系统框架说明

## 技术栈

- C# / .NET 8
- WinForms
- Socket / TcpClient
- SQLite

## 目录约定

```text
Contracts/   统一接口定义
Data/        SQLite 表结构和数据库访问实现
Models/      账号、邮件、日志、协议结果等数据模型
Networking/  底层 Socket 通信封装
Protocols/   SMTP 和 POP3 协议客户端
Services/    邮件解析、日志等通用服务
UI/          WinForms 界面
docs/        项目说明、分工和测试材料
```

## 核心接口

### ISmtpClientSocket

负责 SMTP 邮件发送。

```text
ConnectAsync()
SendMailAsync()
QuitAsync()
```

SMTP 模块成员只实现接口内部的协议命令流程，不直接操作 UI。

### IPop3ClientSocket

负责 POP3 邮件接收、阅读和删除。

```text
ConnectAsync()
LoginAsync()
GetStatAsync()
ListMailsAsync()
RetrieveMailAsync()
DeleteMailAsync()
QuitAsync()
```

POP3 模块成员只实现接口内部的协议命令流程，不直接操作 UI。

### IMailParser

负责邮件内容构造和解析。

```text
BuildSmtpContent()
Parse()
```

SMTP 发送邮件时调用 `BuildSmtpContent`，POP3 读取邮件时调用 `Parse`。

### IDatabaseManager

负责 SQLite 持久化。

```text
InitializeAsync()
SaveAccountConfigAsync()
LoadAccountConfigAsync()
SaveMailSummaryAsync()
SaveSentMailAsync()
SaveOperationLogAsync()
```

### ILogManager

负责日志输出。

```text
Info()
Send()
Receive()
Success()
Error()
```

协议模块必须通过日志接口输出交互过程，不直接操作界面控件。

## 当前实现状态

- 已创建可运行的 WinForms 主窗口。
- 已建立账号配置区和四个标签页：写邮件、收件箱、已发送、日志。
- 已定义 SMTP、POP3、数据库、日志、邮件解析等协作接口。
- 已提供 `SocketConnection` 底层收发封装。
- SMTP 已完成基础发送实现，包含连接、响应码判断、`EHLO/HELO`、`AUTH LOGIN`、`MAIL FROM`、`RCPT TO`、`DATA`、`QUIT`、异常处理和协议日志。
- POP3 当前仍为占位实现，等待后续补全登录、列表、读取和删除流程。
- SQLite 当前仍为占位实现，等待后续补全真实读写能力。

## 最新补充文档

```text
docs/SMTP_MODULE_MEMBER2.md
docs/SMTP_TEST_CHECKLIST.md
docs/UI_MODULE_MEMBER4.md
docs/TEAM_DIVISION_6.md
```

## 协作规则

1. 新功能先判断属于哪个接口，不要跨层直接调用。
2. UI 只调用 `Contracts/` 中定义的接口，不直接发送 Socket 命令。
3. 协议模块只负责 SMTP/POP3 命令流程，不负责界面展示。
4. 数据库存储只通过 `IDatabaseManager` 访问。
5. 日志统一通过 `ILogManager` 输出，密码和授权码必须脱敏。
