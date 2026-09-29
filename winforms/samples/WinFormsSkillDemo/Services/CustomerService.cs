// 层级：④ 应用（Services）—— 实现
// 职责：业务规则与用例编排。它只依赖 Data 的【接口】和 Models，不知道数据到底从哪来。
// 约束：Services/ 不允许 using System.Windows.Forms。

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WinFormsSkillDemo.Common.Extensions;
using WinFormsSkillDemo.Data.Abstractions;
using WinFormsSkillDemo.Models;
using WinFormsSkillDemo.Services.Abstractions;

namespace WinFormsSkillDemo.Services;

public sealed class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repository;

    // 依赖注入：需要什么，让别人给我，别自己 new。
    // 好处：单元测试时可以塞一个假的 repository，不需要数据库。
    public CustomerService(ICustomerRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<IReadOnlyList<Customer>> LoadAsync(IProgress<int> progress, CancellationToken cancellationToken)
    {
        var customers = await _repository.GetAllAsync(progress, cancellationToken).ConfigureAwait(false);

        return SortByName(customers);
    }

    public IReadOnlyList<Customer> Search(IReadOnlyList<Customer> source, string keyword)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        // 空关键字 = 返回全部
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return source;
        }

        var trimmed = keyword.Trim();
        var result = new List<Customer>();

        foreach (var customer in source)
        {
            if (customer.Name.ContainsIgnoreCase(trimmed) || customer.Email.ContainsIgnoreCase(trimmed))
            {
                result.Add(customer);
            }
        }

        return result;
    }

    public Customer CreateNewCustomer(int existingCount)
    {
        var index = existingCount + 1;

        return new Customer
        {
            Name = $"新客户 {index}",
            Email = $"customer{index}@example.com",
        };
    }

    private static IReadOnlyList<Customer> SortByName(IReadOnlyList<Customer> customers)
    {
        var list = new List<Customer>(customers);
        list.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.CurrentCulture));
        return list;
    }
}
