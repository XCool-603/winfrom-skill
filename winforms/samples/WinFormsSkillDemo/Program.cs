// ============================================================================
// 程序入口 + 组装根（Composition Root）
//
// 这是全项目【唯一】new 具体实现的地方。分层规则：
//   Forms/Views → Presenters → Services → Data
// 谁都不许自己 new 下层实现，都由这里装配好再传进去（依赖注入）。
// ============================================================================

using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsSkillDemo.Data;
using WinFormsSkillDemo.Data.Abstractions;
using WinFormsSkillDemo.Forms;
using WinFormsSkillDemo.Services;
using WinFormsSkillDemo.Services.Abstractions;

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

        Application.ThreadException += (sender, e) =>
        {
            MessageBox.Show(
                $"发生未处理的错误：{e.Exception.Message}",
                "错误",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        };

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

        TaskScheduler.UnobservedTaskException += (sender, e) => e.SetObserved();

        // ===================== 组装（唯一 new 实现的地方） =====================
        // 换成真实数据库时，只需把下面第一行改成 new SqlCustomerRepository(conn)，
        // Services / Presenters / Forms 一行都不用动。
        ICustomerRepository repository = new FakeCustomerRepository();      // ⑤ 基础设施
        ICustomerService service = new CustomerService(repository);         // ④ 应用服务

        Application.Run(new MainForm(service));                            // ① 表现层
    }
}
