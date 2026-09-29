// 层级：④ 应用（Services）—— 接口
// 职责：定义"能对客户做什么"（业务能力），不关心数据从哪来、界面长什么样。
// 约束：Services/ 不允许 using System.Windows.Forms。

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WinFormsSkillDemo.Models;

namespace WinFormsSkillDemo.Services.Abstractions;

public interface ICustomerService
{
    /// <summary>加载全部客户（已完成排序）。</summary>
    Task<IReadOnlyList<Customer>> LoadAsync(IProgress<int> progress, CancellationToken cancellationToken);

    /// <summary>按关键字筛选。这是业务规则，所以放在服务层，不放在界面里。</summary>
    IReadOnlyList<Customer> Search(IReadOnlyList<Customer> source, string keyword);

    /// <summary>构造一个新客户（命名规则属于业务，不属于界面）。</summary>
    Customer CreateNewCustomer(int existingCount);
}
