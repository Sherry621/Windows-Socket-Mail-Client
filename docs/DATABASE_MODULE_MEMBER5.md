# 成员五 SQLite 数据库和本地存储模块完成说明

本文档记录成员五负责的 SQLite 数据库和本地存储模块当前实现情况，便于后续联调、测试截图整理和实验报告撰写。

## 负责范围

成员五负责以下数据库相关文件：

```text
Data/DatabaseSchema.cs
Data/SqliteDatabaseManager.cs
Contracts/IDatabaseManager.cs
MailClient.csproj
NuGet.Config
```

其中：

```text
Data/DatabaseSchema.cs        维护 SQLite 建表语句
Data/SqliteDatabaseManager.cs 实现 SQLite 初始化和读写
Contracts/IDatabaseManager.cs 定义数据库访问接口
MailClient.csproj             引入 SQLite 驱动包
NuGet.Config                  配置 NuGet 包源
```

本模块只负责数据库文件初始化和本地数据持久化，不直接处理 SMTP、POP3 协议命令，也不直接控制 WinForms 界面显示。

## 已完成内容

### SQLite 驱动接入

当前项目已通过 NuGet 引入 SQLite 驱动：

```xml
<PackageReference Include="Microsoft.Data.Sqlite" Version="8.0.6" />
```

同时在 `NuGet.Config` 中配置了官方 NuGet 包源：

```xml
<add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
```

因此项目可以在还原依赖后直接使用 `Microsoft.Data.Sqlite` 访问本地 SQLite 数据库。

### 数据库文件初始化

`SqliteDatabaseManager` 在构造时接收数据库路径：

```csharp
databaseManager = new SqliteDatabaseManager("mail_client.db");
```

程序启动时，UI 层会调用：

```text
IDatabaseManager.InitializeAsync()
```

当前初始化流程包括：

```text
1. 获取数据库文件完整路径
2. 自动创建数据库所在目录
3. 打开 SQLite 连接
4. 执行四张表的 CREATE TABLE IF NOT EXISTS 语句
5. 若数据库文件不存在，则自动创建 mail_client.db
```

因此程序首次运行时会自动生成本地数据库文件，不需要用户手动创建数据库。

### 数据库表创建

当前已按照实验要求创建四张表：

```text
account_config  账号配置表
mail_summary    邮件摘要表
sent_mail       已发送邮件表
operation_log   操作日志表
```

建表 SQL 统一放在 `DatabaseSchema` 中维护，便于后续修改字段或补充索引。

## 数据库表结构

### 账号配置表 account_config

用于保存用户邮箱账号和服务器配置。

| 字段名 | 类型 | 说明 |
| --- | --- | --- |
| id | INTEGER | 主键，自增 |
| email | TEXT | 邮箱地址 |
| smtp_server | TEXT | SMTP 服务器地址 |
| smtp_port | INTEGER | SMTP 端口 |
| pop3_server | TEXT | POP3 服务器地址 |
| pop3_port | INTEGER | POP3 端口 |
| remember_account | INTEGER | 是否记住账号，1 表示记住 |
| create_time | TEXT | 创建时间 |
| update_time | TEXT | 更新时间 |

安全说明：

```text
当前表中不保存 password 或 auth_code 字段。
账号加载时 PasswordOrAuthCode 会返回空字符串。
用户每次重新打开程序后需要重新输入授权码。
```

这样可以满足实验要求中“密码和授权码不得明文保存”的安全约定。

### 邮件摘要表 mail_summary

用于保存 POP3 获取到的邮件摘要信息。

| 字段名 | 类型 | 说明 |
| --- | --- | --- |
| id | INTEGER | 主键，自增 |
| mail_no | INTEGER | POP3 邮件编号 |
| sender | TEXT | 发件人 |
| receiver | TEXT | 收件人 |
| subject | TEXT | 邮件主题 |
| mail_date | TEXT | 邮件日期 |
| size | INTEGER | 邮件大小 |
| status | TEXT | 邮件状态 |
| receive_time | TEXT | 本地接收或保存时间 |

当前状态值由 `MailStatus` 枚举转换而来：

```text
unread
read
deleted
```

### 已发送邮件表 sent_mail

用于保存 SMTP 发送记录。

| 字段名 | 类型 | 说明 |
| --- | --- | --- |
| id | INTEGER | 主键，自增 |
| sender | TEXT | 发件人 |
| receiver | TEXT | 收件人 |
| subject | TEXT | 邮件主题 |
| body | TEXT | 邮件正文 |
| send_status | TEXT | 发送状态 |
| send_time | TEXT | 发送时间 |
| error_message | TEXT | 失败原因 |

发送状态取值：

