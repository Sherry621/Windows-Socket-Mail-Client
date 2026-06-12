using MailClient.Models;

namespace MailClient.Contracts;

// POP3 邮件接收接口
public interface IPop3ClientSocket
{
    // 连接 POP3 服务器
    Task ConnectAsync(AccountConfig config, CancellationToken cancellationToken = default);
    // 登录账号
    Task LoginAsync(AccountConfig config, CancellationToken cancellationToken = default);
    // 获取邮箱邮件数量和大小
    Task<Pop3MailboxStat> GetStatAsync(CancellationToken cancellationToken = default);
    // 获取邮件列表
    Task<IReadOnlyList<MailSummary>> ListMailsAsync(CancellationToken cancellationToken = default);
    // 读取具体某封邮件的内容
    Task<MailMessageModel> RetrieveMailAsync(int mailNo, CancellationToken cancellationToken = default);
    // 删除指定邮件
    Task DeleteMailAsync(int mailNo, CancellationToken cancellationToken = default);
    // 断开连接
    Task QuitAsync(CancellationToken cancellationToken = default);
}