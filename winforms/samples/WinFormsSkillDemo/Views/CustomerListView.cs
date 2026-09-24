// ============================================================================
// 示例：UserControl 的逻辑文件
// 只做「界面 + 转发」，业务决策交给宿主（MainForm）。
// 对外暴露语义（属性 / 事件），不暴露内部控件字段。
// ============================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using WinFormsSkillDemo.Models;

namespace WinFormsSkillDemo.Views;

public partial class CustomerListView : UserControl
{
    private readonly List<Customer> _allCustomers = new List<Customer>();
    private readonly BindingList<Customer> _customers = new BindingList<Customer>();
    private readonly BindingSource _bsCustomers = new BindingSource();

    // public 无参构造函数 —— 必须。VS 设计器要靠它实例化本控件。
    public CustomerListView()
    {
        InitializeComponent();

        if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
        {
            return;
        }

        // BindingList<T> 而非 List<T>：增删项时界面才会自动刷新
        _bsCustomers.DataSource = _customers;
        dgvCustomers.DataSource = _bsCustomers;
    }

    /// <summary>用户双击了某一行。宿主决定要做什么，控件自己不弹窗。</summary>
    public event EventHandler<CustomerEventArgs> CustomerActivated;

    /// <summary>筛选结果数量变化。宿主据此更新状态栏。</summary>
    public event EventHandler FilteredCountChanged;

    /// <summary>当前显示（已筛选）的客户数。</summary>
    public int CustomerCount => _customers.Count;

    /// <summary>搜索关键字。暴露语义，而不是暴露 txtSearch。</summary>
    public string SearchText
    {
        get => txtSearch.Text;
        set => txtSearch.Text = value ?? string.Empty;
    }

    /// <summary>整体替换数据源。</summary>
    public void SetCustomers(IEnumerable<Customer> customers)
    {
        _allCustomers.Clear();
        _allCustomers.AddRange(customers);
        ApplyFilter();
    }

    /// <summary>追加一条客户。</summary>
    public void AddCustomer(Customer customer)
    {
        if (customer == null)
        {
            throw new ArgumentNullException(nameof(customer));
        }

        _allCustomers.Add(customer);
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var keyword = txtSearch.Text.Trim();

        // 批量修改时先关掉通知，最后 ResetBindings 一次性刷新，避免逐项重绘
        _customers.RaiseListChangedEvents = false;
        try
        {
            _customers.Clear();

            foreach (var customer in _allCustomers)
            {
                if (keyword.Length == 0 || Matches(customer, keyword))
                {
                    _customers.Add(customer);
                }
            }
        }
        finally
        {
            _customers.RaiseListChangedEvents = true;
            _customers.ResetBindings();
        }

        FilteredCountChanged?.Invoke(this, EventArgs.Empty);
    }

    private static bool Matches(Customer customer, string keyword)
    {
        return customer.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0
            || customer.Email.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    // 事件处理器：名字与 Designer.cs 里订阅的一致，签名必须匹配委托
    private void txtSearch_TextChanged(object sender, EventArgs e)
    {
        ApplyFilter();
    }

    private void dgvCustomers_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
    {
        // 表头/行头也会触发，RowIndex < 0 要忽略
        if (e.RowIndex < 0)
        {
            return;
        }

        var customer = _bsCustomers.Current as Customer;
        if (customer == null)
        {
            return;
        }

        CustomerActivated?.Invoke(this, new CustomerEventArgs(customer));
    }
}

/// <summary>双击客户事件的数据。</summary>
public sealed class CustomerEventArgs : EventArgs
{
    public CustomerEventArgs(Customer customer)
    {
        Customer = customer ?? throw new ArgumentNullException(nameof(customer));
    }

    public Customer Customer { get; }
}
