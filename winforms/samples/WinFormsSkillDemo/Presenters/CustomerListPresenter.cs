// 层级：② 表现逻辑（Presenters）
// 职责：响应界面事件 → 调服务 → 更新界面。这里是"点了按钮该干什么"的唯一答案。
// 约束：Presenters/ 不允许 using System.Windows.Forms。
//       本文件只用 System / System.Threading / Models / Services.Abstractions —— 可以单元测试。

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WinFormsSkillDemo.Models;
using WinFormsSkillDemo.Services.Abstractions;

namespace WinFormsSkillDemo.Presenters;

public sealed class CustomerListPresenter : IDisposable
{
    private readonly ICustomerListView _view;
    private readonly ICustomerService _service;

    private IReadOnlyList<Customer> _allCustomers = Array.Empty<Customer>();
    private CancellationTokenSource _cts;
    private bool _disposed;

    public CustomerListPresenter(ICustomerListView view, ICustomerService service)
    {
        _view = view ?? throw new ArgumentNullException(nameof(view));
        _service = service ?? throw new ArgumentNullException(nameof(service));

        _view.SearchTextChanged += OnSearchTextChanged;
        _view.CustomerActivated += OnCustomerActivated;
    }

    /// <summary>状态栏文本。</summary>
    public string Status { get; private set; } = "就绪";

    /// <summary>是否正在忙（界面据此禁用按钮、切光标）。</summary>
    public bool IsBusy { get; private set; }

    /// <summary>当前显示（已筛选）的客户数。</summary>
    public int VisibleCount { get; private set; }

    /// <summary>状态变化通知。界面订阅它来刷新，而不是 Presenter 直接去碰控件。</summary>
    public event EventHandler StateChanged;

    /// <summary>加载（或重新加载）数据。</summary>
    public async Task RefreshAsync()
    {
        if (IsBusy)
        {
            return;   // 防重入
        }

        IsBusy = true;
        _cts = new CancellationTokenSource();
        RaiseStateChanged();

        try
        {
            // Progress<T> 构造时捕获 UI 的 SynchronizationContext，回调自动回到 UI 线程
            var progress = new Progress<int>(percent => SetStatus($"加载中… {percent}%"));

            // 不加 ConfigureAwait(false)：Presenter 需要回到 UI 线程继续更新界面
            _allCustomers = await _service.LoadAsync(progress, _cts.Token);
            ApplyFilter();
        }
        catch (OperationCanceledException)
        {
            SetStatus("已取消");
        }
        finally
        {
            IsBusy = false;

            if (_cts != null)
            {
                _cts.Dispose();
                _cts = null;
            }

            RaiseStateChanged();
        }
    }

    /// <summary>新增一条客户，并重新应用当前筛选。</summary>
    public void AddNewCustomer()
    {
        var customer = _service.CreateNewCustomer(_allCustomers.Count);

        var list = new List<Customer>(_allCustomers) { customer };
        _allCustomers = list;

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var filtered = _service.Search(_allCustomers, _view.SearchText);

        _view.SetCustomers(filtered);
        VisibleCount = filtered.Count;
        SetStatus($"共 {VisibleCount} 位客户");
    }

    private void OnSearchTextChanged(object sender, EventArgs e)
    {
        ApplyFilter();
    }

    private void OnCustomerActivated(object sender, CustomerEventArgs e)
    {
        // 由界面决定怎么显示，Presenter 不碰 MessageBox
        _view.ShowCustomerDetail(e.Customer);
    }

    private void SetStatus(string status)
    {
        Status = status;
        RaiseStateChanged();
    }

    private void RaiseStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _view.SearchTextChanged -= OnSearchTextChanged;
        _view.CustomerActivated -= OnCustomerActivated;

        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }
}
