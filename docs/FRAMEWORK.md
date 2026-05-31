# 邮件客户端系统框架说明

## 技术栈

- C# / .NET 8
- WinForms
- Socket/TcpClient
- SQLite，当前只保留数据库接口和建表脚本，具体驱动接入后实现

## 目录约定

```text
Contracts/   统一接口定义，组员协作时优先看这里
Data/        SQLite 数据库表结构和数据库访问实现
Models/      账号、邮件、日志、协议结果等数据模型
Networking/  底层 Socket 通信封装
Protocols/   SMTP 和 POP3 协议客户端
Services/    邮件解析、日志等通用服务
UI/          WinForms 界面
```

## 核心接口

### ISmtpClientSocket

负责 SMTP 邮件发送。

```text
ConnectAsync()
SendMailAsync()
QuitAsync()
```

后续 SMTP 模块成员只需要实现该接口内部的协议命令流程，不应直接改 UI。

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

后续 POP3 模块成员只需要实现该接口内部的协议命令流程，不应直接改 UI。

### IMailParser

负责邮件内容构造和解析。

```text
BuildSmtpContent()
Parse()
```

SMTP 发送邮件时调用 `BuildSmtpContent`，POP3 阅读邮件时调用 `Parse`。

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

数据库表结构统一放在 `Data/DatabaseSchema.cs`。

### ILogManager

负责日志输出。

```text
Info()
Send()
Receive()
Success()
Error()
```

协议模块必须通过日志接口输出交互过程，不能直接操作界面控件。

## 当前实现状态

- 已创建可运行 WinForms 主窗口。
- 已建立账号配置区和四个标签页：写邮件、收件箱、已发送、日志。
- 已定义 SMTP、POP3、数据库、日志、邮件解析等协作接口。
- 已提供 `SocketConnection` 底层收发封装。
- SMTP/POP3/SQLite 目前是占位实现，等待后续按接口补全。

## 协作规则

1. 新功能先判断属于哪个接口，不要跨层直接调用。
2. UI 只调用 `Contracts` 中的接口，不直接写 Socket 命令。
3. 协议模块只负责 SMTP/POP3 命令，不负责界面展示。
4. 数据库存储只通过 `IDatabaseManager`。
5. 日志统一通过 `ILogManager`，密码和授权码必须脱敏。
