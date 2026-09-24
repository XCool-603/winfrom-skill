// ============================================================================
// 示例：Form 的逻辑文件
// 演示：InitializeComponent() 的位置、设计期守卫、异步加载 + 进度、
//       防重入、跨线程安全、事件转发、OnFormClosed 清理。
// ============================================================================

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsSkillDemo.Models;
using WinFormsSkillDemo.Services;
using WinFormsSkillDemo.Views;

namespace WinFormsSkillDemo.Forms;

public partial class MainForm : Form
{
    private readonly ICustomerService _service = new FakeCustomerService();
    private CancellationTokenSource _cts;

    public MainForm()
    {
        // InitializeComponent() 必须早于任何触碰控件的代码
        InitializeComponent();

        // 设计期守卫：构造函数在 VS 设计器里也会执行一遍。
        // 注意用 LicenseManager 而不是 DesignMode —— DesignMode 在构造函数里恒为 false。
        if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
        {
            return;
        }

        // 订阅子控件的事件：只做转发/展示，业务决策留在本窗体
        customerListView.CustomerActivated += CustomerListView_CustomerActivated;
        customerListView.FilteredCountChanged += CustomerListView_FilteredCountChanged;

        UpdateStatus();
    }

    // 设计器不执行 OnLoad，耗时的加载放这里（构造函数里同步读数据会让窗体白屏）
    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        try
        {
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            // async void 的异常无法被调用方捕获，必须自己兜住
            MessageBox.Show(this, $"加载失败：{ex.Message}", "错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void btnRefresh_Click(object sender, EventArgs e)
    {
        try
        {
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"加载失败：{ex.Message}", "错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task RefreshAsync()
    {
        // 防重入：禁用触发按钮，避免用户连点造成重复请求
        btnRefresh.Enabled = false;
        btnAdd.Enabled = false;
        Cursor = Cursors.WaitCursor;

        _cts = new CancellationTokenSource();

        try
        {
            // Progress<T> 构造时捕获 UI 的 SynchronizationContext，
            // 回调自动回到 UI 线程，不需要手写 Invoke。
            var progress = new Progress<int>(percent =>
            {
                // 窗体可能已经在等待期间关闭
                if (IsDisposed || Disposing)
                {
                    return;
                }

                lblStatus.Text = $"加载中… {percent}%";
            });

            var customers = await _service.GetAllAsync(progress, _cts.Token);

            // await 之后已回到 UI 线程，可以直接操作控件。
            // SetCustomers 内部会触发 FilteredCountChanged，状态栏由事件驱动更新。
            customerListView.SetCustomers(customers);
        }
        catch (OperationCanceledException)
        {
            // 用户主动取消，不是错误
            lblStatus.Text = "已取消";
        }
        finally
        {
            btnRefresh.Enabled = true;
            btnAdd.Enabled = true;
            Cursor = Cursors.Default;

            // OnFormClosed 可能已经在 await 期间把 _cts 置空并释放
            if (_cts != null)
            {
                _cts.Dispose();
                _cts = null;
            }
        }
    }

    private void btnAdd_Click(object sender, EventArgs e)
    {
        var index = customerListView.CustomerCount + 1;

        // 对象初始化器在这里完全没问题 —— 禁令只针对 InitializeComponent() 内部
        customerListView.AddCustomer(new Customer
        {
            Name = $"新客户 {index}",
            Email = $"customer{index}@example.com",
        });

        // 状态栏由 CustomerListView.FilteredCountChanged 驱动更新，这里不用再调
    }

    private void CustomerListView_CustomerActivated(object sender, CustomerEventArgs e)
    {
        MessageBox.Show(this, $"双击了：{e.Customer.Name}", "客户",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void CustomerListView_FilteredCountChanged(object sender, EventArgs e)
    {
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        lblStatus.Text = $"共 {customerListView.CustomerCount} 位客户";
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        // 清理：取消未完成的异步操作
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        // 退订子控件事件（子控件生命周期比窗体短，这里主要是养成习惯）
        customerListView.CustomerActivated -= CustomerListView_CustomerActivated;
        customerListView.FilteredCountChanged -= CustomerListView_FilteredCountChanged;

        base.OnFormClosed(e);
    }
}