```text
success
failed
```

当发送成功时，`error_message` 保存为空字符串；当发送失败时，保存 SMTP 模块返回的失败原因。

### 操作日志表 operation_log

用于保存系统、SMTP、POP3、UI 等模块产生的操作日志。

| 字段名 | 类型 | 说明 |
| --- | --- | --- |
| id | INTEGER | 主键，自增 |
| protocol | TEXT | 模块或协议名称 |
| operation | TEXT | 操作类型 |
| content | TEXT | 日志内容 |
| level | TEXT | 日志级别 |
| create_time | TEXT | 日志时间 |

日志级别由 `LogLevel` 枚举转换而来：

```text
INFO
SEND
RECEIVE
SUCCESS
ERROR
```

## 已实现接口

当前 `SqliteDatabaseManager` 已完整实现 `IDatabaseManager` 中的方法。

### InitializeAsync

功能：

```text
初始化 SQLite 数据库文件
创建账号配置表
创建邮件摘要表
创建已发送邮件表
创建操作日志表
```

该方法使用 `CREATE TABLE IF NOT EXISTS`，可以重复调用，不会重复建表或清空已有数据。

### SaveAccountConfigAsync

功能：

```text
保存当前账号配置
```

当前实现采用“先删除旧配置，再插入新配置”的方式，保证本地只保留一份最新账号配置。保存内容包括：

```text
邮箱地址
SMTP 服务器
SMTP 端口
POP3 服务器
POP3 端口
是否记住账号
创建时间
更新时间
```

不会保存密码或授权码。

### LoadAccountConfigAsync

功能：

```text
读取最近一次保存的账号配置
```

程序启动后，UI 层调用该方法自动加载：

```text
邮箱地址
SMTP 服务器
SMTP 端口
POP3 服务器
POP3 端口
```

由于本模块不保存授权码，加载后的 `PasswordOrAuthCode` 为空，需要用户手动输入。

### SaveMailSummaryAsync

功能：

```text
保存 POP3 邮件摘要记录
```

当前 UI 在刷新收件箱并获取邮件列表后，会遍历邮件摘要并调用该方法保存到 `mail_summary` 表。

保存内容包括：

```text
邮件编号
发件人
收件人
主题
邮件日期
邮件大小
邮件状态
本地保存时间
```

### SaveSentMailAsync

功能：

```text
保存 SMTP 已发送邮件记录
```

当前 UI 在发送邮件成功或失败后都会调用该方法。因此数据库中既可以看到成功发送记录，也可以看到失败发送记录，便于实验报告截图说明异常处理。

保存内容包括：

```text
发件人
收件人
主题
正文
发送状态
发送时间
失败原因
```

### SaveOperationLogAsync

功能：

```text
保存操作日志
```

当前 UI 监听 `ILogManager.LogAdded` 事件。每当系统、SMTP、POP3 或 UI 模块产生日志时，界面会显示日志，同时异步调用数据库接口保存到 `operation_log` 表。

为了避免数据库写入失败影响协议流程，日志落库失败时不会阻断界面显示或邮件操作。

## 关键实现说明

### 参数化 SQL

当前所有插入操作均使用 SQLite 参数化命令，例如：

```text
$email
$smtp_server
$smtp_port
```

这样可以避免手动拼接 SQL 字符串，提高稳定性，也能正确处理中文、空格和特殊字符。

### 时间格式

数据库中的时间字段使用：

```text
DateTimeOffset.ToString("O")
```

该格式包含日期、时间和时区信息，便于后续排序、调试和报告截图。

### 数据库连接管理

每次数据库操作都会：

```text
1. 创建 SQLite 连接
2. 打开连接
3. 执行 SQL
4. 自动释放连接和命令对象
```

代码中使用 `await using` 释放数据库连接，避免文件句柄长期占用。

### 账号配置事务

保存账号配置时使用事务执行：

```text
DELETE FROM account_config
INSERT INTO account_config
COMMIT
```

这样可以保证“删除旧配置”和“写入新配置”作为一个整体完成，避免中间失败导致配置状态不一致。

## 与其他模块的联调关系

### 与 UI 模块

UI 层只通过 `IDatabaseManager` 调用数据库功能：

```text
保存配置        -> SaveAccountConfigAsync
启动加载配置    -> LoadAccountConfigAsync
发送后保存记录  -> SaveSentMailAsync
刷新收件箱后保存 -> SaveMailSummaryAsync
新增日志后保存  -> SaveOperationLogAsync
```

UI 层没有直接编写 SQL，也没有直接创建 SQLite 连接。

### 与 SMTP 模块

SMTP 模块负责发送邮件并返回 `SmtpSendResult`。

