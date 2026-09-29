// 层级：横切（Common）
// 职责：通用扩展方法。与业务无关，谁都可以用。
// 约束：Common/ 不允许 using System.Windows.Forms。

using System;

namespace WinFormsSkillDemo.Common.Extensions;

public static class StringExtensions
{
    /// <summary>忽略大小写的包含判断。关键字为空时视为匹配（便于"空搜索 = 全部"）。</summary>
    public static bool ContainsIgnoreCase(this string source, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return true;
        }

        return source != null && source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
