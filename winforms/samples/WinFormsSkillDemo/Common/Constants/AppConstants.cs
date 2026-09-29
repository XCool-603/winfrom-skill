// 层级：横切（Common）
// 职责：全项目共用的常量。放在这里而不是散落在各处，改一处即可全局生效。
// 约束：Common/ 不允许 using System.Windows.Forms。

namespace WinFormsSkillDemo.Common.Constants;

public static class AppConstants
{
    /// <summary>假仓库模拟一次 IO 的延迟（毫秒）。</summary>
    public const int RepositoryDelayMilliseconds = 80;

    /// <summary>假仓库上报加载进度的步数。</summary>
    public const int RepositoryProgressSteps = 5;
}
