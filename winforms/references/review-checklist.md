# 交付前自检清单与常见翻车案例

[SKILL.md](../SKILL.md) §5 的完整版。**每次交付 WinForms 代码前，逐项过一遍。** 这份清单是为了在用户打开 VS 之前就发现问题——WinForms 的很多错误不报编译错，只在设计器或运行时才暴露。

---

## 0. 怎么用

1. 先跑 §1 的**自动化扫描**（能机械查出来的别用眼睛看）。
2. 再过 §2~§6 的逐项清单。
3. 对照 §7 的翻车案例确认没踩坑。
4. 最后按 §8 的模板如实说明验证状态。

---

## 1. 自动化扫描（先跑这个）

在项目根目录跑，能捞出绝大部分机械性错误。

```bash
# ① Designer.cs 里出现循环/条件/lambda/插值 → 往返必坏
rg -n --glob "*.Designer.cs" "for\s*\(|foreach\s*\(|while\s*\(|\bif\s*\(|\bswitch\s*\(|=>|\$\""

# ② Designer.cs 里出现局部变量声明 → 控件进不了组件图
rg -n --glob "*.Designer.cs" "^\s*(var|string|int|bool|Button|TextBox|Label)\s+\w+\s*="

# ③ Designer.cs 里出现对象初始化器 → 属性会丢
rg -n --glob "*.Designer.cs" "new\s+[\w\.]+\s*\{"

# ④ Designer.cs 里出现 IO/DB/网络 → 设计期会执行
rg -n --glob "*.Designer.cs" "File\.|Directory\.|SqlConnection|HttpClient|MessageBox|Environment\.Exit"

# ⑤ Designer.cs 里出现自定义方法调用（白名单之外）
rg -n --glob "*.Designer.cs" "this\.\w+\("
#    白名单：SuspendLayout / ResumeLayout / PerformLayout / Controls.Add / Controls.AddRange
#           BeginInit / EndInit / SetToolTip / SetColumnSpan / SetRowSpan / SetError

# ⑥ 控件字段是否都有 Controls.Add（人工比对更可靠，见 §3）
```

**扫描有命中 ≠ 一定错**（比如 `Controls.AddRange(new DataGridViewColumn[] { ... })` 是设计器自己的写法），但**每个命中都必须人工确认一遍**。

---

## 2. 结构检查

- [ ] `InitializeComponent()` 位于 `*.Designer.cs`，**不在** `*.cs`。
- [ ] 签名是 `private void InitializeComponent()`——`private`、`void`、无参。
- [ ] 类声明是 `partial class`。
- [ ] `*.Designer.cs` 里的**第一个类**就是窗体/控件类（没有别的类排在它前面）。
- [ ] `components` 字段存在：`private System.ComponentModel.IContainer components = null;`
- [ ] `Dispose(bool disposing)` 重写存在，且调用 `base.Dispose(disposing)`。
- [ ] `#region` 名字正确：Form 用 `Windows Form Designer generated code`，UserControl 用 `Component Designer generated code`。
- [ ] `#region` / `#endregion` 成对。
- [ ] 控件字段声明在 `#endregion` **之后**。
- [ ] 所有控件字段都是 `private`，**没有** `readonly` / `static`。
- [ ] 控件字段类型用全限定名（`System.Windows.Forms.Button`）。
- [ ] `*.cs` 里的构造函数**第一件事**就是 `InitializeComponent()`（字段赋值可在其之前）。
- [ ] `InitializeComponent()` **只被调用一次**。
- [ ] `csproj` 里 `*.Designer.cs` 被正确嵌套（文件名规范时自动；否则需显式 `<DependentUpon>`）。

---

## 3. 一致性检查（AI 最容易错的地方）

这是"三件套"核对。**每一个控件都必须同时满足三条。**

| 控件 | ① 有字段声明 | ② 有 `new` + 属性赋值 | ③ 有 `Controls.Add` 到正确父容器 |
|:---|:---:|:---:|:---:|
| `rootLayout` | ☐ | ☐ | ☐ |
| `btnSave` | ☐ | ☐ | ☐ |
| ... | ☐ | ☐ | ☐ |

**逐项确认**：

