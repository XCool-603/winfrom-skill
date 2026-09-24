using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WinFormsSkillDemo.Models;

namespace WinFormsSkillDemo.Services;

public interface ICustomerService
{
    Task<IReadOnlyList<Customer>> GetAllAsync(IProgress<int> progress, CancellationToken cancellationToken);
}

/// <summary>
/// 假的客户服务：用延迟模拟 IO，不依赖数据库，保证示例可以直接跑起来。
/// 注意这里用 ConfigureAwait(false) —— 服务层不碰 UI，不需要回到 UI 线程。
/// </summary>
public sealed class FakeCustomerService : ICustomerService
{
    private readonly List<Customer> _seed = new List<Customer>
    {
        new Customer { Name = "张三", Email = "zhangsan@example.com" },
        new Customer { Name = "李四", Email = "lisi@example.com" },
        new Customer { Name = "王五", Email = "wangwu@example.com" },
        new Customer { Name = "赵六", Email = "zhaoliu@example.com" },
        new Customer { Name = "钱七", Email = "qianqi@example.com" },
        new Customer { Name = "孙八", Email = "sunba@example.com" },
        new Customer { Name = "周九", Email = "zhoujiu@example.com" },
        new Customer { Name = "吴十", Email = "wushi@example.com" },
    };

    public async Task<IReadOnlyList<Customer>> GetAllAsync(IProgress<int> progress, CancellationToken cancellationToken)
    {
        for (var step = 1; step <= 5; step++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 服务层用 ConfigureAwait(false)：不需要回到 UI 线程
            await Task.Delay(80, cancellationToken).ConfigureAwait(false);

            // Progress<T> 在构造时捕获了 UI 的 SynchronizationContext，
            // 所以 Report 的回调会自动回到 UI 线程 —— 不需要手写 Invoke。
            progress?.Report(step * 20);
        }

        return _seed.ToArray();
    }
}
