# 基于 Socket 的 Windows 邮件客户端

本项目是 2026 年计算机网络实践课程项目，选题为“邮件系统”。系统使用 C# + WinForms + Socket + SQLite 设计并实现一个运行在 Windows 平台上的图形化邮件客户端，目标是通过 Socket 编程从 TCP 连接开始实现 SMTP 和 POP3 协议的核心流程。

当前仓库已经完成项目系统框架，用于统一后续小组协作中的目录结构、模块边界和接口约定。SMTP、POP3、SQLite 等具体功能会在该框架基础上继续补全。

## 项目目标

- 使用 WinForms 实现 Windows 图形化客户端界面。
- 使用 Socket/TcpClient 实现 SMTP 邮件发送。
- 使用 Socket/TcpClient 实现 POP3 邮件接收、阅读和删除。
- 支持账号配置、邮件编写、收件箱、已发送记录和协议日志。
- 使用 SQLite 保存账号配置、邮件摘要、发送记录和操作日志。
- 在日志中展示 SMTP/POP3 协议交互过程，便于课程报告截图和分析。

## 技术栈

```text
开发语言：C#
运行框架：.NET 8
界面框架：WinForms
网络通信：Socket / TcpClient
数据库：SQLite
运行平台：Windows 10 / Windows 11
开发工具：Visual Studio 2022 或 dotnet CLI
```

## 仓库结构

```text
.
├── READ.md                 # 课程设计文档
├── README.md               # GitHub 项目说明
└── MailClient/
    ├── Contracts/          # 统一接口定义
    ├── Data/               # SQLite 数据库表结构和数据库访问实现
    ├── Models/             # 账号、邮件、日志、协议结果等数据模型
    ├── Networking/         # 底层 Socket 通信封装
    ├── Protocols/          # SMTP 和 POP3 协议客户端
    ├── Services/           # 邮件解析、日志等通用服务
    ├── UI/                 # WinForms 界面
    ├── FRAMEWORK.md        # 系统框架和协作说明
    ├── MailClient.csproj   # C# 项目文件
    └── NuGet.Config        # 本地 NuGet 配置
```

## 核心模块

### UI 界面层

位于 `MailClient/UI`，负责 WinForms 主界面和用户交互。界面层只调用 `Contracts` 中定义的接口，不直接编写 SMTP/POP3 协议命令。

当前主界面包含：

- 账号配置区
- 写邮件标签页
- 收件箱标签页
- 已发送标签页
- 日志标签页

### 协议层

位于 `MailClient/Protocols`，负责 SMTP 和 POP3 协议命令流程。

- `SmtpClientSocket`：SMTP 邮件发送客户端。
- `Pop3ClientSocket`：POP3 邮件接收、阅读、删除客户端。

### 网络通信层

位于 `MailClient/Networking`。

- `SocketConnection`：封装 TCP 连接、发送一行命令、读取单行响应、读取多行响应等基础操作。

### 数据库层

位于 `MailClient/Data`。

- `DatabaseSchema`：统一维护 SQLite 建表脚本。
- `SqliteDatabaseManager`：数据库访问实现，目前为占位实现，后续接入 SQLite 驱动后补全。

### 服务层

位于 `MailClient/Services`。

- `MailParser`：负责构造 SMTP 邮件内容、解析 POP3 原始邮件内容。
- `LogManager`：负责统一日志记录和界面通知。

## 主要接口

接口统一放在 `MailClient/Contracts`，后续开发应优先遵守这些接口。

```text
ISmtpClientSocket     SMTP 邮件发送接口
IPop3ClientSocket     POP3 邮件接收、阅读、删除接口
IDatabaseManager      SQLite 数据库存储接口
ILogManager           日志接口
IMailParser           邮件构造和解析接口
```

## 编译运行

进入仓库根目录：

```powershell
cd "C:\Users\Sherry Peng\OneDrive\桌面\Computer-Network Practice"
```

还原项目：

```powershell
$env:NUGET_SCRATCH = Join-Path (Get-Location) ".nuget-scratch"
dotnet restore MailClient\MailClient.csproj --configfile MailClient\NuGet.Config
```

编译项目：

```powershell
$env:NUGET_SCRATCH = Join-Path (Get-Location) ".nuget-scratch"
dotnet build MailClient\MailClient.csproj --configfile MailClient\NuGet.Config
```

运行项目：

```powershell
dotnet run --project MailClient\MailClient.csproj --configfile MailClient\NuGet.Config
```

生成文件位置：

```text
MailClient\bin\Debug\net8.0-windows\
```

说明：当前命令中设置 `NUGET_SCRATCH` 是为了避免部分 Windows 环境下 NuGet 临时锁文件导致 restore/build 失败。

## 当前进度

- 已创建 .NET 8 WinForms 项目。
- 已完成系统目录结构。
- 已完成账号、邮件、日志等基础模型。
- 已完成 SMTP、POP3、数据库、日志、邮件解析接口定义。
- 已完成 Socket 通信基础封装。
- 已完成 WinForms 主窗口框架。
- 已通过 `dotnet build` 编译检查。

尚未完成：

- SMTP 完整命令流程。
- POP3 完整命令流程。
- SQLite 驱动接入和真实读写。
- 邮件解析的复杂编码、HTML、附件处理。
- 完整界面交互和测试用例。

## 协作约定

1. UI 层只调用 `Contracts` 接口，不直接操作 Socket。
2. SMTP/POP3 协议代码只放在 `Protocols` 和 `Networking`。
3. 数据库相关代码只通过 `IDatabaseManager` 访问。
4. 邮件内容构造和解析统一走 `IMailParser`。
5. 协议交互日志统一走 `ILogManager`。
6. 日志中不得输出明文密码或授权码。
7. 新增功能前先确认属于哪个模块，避免跨层耦合。


## 注意事项

本项目用于计算机网络课程实践，重点是展示 SMTP/POP3 应用层协议与 TCP Socket 通信过程。真实公网邮箱通常要求 SSL/TLS、授权码、安全策略和反垃圾限制，后续如需连接真实邮箱服务器，需要补充安全连接和认证兼容处理。