- [ ] 每个在 `InitializeComponent()` 里出现的 `this.xxx`，都能在字段声明区找到 `xxx`。
- [ ] 每个字段都被 `new` 过（否则运行时 `NullReferenceException`）。
- [ ] **每个控件都被加进了某个容器的 `Controls`**，且这个容器最终连到 `this`。漏一个 = 控件不显示 + 保存时被设计器删掉。
- [ ] `Controls.Add` 的**父容器正确**——加错父容器会导致重叠或位置错乱。
- [ ] 每个 `Controls.Add(child, col, row)` 的 `col`/`row` 在 `ColumnCount`/`RowCount` 范围内。
- [ ] `RowCount` / `ColumnCount` 与 `RowStyles` / `ColumnStyles` 的条目数一致。
- [ ] `SetColumnSpan` / `SetRowSpan` 写在对应 `Controls.Add` **之后**。
- [ ] Designer.cs 里订阅的**每个**事件，`*.cs` 里都有签名匹配的方法。
- [ ] `*.cs` 里引用的**每个**控件名都真实存在（拼写一致，含大小写）。
- [ ] 事件处理器方法名与 `控件名_事件名` 约定一致。
- [ ] `Name` 属性与字段名一致。

---

## 4. 往返安全检查

- [ ] `InitializeComponent()` 内**无** `for` / `foreach` / `while` / `if` / `switch` / `?:`。
- [ ] **无**局部变量，全部用 `this.` 字段。
- [ ] **无** lambda / 匿名委托，事件用 `new System.EventHandler(this.M)` 或方法组。
- [ ] **无**对象初始化器 `new X { ... }`。
- [ ] **无** LINQ / `nameof()` / 字符串插值。
- [ ] **无**自定义方法调用（`BuildUi()`、`CreateGrid()` 之类）。
- [ ] **无**设计期副作用（`File.*`、`Directory.*`、DB、网络、`MessageBox`、`Environment.Exit`）。
- [ ] **无**业务逻辑（校验、计算、状态判断）。
- [ ] `AutoScaleDimensions` 与项目字体匹配（抄项目内已有窗体的值）。
- [ ] `AutoScaleMode = AutoScaleMode.Font`。
- [ ] 需要动态生成的控件，改成了"Designer 声明容器 + `*.cs` 运行时填充"。
- [ ] **项目能 `dotnet build` 通过**（H6，编译不过设计器一定打不开）。

---

## 5. 布局检查

- [ ] 主要布局由容器表达，**不是**一堆硬编码 `Location`。
- [ ] 没有为了对齐而手工计算坐标。
- [ ] 拉大窗口时控件排布合理（`Anchor` / `Dock` / `Percent` 生效）。
- [ ] 边缘停靠的面板没有依赖 `Controls.Add` 顺序（**两个以上边缘停靠就该改用 `TableLayoutPanel`**）。
- [ ] 同一控件上没有同时设 `Dock` 和 `Anchor`。
- [ ] 间距用 `Margin` / `Padding`，**没有**用空 `Label` / 空 `Panel` 占位。
- [ ] `AutoSize = true` 的控件没有同时手设冲突的 `Size`。
- [ ] `TabIndex` 顺序符合阅读顺序。
- [ ] 控件数 > 15~20 的窗体已按 R5 拆成 `UserControl`。
- [ ] 每个 `UserControl` 都有 `public` 无参构造函数（否则设计器打不开）。
- [ ] `UserControl` 没有暴露 `public` 控件字段，而是暴露属性 + 事件。
- [ ] 控件命名有意义，**没有** `button1` / `label2` / `textBox3`。
- [ ] 用 `DataGridView` 时设了 `AutoGenerateColumns = false`。

---

## 6. 逻辑检查

- [ ] 事件处理器签名与委托匹配（对照 [logic-and-events.md](./logic-and-events.md) §3）。
- [ ] 所有 `async void` **只**出现在事件处理器 / `OnLoad` 重写里。
- [ ] 每个 `async void` 内部都有 `try/catch`。
- [ ] UI 代码里**没有** `ConfigureAwait(false)`。
- [ ] 没有 `.Result` / `.Wait()` / `.GetAwaiter().GetResult()` 阻塞 UI 线程。
- [ ] 异步操作有防重入（禁用触发按钮）。
- [ ] 耗时初始化在 `OnLoad`，**不在**构造函数。
- [ ] 构造函数里的逻辑有设计期守卫（`LicenseManager.UsageMode == LicenseUsageMode.Designtime`）。
- [ ] 跨线程更新 UI 走了 `BeginInvoke` 或 `Progress<T>`（不是直接改控件）。
- [ ] 跨线程回调开头有 `IsDisposed || Disposing` 守卫。
- [ ] 数据绑定用 `BindingList<T>` 而非 `List<T>`。
- [ ] 读取绑定数据前调用了 `EndEdit()`。
- [ ] `ErrorProvider.SetError` 清除时传 `string.Empty`（不是 `null`）。
- [ ] 会被 `Validating` 拦住的按钮设了 `CausesValidation = false`。
- [ ] `OnFormClosed` 里：取消并释放 `CancellationTokenSource`、释放自建定时器、退订外部事件。
- [ ] `Program.cs` 里有全局异常兜底（`Application.ThreadException` 等）。

