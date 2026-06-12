using MailClient.Models;

namespace MailClient.Contracts;

// 数据库读写接口
public interface IDatabaseManager
{
    // 初始化建库建表
    Task InitializeAsync(CancellationToken cancellationToken = default);
    // 保存账号配置
    Task SaveAccountConfigAsync(AccountConfig config, CancellationToken cancellationToken = default);
    // 加载账号配置
    Task<AccountConfig?> LoadAccountConfigAsync(CancellationToken cancellationToken = default);
    // 保存邮件列表摘要
    Task SaveMailSummaryAsync(MailSummary summary, CancellationToken cancellationToken = default);
    // 保存已发送邮件记录
    Task SaveSentMailAsync(MailMessageModel mail, SmtpSendResult result, CancellationToken cancellationToken = default);
    // 保存运行日志
    Task SaveOperationLogAsync(OperationLogEntry entry, CancellationToken cancellationToken = default);
}