using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WinFormsSkillDemo.Models;

/// <summary>
/// 客户模型。实现 <see cref="INotifyPropertyChanged"/>，
/// 属性变化才能自动反映到绑定控件上。
/// </summary>
public sealed class Customer : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _email = string.Empty;

    public event PropertyChangedEventHandler PropertyChanged;

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public string Email
    {
        get => _email;
        set => SetField(ref _email, value);
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
