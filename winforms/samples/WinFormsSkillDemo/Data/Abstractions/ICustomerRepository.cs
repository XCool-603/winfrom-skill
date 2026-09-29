// 层级：⑤ 基础设施（Data）—— 接口
// 职责：定义"数据从哪来"，不关心具体实现（数据库 / API / 文件 / 假数据）。
// 约束：Data/ 不允许 using System.Windows.Forms。

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WinFormsSkillDemo.Models;

namespace WinFormsSkillDemo.Data.Abstractions;

public interface ICustomerRepository
{
    /// <summary>读取全部客户。progress 用于上报 0~100 的进度。</summary>
    Task<IReadOnlyList<Customer>> GetAllAsync(IProgress<int> progress, CancellationToken cancellationToken);
}
