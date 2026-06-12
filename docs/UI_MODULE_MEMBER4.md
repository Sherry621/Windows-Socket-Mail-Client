# 成员四 UI 模块完成说明

本文档记录成员四负责的 WinForms 图形界面模块实现内容，便于后续协作和实验报告填写。

## 负责范围

成员四负责 `UI/` 目录下的 WinForms 界面实现，主要文件为：

```text
UI/MainForm.cs
```

本次实现只负责界面、控件布局、用户输入校验和按钮事件绑定。SMTP、POP3、SQLite 的具体底层逻辑仍由对应成员在 `Protocols/`、`Networking/`、`Data/` 中实现。

## 已完成界面

### 界面美化

当前 UI 已进行统一视觉调整：

```text
顶部自适应渐变标题栏
右侧 SMTP / POP3 / Socket / SQLite 协议标签
浅色主背景
白色内容区域
统一按钮颜色和悬停效果
统一输入框边框和间距
统一 DataGridView 表头、选中行和隔行背景
深色日志显示区域
底部状态栏
```

整体风格偏向课程实验工具界面，重点突出账号配置、协议操作和日志展示。标题栏已改为表格自适应布局，避免副标题被下方账号配置区域遮挡。

### 账号配置区

账号配置区位于主窗口顶部，包含：

```text
邮箱
授权码
SMTP服务器
SMTP端口
POP3服务器
POP3端口
保存配置
测试SMTP
测试POP3
```

按钮行为：

- 保存配置：调用 `IDatabaseManager.SaveAccountConfigAsync`。
- 测试 SMTP：调用 `ISmtpClientSocket.ConnectAsync` 和 `QuitAsync`。
- 测试 POP3：调用 `IPop3ClientSocket.ConnectAsync`、`LoginAsync` 和 `QuitAsync`。

### 写邮件页

写邮件页包含：

```text
收件人
主题
正文
发送
清空
```

发送按钮行为：

1. 检查账号配置是否完整。
2. 检查收件人和主题是否为空。
3. 构造 `MailMessageModel`。
4. 调用 `ISmtpClientSocket.SendMailAsync`。
5. 调用 `IDatabaseManager.SaveSentMailAsync` 保存发送记录。
6. 将发送结果添加到“已发送”页面。

### 收件箱页

收件箱页包含：

```text
刷新收件箱
阅读
删除
邮件列表 DataGridView
邮件详情区域
```

邮件列表字段：

```text
编号
发件人
主题
日期
大小
状态
```

按钮行为：

- 刷新收件箱：调用 POP3 接口获取邮件状态和邮件列表。
- 阅读：调用 `IPop3ClientSocket.RetrieveMailAsync`，并在邮件详情区域显示内容。
- 删除：弹出确认框后调用 `IPop3ClientSocket.DeleteMailAsync`。

### 已发送页

已发送页包含：

```text
刷新
已发送列表 DataGridView
```

当前已发送列表使用界面内存中的发送结果展示。后续数据库成员补充查询接口后，可以改为从 SQLite 读取历史发送记录。

### 日志页

日志页包含：

```text
清空日志
日志文本框
```

日志页监听 `ILogManager.LogAdded` 事件，实时显示系统、SMTP、POP3 等模块输出的日志。

## 接口调用边界

UI 层只调用以下接口：

```text
ISmtpClientSocket
IPop3ClientSocket
IDatabaseManager
ILogManager
IMailParser
```

UI 层没有直接编写 SMTP/POP3 协议命令，也没有直接操作 SQLite。

## 输入校验

当前界面已完成以下校验：

```text
邮箱不能为空
授权码不能为空
SMTP服务器不能为空
POP3服务器不能为空
收件人不能为空
主题不能为空
正文为空时弹出确认提示
阅读或删除邮件前必须先选择邮件
```

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

## 后续可扩展点

后续可以继续增强：

```text
1. 从 SQLite 加载历史已发送记录。
2. 给账号配置增加“显示/隐藏授权码”按钮。
3. 给收件箱增加搜索或过滤。
4. 给邮件正文增加富文本显示。
5. 将各个 Tab 拆分成独立 UserControl，降低 MainForm 代码长度。
```
