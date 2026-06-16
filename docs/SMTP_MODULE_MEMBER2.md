# 成员二 SMTP 模块完成说明

本文档记录成员二负责的 SMTP 邮件发送模块当前实现情况，便于后续联调、测试截图整理和实验报告撰写。

## 负责范围

成员二负责以下文件中的 SMTP 协议实现：

```text
Protocols/SmtpClientSocket.cs
Networking/SocketConnection.cs
Contracts/ISmtpClientSocket.cs
```

本模块只负责 SMTP 协议交互和发送流程控制，不直接操作 UI，也不直接处理数据库持久化。

## 已完成内容

### SMTP 连接与响应读取

当前已完成以下基础能力：

```text
1. 使用 TcpClient 建立 SMTP TCP 连接
2. 连接成功后读取服务器欢迎响应
3. 校验 220 响应码
4. 支持单行响应和多行响应读取
5. 对响应码进行统一解析和判断
```

其中多行响应按 SMTP 规范处理：

```text
250-xxxx
250-xxxx
250 xxxx
```

只有读到最后一行带空格前缀的状态码时，才认为该次响应结束。

### SMTP 命令流程

当前 `SendMailAsync` 已实现完整普通文本邮件发送流程：

```text
Connect
EHLO
HELO（EHLO 失败时回退）
AUTH LOGIN
Base64(username)
Base64(auth code)
MAIL FROM
RCPT TO
DATA
发送邮件头和正文
发送 "."
QUIT
```

其中：

1. `EHLO` 失败时会自动回退到 `HELO`
2. `AUTH LOGIN` 阶段分别处理用户名和授权码的 Base64 编码
3. `MAIL FROM`、`RCPT TO`、`DATA`、`QUIT` 都会校验服务端返回码
4. `DATA` 阶段会在正文结束后单独发送 `.` 作为结束标记

### 邮件内容发送

SMTP 模块通过 `IMailParser.BuildSmtpContent()` 获取邮件内容字符串，再逐行发送到服务器。

当前支持的邮件类型为：

```text
普通文本邮件
UTF-8 编码主题
UTF-8 编码正文
Base64 方式传输正文
```

为避免 DATA 段正文与 SMTP 结束符冲突，发送时已经处理了以 `.` 开头的行，自动进行 dot-stuffing。

### 日志记录

当前 SMTP 模块已经将以下交互过程写入 `ILogManager`：

```text
连接服务器
发送命令
接收服务器响应
认证成功
发送成功
发送失败
退出会话
```

日志遵守项目约定：

```text
不记录明文密码
不记录明文授权码
不直接暴露 Base64 后的敏感值
```

认证阶段日志只保留占位内容，例如：

```text
AUTH LOGIN
<base64 username>
<base64 auth code>
```

### 异常处理

当前已覆盖的 SMTP 异常处理包括：

```text
1. 连接失败
2. 服务器提前断开
3. 响应码不符合预期
4. AUTH LOGIN 认证失败
5. 收件人被服务器拒绝
6. DATA 阶段发送失败
7. 用户取消发送
```

`SendMailAsync` 在失败时会返回：

```text
Success = false
Message = 错误原因
```

这样 UI 层可以直接显示错误信息并保存发送记录。

## 主要实现方法

当前 SMTP 模块核心方法包括：

```text
ConnectAsync()
SendMailAsync()
QuitAsync()
SendGreetingAsync()
AuthenticateAsync()
SendEnvelopeAsync()
SendDataAsync()
ReadResponseAsync()
```

它们分别负责：

1. 建立连接并读取 220 响应
2. 组织完整发送流程
3. 结束会话并发送 QUIT
4. 完成 EHLO / HELO
5. 完成 AUTH LOGIN
6. 完成 MAIL FROM / RCPT TO
7. 完成 DATA 段发送
8. 读取并解析服务器响应

## 当前发送流程说明

SMTP 发送流程可概括为：

```text
开始
  ->
建立 TCP 连接
  ->
读取 220 欢迎响应
  ->
发送 EHLO
  ->
必要时回退 HELO
  ->
发送 AUTH LOGIN
  ->
发送 Base64 用户名
  ->
发送 Base64 授权码
  ->
发送 MAIL FROM
  ->
发送 RCPT TO
  ->
发送 DATA
  ->
发送邮件头和正文
  ->
发送 "."
  ->
读取 250 发送结果
  ->
发送 QUIT
  ->
结束
```

## 与其他模块的边界

SMTP 模块与其他成员代码之间的边界如下：

```text
UI 只调用 ISmtpClientSocket
数据库保存由 IDatabaseManager 负责
邮件内容构造由 IMailParser 负责
日志输出由 ILogManager 负责
```

因此成员二的代码不会直接：

```text
操作 WinForms 控件
写入 SQLite
解析 POP3 邮件
处理界面状态栏
```

## 编译验证

已执行以下命令：

```powershell
$env:NUGET_SCRATCH = Join-Path (Get-Location) ".nuget-scratch"
dotnet build MailClient.csproj --configfile NuGet.Config
```

当前验证结果：

```text
0 个警告
0 个错误
```

## 当前仍未完成的部分

按成员二完整交付物要求，当前还缺少以下非代码材料：

```text
1. SMTP 成功测试截图
2. SMTP 失败测试截图
3. 实验报告中“SMTP 协议实现”成稿
```

## 已知限制

当前 SMTP 模块仍有以下限制，这些不影响课程基础协议演示，但需要在报告中说明：

```text
1. 当前为明文 SMTP，不支持 SSL/TLS 或 STARTTLS
2. 当前仅支持单收件人普通文本邮件
3. 不支持附件发送
4. 不支持 HTML 邮件
5. 不支持抄送、密送和多收件人
```

## 可直接用于实验报告的内容提纲

实验报告“SMTP 协议实现”部分建议按以下结构整理：

```text
1. SMTP 模块职责
2. Socket 连接建立过程
3. 响应码读取与判断方法
4. EHLO/HELO 实现
5. AUTH LOGIN 与 Base64 编码
6. MAIL FROM / RCPT TO / DATA / QUIT 实现
7. 日志记录与敏感信息脱敏
8. 异常处理策略
9. 当前限制与后续扩展方向
```
