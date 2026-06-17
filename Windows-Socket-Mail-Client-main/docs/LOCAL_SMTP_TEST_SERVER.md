# 本地 SMTP 测试服务器使用说明

这份说明用于在没有真实服务器的情况下，独立完成 SMTP 模块测试和截图。

## 适用场景

如果你当前：

```text
1. 只有自己一个人开发
2. 没有课程提供的 SMTP 服务器
3. 当前客户端不支持 SSL/TLS 或 STARTTLS
4. 只需要完成 SMTP 成功和失败测试截图
```

就直接使用仓库内提供的本地 SMTP 测试服务器。

## 服务器位置

```text
Tools/LocalSmtpTestServer/
```

## 支持模式

本地服务器支持三种模式：

```text
success    成功发送
auth-fail  认证失败
rcpt-fail  收件人失败
```

## 运行命令

### 1. 成功模式

```powershell
cd D:\code\Windows-Socket-Mail-Client
dotnet run --project Tools\LocalSmtpTestServer\LocalSmtpTestServer.csproj -- --mode success
```

### 2. 认证失败模式

```powershell
cd D:\code\Windows-Socket-Mail-Client
dotnet run --project Tools\LocalSmtpTestServer\LocalSmtpTestServer.csproj -- --mode auth-fail
```

### 3. 收件人失败模式

```powershell
cd D:\code\Windows-Socket-Mail-Client
dotnet run --project Tools\LocalSmtpTestServer\LocalSmtpTestServer.csproj -- --mode rcpt-fail
```

默认监听地址：

```text
127.0.0.1:2525
```

默认邮件保存目录：

```text
artifacts/local-smtp-mails/
```

## 客户端界面填写示例

打开你的 WinForms 客户端后，顶部账号配置区直接这样填：

```text
邮箱：student01@test.local
授权码：abc123456
SMTP服务器：127.0.0.1
SMTP端口：2525
POP3服务器：127.0.0.1
POP3端口：110
```

说明：

```text
POP3 只是为了通过当前界面校验
本次测试不依赖真实 POP3
```

## 写邮件页填写示例

### 成功发送时

```text
收件人：student02@test.local
主题：SMTP Test 2026-06-16
正文：
Hello teacher,

This is a SMTP test mail.
Sent from our Windows Socket Mail Client.

Best regards,
student01
```

### 认证失败时

只改授权码：

```text
授权码：wrong-password
```

其他内容可以继续使用：

```text
收件人：student02@test.local
主题：SMTP Auth Fail Test
正文：This mail is used for SMTP auth failure testing.
```

### 收件人失败时

把授权码改回正确值，再改收件人：

```text
收件人：nobody@test.local
主题：SMTP RCPT TO Fail Test
正文：This mail is used for recipient failure testing.
```

## 截图完整流程

### A. SMTP 连接成功图

1. 先启动本地 SMTP 服务器，使用 `success` 模式。
2. 启动 WinForms 客户端。
3. 按上面的示例填写账号配置。
4. 点击顶部的 `测试SMTP`。
5. 切到 `日志` 页截图。

截图里建议保留：

```text
220 欢迎响应
QUIT
状态栏成功提示
```

### B. SMTP 发送成功图

1. 保持本地 SMTP 服务器仍在 `success` 模式。
2. 切到 `写邮件` 页。
3. 按上面的成功示例填写收件人、主题和正文。
4. 点击 `发送`。
5. 切到 `日志` 页截图。

截图里建议保留：

```text
EHLO/HELO
AUTH LOGIN
MAIL FROM
RCPT TO
DATA
250
QUIT
```

### C. 邮件已接收证明图

成功发送后，本地 SMTP 测试服务器会把收到的邮件保存到：

```text
artifacts/local-smtp-mails/
```

你可以：

1. 打开资源管理器进入这个目录
2. 找到刚生成的 `.eml` 文件
3. 截图文件列表
4. 再用记事本打开 `.eml` 文件截图正文

这样可以代替“收件箱收到邮件”的证明截图。

### D. 认证失败图

1. 关闭当前本地 SMTP 服务器窗口
2. 重新用 `auth-fail` 模式启动
3. 客户端中把授权码改成：

```text
wrong-password
```

4. 点击 `发送`
5. 切到 `日志` 页截图

### E. 收件人失败图

1. 关闭当前本地 SMTP 服务器窗口
2. 重新用 `rcpt-fail` 模式启动
3. 客户端里把授权码改回正确值：

```text
abc123456
```

4. 把收件人改成：

```text
nobody@test.local
```

5. 点击 `发送`
6. 切到 `日志` 页截图

## 推荐截图文件名

```text
smtp-00-main-ui.png
smtp-01-connect-success.png
smtp-02-compose-before-send.png
smtp-03-send-success-log.png
smtp-04-local-mail-received.png
smtp-05-auth-fail.png
smtp-06-rcpt-fail.png
```

## 说明

当前本地 SMTP 服务器只用于课程实验和截图，不是完整邮件系统。它的目标是：

```text
让客户端能稳定走完 SMTP 协议流程
让你能独立完成成功和失败场景测试
让日志截图和实验报告有可靠依据
```
