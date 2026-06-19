using System;
using System.Windows.Forms;
using MailClient.UI;

namespace MailClient;

static class Program
{

    [STAThread]
    static void Main()
    {
        // 1. 初始化现代 WinForms 运行环境
        Application.SetHighDpiMode(HighDpiMode.SystemAware); // 启用高 DPI 支持，防止界面在高分屏下模糊
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // 2. 配置全局异常捕获核心逻辑
        // 捕获 UI 线程上的未处理异常（例如界面按钮事件内部未加 try-catch 的异常）
        Application.ThreadException += (sender, e) =>
        {
            HandleGlobalException("全局捕获：UI 线程异常", e.Exception);
        };

        // 捕获非 UI 线程/后台异步任务中的未处理异常（例如 Socket 异步接收流断开、后台数据库读写超时）
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                HandleGlobalException("全局捕获：后台/异步线程崩溃", ex);
            }
        };

        // 强制所有未处理异常都流向指定的异常事件处理器
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

        // 3. 启动主窗体
        Application.Run(new MainForm());
    }

    /// 统一的异常弹窗提示，方便在网络协议实验中调试定位 Socket/MIME 错误
    private static void HandleGlobalException(string title, Exception ex)
    {
        // 提取核心错误链，便于一眼看出是 SocketException 还是 IOException
        string innerMessage = ex.InnerException != null ? $"\n内部引发原因: {ex.InnerException.Message}" : string.Empty;

        string errorMessage = $"【程序运行发生未处理错误】\n\n" +
                             $"异常类型: {ex.GetType().FullName}\n" +
                             $"错误提示: {ex.Message}{innerMessage}\n\n" +
                             $"建议检查:\n" +
                             $"1. 是否填写了正确的邮件服务器地址与端口。\n" +
                             $"2. 本地网络是否通畅（或目标端口是否被防火墙拦截）。\n" +
                             $"3. 密码/授权码是否正确。\n\n" +
                             $"详细堆栈追踪:\n{ex.StackTrace}";

        MessageBox.Show(errorMessage, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}