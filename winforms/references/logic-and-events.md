# 逻辑、事件、异步与数据绑定

[SKILL.md](../SKILL.md) 的 R1/R3 展开：**`*.Designer.cs` 是布局，`*.cs` 是行为。** 本文件讲后者。

---

## 1. 逻辑文件的标准结构

```csharp
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Demo.Forms;

public partial class CustomerEditForm : Form
{
    // ① 字段：依赖、状态。只读依赖用 readonly
    private readonly ICustomerService _service;
    private readonly BindingList<Customer> _customers = new();
    private readonly BindingSource _bsCustomers = new();

    // ② 构造函数：InitializeComponent() 尽早调用
    public CustomerEditForm(ICustomerService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        InitializeComponent();      // ★ 必须在任何控件访问之前
        SetupBindings();            // 之后才是逻辑
        LoadData();
    }

    // ③ 初始化辅助方法（private，只在构造函数里调）
    private void SetupBindings() { /* ... */ }
    private void LoadData() { /* ... */ }

    // ④ 事件处理器：命名与 Designer.cs 里订阅的完全一致
    private void btnSave_Click(object sender, EventArgs e) { /* ... */ }
    private void txtName_Validating(object sender, CancelEventArgs e) { /* ... */ }

    // ⑤ 业务方法（private）
    private bool ValidateInput() { /* ... */ }

    // ⑥ 生命周期重写：清理
    protected override void OnFormClosed(FormClosedEventArgs e) { /* ... */ }
}
```

**顺序纪律**：

- `InitializeComponent()` **永远在构造函数最前面**（字段赋值不算触碰控件，可以放它前面——构造函数注入依赖时必须如此）。
- 任何触碰控件的代码（含 `SetupBindings()`、`LoadData()`）**必须在 `InitializeComponent()` 之后**。
- 重活（读库、拉网络）**不要在构造函数里同步做**——构造函数里同步阻塞会让窗体白屏。用 `OnLoad` + `async`（见 §4）。

**绝对不要**：

- 在构造函数里再次调用 `InitializeComponent()`（控件会被建两遍，事件订阅翻倍）。
- 在 `*.Designer.cs` 里写任何逻辑（下次设计器保存就没了）。

---

## 2. 设计期守卫

构造函数的逻辑会在 **VS 设计器打开窗体时也执行一遍**。这是设计器卡死/报错的主要原因。

```csharp
public CustomerEditForm(ICustomerService service)
{
    _service = service;
    InitializeComponent();

    // ✅ 可靠：LicenseManager 在构造函数里就能判断设计期
    if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
    {
        return;
    }

    LoadData();     // 只有运行时才执行
}
```

> **注意**：`Control.DesignMode` 属性在**构造函数里始终返回 `false`**（控件还没被 sited），所以在构造函数里判断设计期**必须**用 `LicenseManager.UsageMode`。这是很常见的坑。

**更好的做法**：把初始化挪到 `OnLoad`，设计器根本不会执行 `OnLoad`。

```csharp
protected override async void OnLoad(EventArgs e)
{
    base.OnLoad(e);
    await LoadDataAsync();
}
```

`UserControl` 没有 `OnLoad`，用 `Load` 事件或 `LicenseManager` 守卫。

---

## 3. 事件：Designer 与逻辑的契约

事件是唯一横跨两个文件的东西，**签名必须严丝合缝**。

```csharp
// Designer.cs —— 订阅
this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
this.txtName.Validating += new System.ComponentModel.CancelEventHandler(this.txtName_Validating);
this.dgvCustomers.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvCustomers_CellDoubleClick);
this.tmrRefresh.Tick += new System.EventHandler(this.tmrRefresh_Tick);
```

```csharp
// .cs —— 实现，签名逐字匹配
private void btnSave_Click(object sender, EventArgs e) { }

private void txtName_Validating(object sender, CancelEventArgs e) { }

private void dgvCustomers_CellDoubleClick(object sender, DataGridViewCellEventArgs e) { }

private void tmrRefresh_Tick(object sender, EventArgs e) { }
```

**常用委托签名速查**：