数据库模块保存发送结果：

```text
Success = true  -> send_status = success
Success = false -> send_status = failed
Message         -> error_message
```

SMTP 模块本身不直接操作 SQLite。

### 与 POP3 模块

POP3 模块负责获取邮件列表并返回 `MailSummary` 集合。

数据库模块保存邮件摘要：

```text
MailNo
Sender
Receiver
Subject
MailDate
Size
Status
```

POP3 模块本身不直接操作 SQLite。

### 与日志模块

日志模块通过 `ILogManager.LogAdded` 事件输出日志。

数据库模块保存日志内容：

```text
Protocol
Operation
Content
Level
CreateTime
```

日志中仍需遵守项目约定，不得输出明文密码或授权码。

## 安全性说明

根据实验要求：

```text
密码和授权码不得明文保存。
```

当前实现采用“不保存授权码”的方式处理敏感信息：

```text
1. account_config 表没有 password 或 auth_code 字段
2. SaveAccountConfigAsync 不写入 PasswordOrAuthCode
3. LoadAccountConfigAsync 返回的 PasswordOrAuthCode 为空
4. 用户重新打开程序后需要重新输入授权码
```

该方案牺牲了一点便利性，但可以避免本地数据库泄露授权码，适合课程实验场景。

如果后续确需记住授权码，应增加加密存储方案，并在实验报告中说明加密方式和实验环境限制。

## 当前数据库文件

默认数据库文件名为：

```text
mail_client.db
```

在当前程序中，该文件位于程序运行目录。使用 Visual Studio 或 `dotnet run` 启动时，通常会生成在输出目录附近，例如：

```text
bin/Debug/net8.0-windows/mail_client.db
```

也可能根据启动工作目录生成在项目根目录。截图时可以先运行程序保存配置或发送邮件，再查找 `mail_client.db` 文件。

## 编译验证

已执行以下命令验证：

```powershell
$env:NUGET_SCRATCH = Join-Path (Get-Location) ".nuget-scratch"
dotnet build MailClient.csproj --configfile NuGet.Config
```

验证结果：

```text
0 个警告
0 个错误
```

说明当前 SQLite 驱动引用、数据库实现和 UI 调用均已通过编译检查。

## 建议测试截图

成员五后续可准备以下截图作为交付材料：

```text
1. 程序启动后自动生成 mail_client.db 文件
2. 点击“保存配置”后，account_config 表出现账号和服务器配置
3. 发送邮件后，sent_mail 表出现发送记录
4. 刷新收件箱后，mail_summary 表出现邮件摘要
5. 进行 SMTP/POP3/UI 操作后，operation_log 表出现日志记录
6. account_config 表中没有明文授权码字段
```

推荐使用 DB Browser for SQLite 或 Visual Studio 数据库查看工具打开 `mail_client.db` 截图。

## 当前仍未完成的部分

按成员五完整交付物要求，当前代码功能已经完成基础数据库读写，但仍需要补充以下非代码材料：

```text
1. 数据库表记录截图
2. 本地记录保存和加载测试截图
3. 实验报告中“数据库设计”部分成稿
```

另外，当前 `IDatabaseManager` 接口只定义了保存方法，没有定义查询历史已发送记录、查询历史日志、查询邮件摘要列表的方法。因此：

```text
已发送页当前仍主要显示本次运行期间的发送记录。
历史发送记录已经保存到数据库，但 UI 暂未从数据库重新加载。
```

如后续需要完整展示历史记录，可由项目负责人审核接口变更后，为 `IDatabaseManager` 增加查询方法。

## 已知限制

当前数据库模块存在以下限制：

```text
1. 暂不支持多账号配置，只保留最近一次账号配置
2. 暂不支持从数据库加载历史已发送列表到 UI
3. 暂不支持从数据库加载历史日志到 UI
4. 邮件摘要保存时没有去重，多次刷新可能产生重复摘要记录
5. 暂不保存授权码，因此重启程序后需要重新输入授权码
```

这些限制不影响课程实验中对 SQLite 初始化、建表、保存账号配置、保存邮件摘要、保存发送记录和保存日志的展示。

## 可直接用于实验报告的内容提纲

实验报告“数据库设计”部分建议按以下结构整理：

```text
1. 数据库模块职责
2. SQLite 选型原因
3. 数据库文件初始化流程
4. account_config 表结构和安全说明
5. mail_summary 表结构
6. sent_mail 表结构
7. operation_log 表结构
8. IDatabaseManager 接口实现
9. 与 UI、SMTP、POP3、日志模块的协作关系
10. 敏感信息不明文保存策略
11. 测试截图和运行结果说明
12. 当前限制与后续扩展方向
```
