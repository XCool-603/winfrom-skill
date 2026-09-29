// 层级：② 表现逻辑（Presenters）—— 界面接口
// 职责：描述 Presenter 需要界面提供的能力。只暴露"语义"，不暴露任何控件。
// 约束：Presenters/ 不允许 using System.Windows.Forms —— 所以这个接口里
//       不会出现 TextBox / DialogResult / MessageBox 之类的东西。
//       正因为如此，Presenter 才能脱离 UI 做单元测试。

using System;
using System.Collections.Generic;
using WinFormsSkillDemo.Models;

namespace WinFormsSkillDemo.Presenters;

/// <summary>客户列表界面必须实现的能力。</summary>
public interface ICustomerListView
{
    /// <summary>用户在搜索框里输入的内容。</summary>
    string SearchText { get; }

    /// <summary>把要显示的数据交给界面。</summary>
    void SetCustomers(IEnumerable<Customer> customers);

    /// <summary>显示某个客户的详情。怎么显示由界面决定，Presenter 不关心。</summary>
    void ShowCustomerDetail(Customer customer);

    /// <summary>搜索框内容变化。</summary>
    event EventHandler SearchTextChanged;

    /// <summary>用户双击了某个客户。</summary>
    event EventHandler<CustomerEventArgs> CustomerActivated;
}