| 事件 | 委托 | 处理器签名 |
|:---|:---|:---|
| `Click` / `Load` / `Tick` / `TextChanged` | `EventHandler` | `(object sender, EventArgs e)` |
| `Validating` / `FormClosing` | `CancelEventHandler` | `(object sender, CancelEventArgs e)` |
| `KeyDown` / `KeyPress` | `KeyEventHandler` / `KeyPressEventHandler` | `(object sender, KeyEventArgs e)` |
| `MouseClick` | `MouseEventHandler` | `(object sender, MouseEventArgs e)` |
| `CellDoubleClick` / `CellClick` | `DataGridViewCellEventHandler` | `(object sender, DataGridViewCellEventArgs e)` |
| `SelectedIndexChanged` | `EventHandler` | `(object sender, EventArgs e)` |

**注意**：设计器默认生成 `private void X(object sender, EventArgs e)`。写 `private void X()`（无参）**不能**被订阅——编译错。

> **关于 `<Nullable>`**：本文档与模板统一用 `object sender`（与 VS 设计器生成保持一致），按**未开启 Nullable** 编写。
> 若项目开启 `<Nullable>enable</Nullable>`：
> - 事件处理器参数要写成 `object? sender`，否则报 **CS8622**（实测确认）；
> - 事件声明要写成 `event EventHandler? Xxx`，否则报 **CS8618**；
> - `*.Designer.cs` 里的 `components = null` 报 **CS8625**、控件字段报 **CS8618**——这是设计器自己的写法，**别改它**，在文件头加 `#nullable disable` 即可。
>
> 完整的实测对照表和推荐处理方式见 [designer-cs-contract.md](./designer-cs-contract.md) §7.1。
> **无论哪种情况，改完都要回 VS 设计器保存一次，确认没被改回去。**

**多个控件共用一个处理器**（如所有按钮共用一个）：

```csharp
// Designer.cs
this.btnSave.Click += new System.EventHandler(this.ActionButton_Click);
this.btnCancel.Click += new System.EventHandler(this.ActionButton_Click);

// .cs —— 用 sender 或 Tag 区分
private void ActionButton_Click(object sender, EventArgs e)
{
    if (sender is not Button button)
    {
        return;
    }

    switch (button.Name)
    {
        case "btnSave":
            Save();
            break;
        case "btnCancel":
            Close();
            break;
    }
}
```

用 `switch (button.Name)` 而不是 `switch (button.Text)`——文本会被本地化改掉，`Name` 稳定。

---

## 4. `async` / `await`

### 4.1 三条规则

1. **`async void` 只用于事件处理器。** 其他地方一律 `async Task` / `async Task<T>`。
2. **UI 代码不要加 `ConfigureAwait(false)`。** WinForms 的 `SynchronizationContext` 会把后续代码送回 UI 线程，这正是你要的。`ConfigureAwait(false)` 只用在类库/服务层。
3. **事件处理器里的异常必须自己 catch。** `async void` 的异常无法被调用方捕获，会直接崩进程。

### 4.2 标准写法

```csharp
private async void btnLoad_Click(object sender, EventArgs e)
{
    // ① 进入时禁用 + 显示忙碌（防止重入和重复点击）
    btnLoad.Enabled = false;
    prgLoad.Visible = true;
    Cursor = Cursors.WaitCursor;

    try
    {
        // ② 用 IProgress<T> 回报进度，Progress<T> 自动回到 UI 线程
        var progress = new Progress<int>(percent => prgLoad.Value = percent);

        // ③ 取消支持
        _cts = new CancellationTokenSource();
        var data = await _service.GetCustomersAsync(progress, _cts.Token);

        // ④ 这里已经回到 UI 线程，可以直接碰控件
        _customers.Clear();
        foreach (var item in data)
        {
            _customers.Add(item);
        }
    }
    catch (OperationCanceledException)
    {
        // 用户主动取消：静默，不算错误
    }
    catch (Exception ex)
    {
        // ⑤ 事件处理器必须自己处理异常
        MessageBox.Show(this, $"加载失败：{ex.Message}", "错误",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
    finally
    {
        // ⑥ 恢复 UI 状态，必须放在 finally
        btnLoad.Enabled = true;
        prgLoad.Visible = false;
        Cursor = Cursors.Default;
        _cts?.Dispose();
        _cts = null;
    }
}
```

**要点**：