---

## 7. 常见 AI 翻车案例

### 7.1 忘了 `Controls.Add`（最高频）

```csharp
// ❌ 控件建了、属性设了、就是没加进容器 → 运行时看不见，保存时被设计器删掉
this.btnSave = new System.Windows.Forms.Button();
this.btnSave.Text = "保存";
this.btnSave.Location = new System.Drawing.Point(12, 12);
// ...然后就没了
```

```csharp
// ✅ 三件套齐全
this.rootLayout.Controls.Add(this.btnSave, 0, 0);
this.Controls.Add(this.rootLayout);
```

### 7.2 用循环"优雅地"批量建控件

```csharp
// ❌ 设计器直接打不开（CodeDOM 解析失败）
string[] names = { "姓名", "邮箱", "电话" };
for (int i = 0; i < names.Length; i++)
{
    this.Controls.Add(new Label { Text = names[i], Top = i * 30 });
}
```

```csharp
// ✅ Designer.cs 平铺；数量固定就逐个写
this.lblName = new System.Windows.Forms.Label();
this.lblName.Text = "姓名";
// ...
this.lblEmail = new System.Windows.Forms.Label();
this.lblEmail.Text = "邮箱";
// ...

// ✅ 或者：Designer 只声明容器，运行时填充
this.flowFields = new System.Windows.Forms.FlowLayoutPanel();
this.flowFields.Dock = System.Windows.Forms.DockStyle.Fill;
this.flowFields.Name = "flowFields";
// .cs 里：
// foreach (var name in names) { flowFields.Controls.Add(new Label { Text = name, AutoSize = true }); }
```

### 7.3 lambda 订阅事件

```csharp
// ❌ 无法序列化；设计器保存后事件丢失
this.btnSave.Click += (s, e) => Save();
```

```csharp
// ✅ Designer.cs
this.btnSave.Click += new System.EventHandler(this.btnSave_Click);

// ✅ .cs
private void btnSave_Click(object sender, EventArgs e) => Save();
```

### 7.4 对象初始化器

```csharp
// ❌ 属性在往返中丢失，设计器里变成默认值
this.txtName = new System.Windows.Forms.TextBox { Location = new System.Drawing.Point(80, 12), Width = 200 };
```

```csharp
// ✅ 分开写
this.txtName = new System.Windows.Forms.TextBox();
this.txtName.Location = new System.Drawing.Point(80, 12);
this.txtName.Name = "txtName";
this.txtName.Size = new System.Drawing.Size(200, 23);
```

### 7.5 在 `InitializeComponent()` 里连数据库

```csharp
// ❌ 设计器一打开就执行 → 报错、卡死、或弹窗
this.dgvCustomers.DataSource = _repository.GetAllCustomers();
```

```csharp
// ✅ Designer.cs 只声明控件
this.dgvCustomers.AutoGenerateColumns = false;
this.dgvCustomers.Dock = System.Windows.Forms.DockStyle.Fill;
this.dgvCustomers.Name = "dgvCustomers";

// ✅ .cs 运行时加载
protected override async void OnLoad(EventArgs e)
{
    base.OnLoad(e);
    try { dgvCustomers.DataSource = await _repository.GetAllCustomersAsync(); }
    catch (Exception ex) { MessageBox.Show(this, ex.Message, "加载失败"); }
}
```

### 7.6 构造函数里判断设计期用了 `DesignMode`

```csharp
// ❌ DesignMode 在构造函数里恒为 false，守卫失效，设计器照样执行逻辑
public MainForm()
{
    InitializeComponent();
    if (!DesignMode) { LoadData(); }   // 设计器里 LoadData() 仍然会跑
}
```

```csharp
// ✅ 用 LicenseManager
public MainForm()
{
    InitializeComponent();
    if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) { return; }
    LoadData();
}

// ✅ 或者干脆挪到 OnLoad（设计器不执行 OnLoad）
protected override void OnLoad(EventArgs e) { base.OnLoad(e); LoadData(); }
```

### 7.7 在 `*.Designer.cs` 里写业务逻辑

```csharp
// ❌ 下次设计器保存，这段代码被整段抹掉
private void InitializeComponent()
{
    // ...
    this.btnSave.Enabled = _user.Role == "Admin";   // 逻辑！会被抹掉
}
```

```csharp
// ✅ Designer.cs 保持纯净
this.btnSave.Enabled = true;
this.btnSave.Name = "btnSave";
this.btnSave.Text = "保存";

// ✅ .cs 里按权限控制
private void ApplyPermissions() => btnSave.Enabled = _user.Role == "Admin";
```

