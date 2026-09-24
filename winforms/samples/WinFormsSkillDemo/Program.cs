using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsSkillDemo;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // 由 csproj 的 ApplicationHighDpiMode / ApplicationVisualStyles /
        // ApplicationUseCompatibleTextRendering 生成。不要再手写
        // SetHighDpiMode / EnableVisualStyles / SetCompatibleTextRenderingDefault。
        ApplicationConfiguration.Initialize();

        // 全局异常兜底：必须在创建任何窗口之前设置
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

        // UI 线程未处理异常
        Application.ThreadException += (sender, e) =>
        {
            MessageBox.Show(
                $"发生未处理的错误：{e.Exception.Message}",
                "错误",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        };

        // 非 UI 线程未处理异常：无法阻止进程退出，只能提示
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                MessageBox.Show(
                    $"发生严重错误，程序即将退出：{ex.Message}",
                    "错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        };

        // 未观察的 Task 异常：标记为已观察，避免进程崩溃
        TaskScheduler.UnobservedTaskException += (sender, e) => e.SetObserved();

        Application.Run(new Forms.MainForm());
    }
}