- **禁用触发按钮**是防重入的关键。用户连点两次 = 两次请求 = 数据错乱。
- `Progress<T>` 在构造时捕获当前 `SynchronizationContext`，回调自动 marshal 回 UI 线程——**不需要手写 `Invoke`**。这是回报进度的首选方式。
- `OperationCanceledException` 要单独 catch，它不是错误。
- `finally` 里恢复状态，否则异常后按钮永久禁用。

### 4.3 不要在构造函数里 `await`

构造函数不能是 `async`。用 `OnLoad`：

```csharp
protected override async void OnLoad(EventArgs e)
{
    base.OnLoad(e);

    try
    {
        await LoadDataAsync();
    }
    catch (Exception ex)
    {
        MessageBox.Show(this, ex.Message, "加载失败");
    }
}
```

`OnLoad` 是 `protected override`，**不是设计器订阅的事件**，所以它属于 `*.cs`，写在这里完全正确。

---

## 5. 跨线程更新 UI

WinForms 控件**只能在创建它的线程（UI 线程）上访问**。跨线程直接改控件会抛 `InvalidOperationException`，或在调试时随机崩溃。

### 5.1 什么时候真的需要 `Invoke`

`await` 已经覆盖了绝大多数场景。**真正需要手工 marshal 的**是"外部线程主动推数据"的回调：

| 来源 | 线程 | 需要 Invoke？ |
|:---|:---|:---|
| `await` 之后 | UI 线程 | 不需要 |
| `Progress<T>` 回调 | UI 线程 | 不需要 |
| `System.Windows.Forms.Timer.Tick` | UI 线程 | 不需要 |
| `System.Timers.Timer.Elapsed` | 线程池 | **需要** |
| `System.Threading.Timer` 回调 | 线程池 | **需要** |
| 串口 `SerialPort.DataReceived` | 线程池 | **需要** |
| `Task.Run` 内部 | 线程池 | **需要** |
| `BackgroundWorker.ProgressChanged` | UI 线程 | 不需要 |

**首选方案**：能用 `System.Windows.Forms.Timer` 就别用 `System.Timers.Timer`；能用 `await` 就别用回调。**从源头避免跨线程，比到处写 `Invoke` 好。**

### 5.2 标准 marshal 写法

```csharp
private void OnDataReceived(object sender, string data)
{
    // ① 先判断，再递归回 UI 线程
    if (InvokeRequired)
    {
        // ② 用 BeginInvoke（异步投递），不用 Invoke（同步阻塞）
        BeginInvoke(new Action<string>(OnDataReceived), data);
        return;
    }

    // ③ 到这里一定在 UI 线程
    txtLog.AppendText(data);
    txtLog.SelectionStart = txtLog.TextLength;
    txtLog.ScrollToCaret();
}
```

**为什么用 `BeginInvoke` 而不是 `Invoke`**：`Invoke` 是同步的，如果 UI 线程正等着发送方（典型：`Invoke` 里又触发了对方的事件），就死锁。`BeginInvoke` 异步投递，不阻塞调用线程。

**关闭时的陷阱**：`BeginInvoke` 在窗体已销毁后会抛 `ObjectDisposedException`。回调里要守卫：

```csharp
private void OnDataReceived(object sender, string data)
{
    if (IsDisposed || Disposing || !IsHandleCreated)
    {
        return;     // 窗体正在/已经关闭，丢弃这次回调
    }

    if (InvokeRequired)
    {
        BeginInvoke(new Action<string>(OnDataReceived), data);
        return;
    }

    // ...
}
```

---

## 6. 数据绑定

### 6.1 模型：`INotifyPropertyChanged`

```csharp
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Demo.Models;

public sealed class Customer : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _email = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

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

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

### 6.2 绑定：`BindingSource` + `BindingList<T>`

```csharp
private readonly BindingList<Customer> _customers = new();
private readonly BindingSource _bsCustomers = new();

