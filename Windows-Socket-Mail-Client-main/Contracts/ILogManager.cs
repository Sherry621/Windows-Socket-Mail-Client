using MailClient.Models;

namespace MailClient.Contracts;

// 协议日志接口
public interface ILogManager
{
    // 新增日志时触发的事件
    event EventHandler<OperationLogEntry>? LogAdded;

    // 记录普通信息
    void Info(string protocol, string operation, string content);
    // 记录发送的命令
    void Send(string protocol, string content);
    // 记录收到的响应
    void Receive(string protocol, string content);
    // 记录成功操作
    void Success(string protocol, string operation, string content);
    // 记录错误操作
    void Error(string protocol, string operation, string content);

    // 获取所有日志
    IReadOnlyList<OperationLogEntry> GetAll();
    // 清空日志
    void Clear();
}