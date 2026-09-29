// 层级：① 表现（Views）—— UserControl
// 职责：只负责"显示"和"收集输入"。零业务逻辑 —— 逻辑全在 Presenters/CustomerListPresenter。
// 约束：Views/ 允许 using System.Windows.Forms（本文件就是界面）。
//       对外只暴露语义（属性 + 事件），绝不把控件字段改成 public。

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using WinFormsSkillDemo.Models;
using WinFormsSkillDemo.Presenters;

namespace WinFormsSkillDemo.Views;

public partial class CustomerListView : UserControl, ICustomerListView
{
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

    // ---- ICustomerListView 实现 ----

    public event EventHandler SearchTextChanged;

    public event EventHandler<CustomerEventArgs> CustomerActivated;

    public string SearchText
    {
        get => txtSearch.Text;
        set => txtSearch.Text = value ?? string.Empty;
    }

    public void SetCustomers(IEnumerable<Customer> customers)
    {
        // 批量修改时先关掉通知，最后 ResetBindings 一次性刷新，避免逐项重绘
        _customers.RaiseListChangedEvents = false;
        try
        {
            _customers.Clear();

            if (customers != null)
            {
                foreach (var customer in customers)
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
    }

    public void ShowCustomerDetail(Customer customer)
    {
        if (customer == null)
        {
            return;
        }

        // "怎么显示"是界面的决定，所以 MessageBox 写在这里，而不是 Presenter 里
        MessageBox.Show(
            this,
            $"双击了：{customer.Name}{Environment.NewLine}{customer.Email}",
            "客户",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    // ---- 事件处理器：名字与 Designer.cs 里订阅的一致 ----

    private void txtSearch_TextChanged(object sender, EventArgs e)
    {
        SearchTextChanged?.Invoke(this, EventArgs.Empty);
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