private void SetupBindings()
{
    _bsCustomers.DataSource = _customers;
    dgvCustomers.DataSource = _bsCustomers;

    txtName.DataBindings.Add(
        nameof(txtName.Text),
        _bsCustomers,
        nameof(Customer.Name),
        formattingEnabled: true,
        DataSourceUpdateMode.OnPropertyChanged);
}
```

**为什么用 `BindingList<T>` 而不是 `List<T>`**：`List<T>` 不实现 `IBindingList`，增删项时界面**不会**自动刷新。`BindingList<T>` 会。

**为什么用 `BindingSource` 中间层**：`BindingSource` 提供 `Current`、`MoveNext()`、`Position`、`EndEdit()`、排序/筛选，是 WinForms 绑定的枢纽。直接绑 `BindingList<T>` 也能工作，但少了这层能力。**多控件绑定同一份数据时，`BindingSource` 是必需的**（否则每个控件各自维护游标）。

**提交编辑**：用户还在编辑单元格时，值没写回模型。读数据前先 `EndEdit()`：

```csharp
private void btnSave_Click(object sender, EventArgs e)
{
    _bsCustomers.EndEdit();      // ★ 把单元格编辑提交到模型
    var current = (Customer?)_bsCustomers.Current;
    // ...
}
```

**`DataGridView` 记得 `AutoGenerateColumns = false`** 并在 Designer 里显式定义列（见 [layout-and-decomposition.md](./layout-and-decomposition.md) §10）。

### 6.3 数据绑定的 Designer.cs 写法

`DataBindings.Add(...)` 是设计器允许并会生成的语句。但 **`BindingSource` 通常在 `*.cs` 里创建和赋值**（因为数据源是运行时才知道的），所以：

- **Designer.cs**：只放控件、列定义、以及必要的 `DataBindings.Add(new Binding(...))`（如果绑定在设计期就确定）。
- **`*.cs`**：`_bsCustomers.DataSource = _customers;` 和 `dgvCustomers.DataSource = _bsCustomers;`。

**不要**在 Designer.cs 里写 `_bsCustomers.DataSource = GetData();`——设计期执行会连库。

---

## 7. 校验

### 7.1 `Validating` + `ErrorProvider`（推荐）

```csharp
private void txtEmail_Validating(object sender, CancelEventArgs e)
{
    var text = txtEmail.Text.Trim();

    if (text.Length > 0 && !text.Contains('@'))
    {
        errInput.SetError(txtEmail, "邮箱格式不正确");
        e.Cancel = true;        // 阻止离开该控件
        return;
    }

    errInput.SetError(txtEmail, string.Empty);   // 清除错误图标
}
```

配合 Designer.cs 里的：

```csharp
this.errInput = new System.Windows.Forms.ErrorProvider(this.components);
this.errInput.ContainerControl = this;
```

**要点**：

- 清除错误必须 `SetError(ctrl, string.Empty)`，**不能**传 `null`（会抛异常）。
- `e.Cancel = true` 会阻止焦点离开。**副作用**：用户点"取消"按钮时也会被拦住。解决办法是给取消按钮设 `CausesValidation = false`（在 Designer.cs 里）：
  ```csharp
  this.btnCancel.CausesValidation = false;
  ```
- 整表校验（点保存时）不要依赖 `Validating`，直接调一个显式的 `ValidateInput()` 方法返回 `bool`（见 [designer-cs-contract.md](./designer-cs-contract.md) §5 范例）。

### 7.2 输入过滤

```csharp
// 只允许数字：用 KeyPress
private void txtQty_KeyPress(object sender, KeyPressEventArgs e)
{
    if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
    {
        e.Handled = true;       // 吞掉按键
    }
}
```

**不要**在 `TextChanged` 里改 `Text` 做过滤——会触发递归 `TextChanged`，且光标位置会跳。

---

## 8. 生命周期与资源释放

### 8.1 什么会被自动释放

`components` 容器里的东西（`Timer`、`ErrorProvider`、`ToolTip`、`ImageList`、`BindingSource` 等从工具箱拖的非可视组件）由设计器生成的 `Dispose(bool)` 自动释放。**不用管**。

### 8.2 什么必须手工释放

在 `*.cs` 里 `new` 出来的东西：

| 资源 | 释放方式 |
|:---|:---|
| `System.Threading.Timer` / `System.Timers.Timer` | `Stop()` + `Dispose()` |
| `CancellationTokenSource` | `Cancel()` + `Dispose()` |
| `HttpClient` | 通常共享静态实例；自建的才 `Dispose()` |
| `SerialPort` / `FileStream` / `SqlConnection` | `Dispose()`（优先 `using`） |
| 订阅的**外部对象**事件 | `-=` 退订，否则目标对象被事件引用住，无法回收 |
| 自己 `new` 的 `Font` / `Brush` / `Pen` | `Dispose()` |

```csharp
protected override void OnFormClosed(FormClosedEventArgs e)
{
    // ① 先取消未完成的异步操作
    _cts?.Cancel();
    _cts?.Dispose();
    _cts = null;

    // ② 停掉自建定时器
    _pollTimer?.Dispose();
    _pollTimer = null;

    // ③ 退订外部事件（防止内存泄漏和关闭后回调）
    _service.DataReceived -= OnDataReceived;

    base.OnFormClosed(e);
}
```

**`FormClosing` vs `FormClosed`**：

- `FormClosing`：还能取消（`e.Cancel = true`）。适合"有未保存修改，确认要关闭吗？"
- `FormClosed`：已经关了，只能清理。**清理逻辑放这里。**

```csharp
protected override void OnFormClosing(FormClosingEventArgs e)
{
    if (_isDirty && e.CloseReason == CloseReason.UserClosing)
    {
        var result = MessageBox.Show(this, "有未保存的修改，确定关闭？", "确认",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
        {
            e.Cancel = true;
            return;
        }
    }

    base.OnFormClosing(e);
}
```

### 8.3 全局异常兜底

放在 `Program.cs`，**在 `Application.Run` 之前**：

```csharp
[STAThread]
static void Main()
{
    ApplicationConfiguration.Initialize();

    // ① 让 Application.ThreadException 生效（必须在创建任何窗口之前）
    Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

    // ② UI 线程未处理异常
    Application.ThreadException += (sender, e) =>
    {
        Log(e.Exception);
        MessageBox.Show($"发生错误：{e.Exception.Message}", "错误",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
    };

    // ③ 非 UI 线程未处理异常（无法阻止进程退出，只能记录）
    AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
    {
        if (e.ExceptionObject is Exception ex)
        {
            Log(ex);
        }
    };

    // ④ 未观察的 Task 异常
    TaskScheduler.UnobservedTaskException += (sender, e) =>
    {
        Log(e.Exception);
        e.SetObserved();        // 阻止进程崩溃
    };

    Application.Run(new MainForm());
}
```

**注意**：异常处理器里**不要**再抛异常（`MessageBox` 本身失败就会直接崩），并保证 `Log` 是绝对安全的（写文件失败要吞掉）。

---

## 9. 轻量分层：让逻辑可测

WinForms 的 `Form` 直接塞业务逻辑，结果是**无法单元测试**（要跑消息循环、要真实控件）。轻量做法是把逻辑挪到纯 C# 类里。

### 9.1 三层

```
View (Form / UserControl)   ← 只做界面 + 转发事件。零业务逻辑。
    ↓ 事件 / 属性
Presenter / ViewModel       ← 业务逻辑。纯 C#，无 WinForms 引用，可测试。
    ↓
Service / Repository        ← 数据访问
```

### 9.2 具体做法

**View 暴露语义接口**（不是控件）：

```csharp
public interface ICustomerEditView
{
    string CustomerName { get; set; }
    string Email { get; set; }
    event EventHandler SaveRequested;
    event EventHandler CancelRequested;
    void ShowError(string message);
    void CloseView(DialogResult result);
}
```

**Form 实现它**：

```csharp
public partial class CustomerEditForm : Form, ICustomerEditView
{
    private readonly CustomerEditPresenter _presenter;

    public CustomerEditForm(ICustomerService service)
    {
        InitializeComponent();
        _presenter = new CustomerEditPresenter(this, service);
    }

    public string CustomerName
    {
        get => txtName.Text;
        set => txtName.Text = value ?? string.Empty;
    }

    public string Email
    {
        get => txtEmail.Text;
        set => txtEmail.Text = value ?? string.Empty;
    }

    public event EventHandler? SaveRequested;
    public event EventHandler? CancelRequested;

    public void ShowError(string message) =>
        MessageBox.Show(this, message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);

    public void CloseView(DialogResult result)
    {
        DialogResult = result;
        Close();
    }

    // 只做转发，零逻辑
    private void btnSave_Click(object sender, EventArgs e) => SaveRequested?.Invoke(this, EventArgs.Empty);
    private void btnCancel_Click(object sender, EventArgs e) => CancelRequested?.Invoke(this, EventArgs.Empty);
}
```

**Presenter 承载逻辑**（可单元测试）：

```csharp
public sealed class CustomerEditPresenter
{
    private readonly ICustomerEditView _view;
    private readonly ICustomerService _service;

    public CustomerEditPresenter(ICustomerEditView view, ICustomerService service)
    {
        _view = view;
        _service = service;
        _view.SaveRequested += OnSaveRequested;
        _view.CancelRequested += OnCancelRequested;
    }

    private async void OnSaveRequested(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_view.CustomerName))
        {
            _view.ShowError("姓名不能为空");
            return;
        }

        try
        {
            await _service.SaveAsync(_view.CustomerName, _view.Email);
            _view.CloseView(DialogResult.OK);
        }
        catch (Exception ex)
        {
            _view.ShowError(ex.Message);
        }
    }

    private void OnCancelRequested(object sender, EventArgs e) => _view.CloseView(DialogResult.Cancel);
}
```

**Presenter 里 `async void` 可以接受**——它就是事件处理器的延伸。但更严谨的做法是暴露 `Task` 方法并让 View 用 `async void` 包一层。

### 9.3 什么时候值得这么做

- **不值得**：小工具、原型、10 个窗体以内的一次性项目。直接写 Form 里更快。
- **值得**：有业务规则要测、逻辑超过 200 行、多人协作、长期维护。

**中间档**：不做完整 MVP，只把"纯计算/纯规则"抽成静态类或 service，Form 只负责显示和调用。这是性价比最高的折中。

---

## 10. 逻辑 bug 对照表

| 现象 | 原因 | 修法 |
|:---|:---|:---|
| 点两次按钮发两次请求 | 没禁用按钮 / 没防重入 | 进入时 `btn.Enabled = false`，`finally` 恢复 |
| 跨线程异常 `InvalidOperationException` | 在非 UI 线程碰控件 | `InvokeRequired` + `BeginInvoke`；或改用 `Progress<T>`/`await` |
| 关闭窗体后崩溃 `ObjectDisposedException` | 后台回调在窗体销毁后到达 | 回调开头判断 `IsDisposed \|\| Disposing` |
| 内存不释放 | 订阅了长生命周期对象的事件没退订 | `OnFormClosed` 里 `-=` |
| 定时器关闭后还在跑 | 只 `Stop()` 没 `Dispose()`，或用了 `System.Timers.Timer` | `Dispose()`；优先 `System.Windows.Forms.Timer` |
| 界面不刷新（列表增删后） | 绑了 `List<T>` 而非 `BindingList<T>` | 换 `BindingList<T>` |
| 保存时拿到的是旧值 | 单元格还在编辑中，未提交 | 读之前 `_bsCustomers.EndEdit()` |
| 事件处理器签名不匹配编译错 | 参数类型/个数与委托不符 | 对照 §3 速查表 |
| 设计器一打开就报错/卡死 | 构造函数里的逻辑在设计期执行了 | `LicenseManager.UsageMode` 守卫，或挪到 `OnLoad` |
| 点"取消"被校验拦住 | `Validating` 里 `e.Cancel = true` | 取消按钮设 `CausesValidation = false` |
| `async void` 异常直接崩进程 | 事件处理器没 catch | 事件处理器内必须 try/catch |
| 异步后界面卡住 | UI 线程上 `.Result` / `.Wait()` | 全链路 `await`，不要阻塞 UI 线程 |
| 窗体白屏几秒 | 构造函数里同步读库/拉网络 | 挪到 `OnLoad` + `async` |

---

## 11. 小结

1. **`InitializeComponent()` 第一，逻辑第二。**
2. **设计期守卫用 `LicenseManager.UsageMode`**，不是 `DesignMode`。
3. **事件签名和 Designer.cs 逐字对齐**，处理器名用 `控件名_事件名`。
4. **`async void` 只给事件处理器，且必须 try/catch。**
5. **从源头避免跨线程**（`await` / `Progress<T>` / `Forms.Timer`），万不得已才 `BeginInvoke`。
6. **`BindingList<T>` + `BindingSource`**，读数据前 `EndEdit()`。
7. **`OnFormClosed` 里清理**：取消 CTS、释放定时器、退订事件。
8. **逻辑抽到可测的类里**，Form 只做显示和转发。
