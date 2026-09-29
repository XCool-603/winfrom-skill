// 层级：③ 领域（Models）
// 职责：事件参数，纯数据。
// 约束：Models/ 不引用任何其他层，更不允许 using System.Windows.Forms。
// 从 Views/CustomerListView.cs 里搬出来的 —— 事件参数属于模型，不属于界面。

using System;

namespace WinFormsSkillDemo.Models;

/// <summary>客户相关事件的数据载体。</summary>
public sealed class CustomerEventArgs : EventArgs
{
    public CustomerEventArgs(Customer customer)
    {
        Customer = customer ?? throw new ArgumentNullException(nameof(customer));
    }

    public Customer Customer { get; }
}