### 7.8 事件签名不匹配

```csharp
// Designer.cs
this.txtName.Validating += new System.ComponentModel.CancelEventHandler(this.txtName_Validating);

// ❌ 签名错：EventArgs 而非 CancelEventArgs → 编译错
private void txtName_Validating(object sender, EventArgs e) { }

// ✅
private void txtName_Validating(object sender, CancelEventArgs e) { }
```

### 7.9 手工算坐标导致重叠

```csharp
// ❌ 算错一个数字就是重叠；加一行要改下面所有 Y
this.lblA.Location = new System.Drawing.Point(12, 12);
this.lblB.Location = new System.Drawing.Point(12, 39);
this.lblC.Location = new System.Drawing.Point(12, 66);
```

```csharp
// ✅ 交给 TableLayoutPanel，位置由行列决定
this.rootLayout.Controls.Add(this.lblA, 0, 0);
this.rootLayout.Controls.Add(this.lblB, 0, 1);
this.rootLayout.Controls.Add(this.lblC, 0, 2);
```

### 7.10 用空 Label 撑间距

```csharp
// ❌ 维护灾难：没人知道这个空标签干什么用的
this.lblSpacer = new System.Windows.Forms.Label();
this.lblSpacer.Location = new System.Drawing.Point(12, 90);
this.lblSpacer.Name = "lblSpacer";
this.lblSpacer.Size = new System.Drawing.Size(100, 23);
```

```csharp
// ✅ 用 Margin
this.btnSave.Margin = new System.Windows.Forms.Padding(3, 12, 3, 3);
```

### 7.11 一个 Form 塞 60 个控件

```csharp
// ❌ 一个 InitializeComponent() 一千行，没人敢改
```

```csharp
// ✅ 拆成 UserControl：MainForm 退化成布局壳
// MainForm.Designer.cs 里只剩：
this.customerListView = new Demo.Views.CustomerListView();
this.customerListView.Dock = System.Windows.Forms.DockStyle.Fill;
this.rootLayout.Controls.Add(this.customerListView, 0, 1);
```

### 7.12 声明自己"已验证设计器往返"

**这是最严重的问题。** 没有真的在 VS 里打开设计器并保存过，就**不能**声称往返安全。编译通过只证明 H6，不证明 H1~H5。

```csharp
// ❌ 不要说
// "已确保设计器兼容，可正常往返编辑。"

// ✅ 要说
// "已通过 dotnet build；Designer.cs 按往返契约编写（无循环/lambda/局部变量，
//  控件三件套齐全）。我无法在此环境启动 VS 设计器，请你在 VS 中双击窗体
//  查看设计器确认，并随手拖动一个控件保存后检查 diff。"
```

---

## 8. 交付说明模板

交付 WinForms 改动时，按这个模板说明，**如实区分"已验证"和"未验证"**。

```markdown
## 改动
- `Forms/MainForm.Designer.cs`：新增 TableLayoutPanel 根布局 + 工具栏 + 状态栏
- `Forms/MainForm.cs`：新增 btnRefresh_Click、异步加载、OnFormClosed 清理
- `Views/CustomerListView.Designer.cs` / `.cs`：新增用户控件（拆出客户列表区块）

## 模式
模式 B（AI 生成 Designer.cs）+ R5 拆分（客户列表拆为 UserControl）

## 已验证
- [x] `dotnet build` 通过
- [x] Designer.cs 无循环/lambda/局部变量/对象初始化器/插值（已 rg 扫描）
- [x] 每个控件三件套齐全（字段声明 + new + Controls.Add）
- [x] 事件订阅与处理器签名一一对应

## 未验证（需你在 VS 中确认）
- [ ] 设计器往返：请双击 MainForm 查看设计器，拖动一个控件后 Ctrl+S，检查 diff
- [ ] DPI 缩放表现（100% / 150%）
- [ ] 实际数据下的布局（长文本是否截断）

## 说明
- `AutoScaleDimensions` 抄自项目内 `CustomerEditForm`（7F, 15F），与默认字体一致
- 未改动任何现有 Designer.cs 的既有内容，只做追加
```

**关键**：把"未验证"项明确列出来。用户拿着这份清单，两分钟就能确认完；藏着不说，用户可能几天后才发现设计器打不开。

---

## 9. 一句话总结

> 交付前问自己三个问题：
> 1. **设计器能打开它吗？**（无循环/lambda/局部变量/副作用）
> 2. **每个控件都进组件图了吗？**（三件套齐全）
> 3. **我声称的"已验证"，是真的验证过吗？**
