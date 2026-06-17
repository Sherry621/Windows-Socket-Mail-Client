# 六人小组后续开发分工

本文档用于统一后续 6 人小组协作边界。项目代码以仓库根目录为准，文档统一放在 `docs/` 目录下。

## 总体协作原则

1. UI 层只调用 `Contracts/` 中定义的接口，不直接操作 Socket。
2. SMTP、POP3 协议实现只放在 `Protocols/` 和 `Networking/`。
3. SQLite 读写只通过 `IDatabaseManager`。
4. 邮件内容构造和解析统一通过 `IMailParser`。
5. 协议日志统一通过 `ILogManager`，不得输出明文密码或授权码。
6. 每个成员提交代码时，应在提交说明中写清楚修改的模块和完成的功能。

## 成员一：项目负责人 / 总体架构 / 集成协调

### 主要职责

- 维护系统整体架构和模块边界。
- 负责 `Contracts/` 接口变更审核。
- 负责主程序入口、依赖组装和整体运行流程。
- 协调 SMTP、POP3、UI、数据库、测试之间的接口对接。
- 负责最终集成、版本管理和报告总体格式检查。

### 负责目录或文件

```text
Program.cs
Contracts/
FRAMEWORK.md
docs/
```

### 交付物

- 接口定义说明。
- 集成后的可运行版本。
- 项目整体结构说明。
- 实验报告中“系统总体设计”和“模块划分”部分。

## 成员二：SMTP 邮件发送模块

### 主要职责

- 实现 SMTP Socket 连接流程。
- 实现服务器响应读取和响应码判断。
- 实现 `EHLO/HELO`、`AUTH LOGIN`、`MAIL FROM`、`RCPT TO`、`DATA`、`QUIT` 命令。
- 实现用户名和授权码 Base64 编码。
- 处理 SMTP 发送异常。
- 将协议交互过程写入日志。

### 负责目录或文件

```text
Protocols/SmtpClientSocket.cs
Networking/SocketConnection.cs
Contracts/ISmtpClientSocket.cs
```

### 交付物

- 可发送普通文本邮件的 SMTP 模块。
- SMTP 协议交互日志。
- SMTP 成功和失败测试截图。
- 实验报告中“SMTP 协议实现”部分。

## 成员三：POP3 收件、阅读和删除模块

### 主要职责

- 实现 POP3 Socket 连接流程。
- 实现 `USER`、`PASS` 登录。
- 实现 `STAT` 获取邮件数量和总大小。
- 实现 `LIST` 获取邮件列表。
- 实现 `RETR` 读取邮件原始内容。
- 实现 `DELE` 删除邮件。
- 正确处理 POP3 多行响应，以单独一行 `.` 为结束标记。
- 将协议交互过程写入日志。

### 负责目录或文件

```text
Protocols/Pop3ClientSocket.cs
Networking/SocketConnection.cs
Contracts/IPop3ClientSocket.cs
```

### 交付物

- 可获取邮件列表、阅读邮件、删除邮件的 POP3 模块。
- POP3 协议交互日志。
- 收件、阅读、删除测试截图。
- 实验报告中“POP3 协议实现”部分。

## 成员四：WinForms 图形界面模块

### 主要职责

- 完善主窗口布局。
- 实现账号配置区。
- 实现写邮件页面。
- 实现收件箱页面。
- 实现邮件详情阅读页面。
- 实现已发送记录页面。
- 实现日志页面。
- 绑定按钮事件，但业务逻辑必须通过 `Contracts/` 接口调用。

### 负责目录或文件

```text
UI/MainForm.cs
UI/
```

### 交付物

- 完整 WinForms 操作界面。
- 账号配置、写邮件、收件箱、已发送、日志等页面截图。
- 实验报告中“系统界面设计”部分。

## 成员五：SQLite 数据库和本地存储模块

### 主要职责

- 接入 SQLite 驱动。
- 初始化数据库文件。
- 创建账号配置表、邮件摘要表、已发送邮件表、操作日志表。
- 实现账号配置保存和加载。
- 实现邮件摘要保存。
- 实现已发送记录保存。
- 实现操作日志保存。
- 注意密码和授权码不得明文保存，若确需保存，应注明实验环境限制或使用加密方式。

### 负责目录或文件

```text
Data/DatabaseSchema.cs
Data/SqliteDatabaseManager.cs
Contracts/IDatabaseManager.cs
```

### 交付物

- 可正常初始化和读写的 SQLite 数据库模块。
- 数据库表结构说明。
- 本地记录保存和加载测试截图。
- 实验报告中“数据库设计”部分。

## 成员六：邮件解析、日志、测试与报告材料整理

### 主要职责

- 完善 `MailParser` 邮件构造和解析逻辑。
- 解析邮件头部字段：`From`、`To`、`Subject`、`Date`。
- 解析邮件正文，优先支持普通文本。
- 处理 UTF-8、GBK 等常见编码的基本兼容。
- 完善 `LogManager` 日志格式。
- 编写功能测试和异常测试用例。
- 整理运行截图、测试结果和报告材料。

### 负责目录或文件

```text
Services/MailParser.cs
Services/LogManager.cs
Contracts/IMailParser.cs
Contracts/ILogManager.cs
docs/
```

### 交付物

- 邮件解析模块。
- 日志模块。
- 测试用例表。
- 测试结果截图。
- 实验报告中“测试方案”和“运行结果分析”部分。

## 推荐开发顺序

### 第一阶段：接口确认和环境统一

- 成员一确认接口边界。
- 全体成员拉取最新代码并成功编译。
- 成员四确认 UI 页面结构。
- 成员五确认 SQLite 包接入方式。

### 第二阶段：协议模块优先实现

- 成员二完成 SMTP 基础发送。
- 成员三完成 POP3 登录、列表、读取和删除。
- 成员六完善日志格式，保证协议过程可截图。

### 第三阶段：界面和业务串联

- 成员四将按钮事件接入 SMTP、POP3、数据库和日志接口。
- 成员五完成账号配置、邮件摘要、发送记录保存。
- 成员一负责整体集成。

### 第四阶段：测试和报告整理

- 成员六组织测试用例。
- 成员二、三提供协议测试截图。
- 成员四提供界面运行截图。
- 成员五提供数据库记录截图。
- 成员一整合报告，确认每位成员源码贡献说明。

## 建议提交粒度

```text
feat: implement smtp auth login
feat: implement pop3 list and retrieve
feat: add account config ui
feat: implement sqlite account storage
test: add smtp and pop3 test cases
docs: add module design notes
```

## 报告中源码归属写法

```text
成员一：Program.cs、Contracts/*、系统总体集成代码
成员二：Protocols/SmtpClientSocket.cs、SMTP 发送流程代码
成员三：Protocols/Pop3ClientSocket.cs、POP3 收件/阅读/删除代码
成员四：UI/MainForm.cs、WinForms 界面代码
成员五：Data/DatabaseSchema.cs、Data/SqliteDatabaseManager.cs、数据库读写代码
成员六：Services/MailParser.cs、Services/LogManager.cs、测试用例和测试结果文档
```
