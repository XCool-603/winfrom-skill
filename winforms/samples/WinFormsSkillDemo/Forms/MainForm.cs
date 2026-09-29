// 层级：① 表现（Forms）—— 窗体
// 职责：布局壳 + 事件转发。业务逻辑全在 Presenters/CustomerListPresenter。
// 约束：Forms/ 允许 using System.Windows.Forms（本文件就是界面）。

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsSkillDemo.Presenters;
using WinFormsSkillDemo.Services.Abstractions;

namespace WinFormsSkillDemo.Forms;

public partial class MainForm : Form
{
    private CustomerListPresenter _presenter;

    /// <summary>
    /// 设计器专用构造函数。
    /// VS 设计器需要一个无参构造来实例化窗体；运行时请用 <see cref="MainForm(ICustomerService)"/>。
    /// 这是"既要依赖注入，又不能让设计器打不开"的标准做法。
    /// </summary>
    public MainForm()
    {
        InitializeComponent();
    }

    /// <summary>运行时构造函数：由 Program.cs（组装根）注入服务。</summary>
    public MainForm(ICustomerService service) : this()
    {
        if (service == null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        // 设计期守卫：构造函数在 VS 设计器里也会执行一遍。
        // 注意用 LicenseManager 而不是 DesignMode —— DesignMode 在构造函数里恒为 false。
        if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
        {
            return;
        }

        // 组装：把视图和服务交给 Presenter，窗体自己不碰业务
        _presenter = new CustomerListPresenter(customerListView, service);
        _presenter.StateChanged += Presenter_StateChanged;

        ApplyPresenterState();
    }

    // 设计器不执行 OnLoad，耗时的加载放这里（构造函数里同步读数据会让窗体白屏）
    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await RefreshSafelyAsync();
    }

    // ---- 事件转发：只做转发，零逻辑 ----

    private async void btnRefresh_Click(object sender, EventArgs e)
    {
        await RefreshSafelyAsync();
    }

    private void btnAdd_Click(object sender, EventArgs e)
    {
        if (_presenter == null)
        {
            return;
        }

        _presenter.AddNewCustomer();
    }

    // ---- 界面刷新：由 Presenter 的状态驱动 ----

    private void Presenter_StateChanged(object sender, EventArgs e)
    {
        ApplyPresenterState();
    }

    private void ApplyPresenterState()
    {
        if (_presenter == null)
        {
            return;
        }

        lblStatus.Text = _presenter.Status;
        btnRefresh.Enabled = !_presenter.IsBusy;
        btnAdd.Enabled = !_presenter.IsBusy;
        customerListView.Enabled = !_presenter.IsBusy;
        Cursor = _presenter.IsBusy ? Cursors.WaitCursor : Cursors.Default;
    }

    private async Task RefreshSafelyAsync()
    {
        if (_presenter == null)
        {
            return;
        }

        try
        {
            await _presenter.RefreshAsync();
        }
        catch (Exception ex)
        {
            // async void 的异常无法被调用方捕获，必须自己兜住
            MessageBox.Show(this, $"加载失败：{ex.Message}", "错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        if (_presenter != null)
        {
            _presenter.StateChanged -= Presenter_StateChanged;
            _presenter.Dispose();
            _presenter = null;
        }

        base.OnFormClosed(e);
    }
}
