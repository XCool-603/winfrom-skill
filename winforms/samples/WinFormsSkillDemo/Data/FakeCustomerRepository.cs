// 层级：⑤ 基础设施（Data）—— 实现
// 职责：真正去拿数据。这里是假数据 + 延迟，真实项目换成 SQL / HTTP 实现即可，
//       上层（Services / Presenters / Views）一行都不用改 —— 这就是分层的价值。
// 约束：Data/ 不允许 using System.Windows.Forms。

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WinFormsSkillDemo.Common.Constants;
using WinFormsSkillDemo.Data.Abstractions;
using WinFormsSkillDemo.Models;

namespace WinFormsSkillDemo.Data;

public sealed class FakeCustomerRepository : ICustomerRepository
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
        var steps = AppConstants.RepositoryProgressSteps;

        for (var step = 1; step <= steps; step++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 服务层/数据层用 ConfigureAwait(false)：不碰 UI，不需要回到 UI 线程
            await Task.Delay(AppConstants.RepositoryDelayMilliseconds, cancellationToken).ConfigureAwait(false);

            // Progress<T> 在构造时捕获了 UI 的 SynchronizationContext，
            // 所以 Report 的回调会自动回到 UI 线程 —— 不需要手写 Invoke。
            progress?.Report(step * 100 / steps);
        }

        return _seed.ToArray();
    }
}
