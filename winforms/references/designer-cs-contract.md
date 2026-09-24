# `*.Designer.cs` 与 `InitializeComponent()` 往返契约

本文件是 [SKILL.md](../SKILL.md) 的核心补充。目标只有一个：**你手写的 `InitializeComponent()` 必须和 Visual Studio 设计器自己生成的一模一样，或者说，必须能被设计器解析、渲染、并无损地重新序列化。**

---

## 1. 先建立正确的心智模型

很多人以为设计器是"解析你的代码然后显示"。**不是。**

真实流程是：

```
① 解析   设计器用 CodeDOM 解析器读 *.Designer.cs
         ↓ 解析失败 → 黄色错误页："设计器无法显示"
② 构建   从代码里的字段 + Controls.Add 调用重建控件树（组件图）
         ↓ 控件没被 Controls.Add → 它不在图里，设计器不显示、保存时还会被删掉
③ 渲染   把组件图画到设计画布上
④ 保存   设计器【重新生成】整个 InitializeComponent() 和字段声明，覆盖你的代码
```

第 ④ 步是全部痛苦的根源：**设计器不是"编辑"你的代码，是"重写"你的代码。**

由此推出两条最重要的结论：

- **设计器不认识的东西，保存时会静默消失。** 你写的 `foreach` 批量建控件，如果能打开设计器，保存一次就全没了——没有报错，没有提示。
- **设计器认识、但你没写的东西，保存时会被补上。** 所以不必纠结格式细节，格式最终由设计器统一。

所以你的目标不是"写得漂亮"，而是**写进组件图、且不触发解析失败**。

---

## 2. 硬性要求 vs. 建议约定

把力气花对地方。下面两类要分清。

### 2.1 硬性要求（违反 = 设计器坏掉或丢代码）

| # | 要求 | 违反后果 |
|:--|:---|:---|
| H1 | 类必须是 `partial`，且**窗体/控件类是本文件里的第一个类** | 设计器找错类，报错或串味 |
| H2 | 方法签名必须是 `private void InitializeComponent()`，无参无返回 | 设计器找不到入口，窗体空白 |
| H3 | 每个用到的控件**必须有对应的私有字段声明** | 解析报错，设计器打不开 |
| H4 | 每个控件**必须被 `Controls.Add` 加进某个容器**（最终连到窗体） | 控件不在组件图里，设计器不显示，保存后被删 |
| H5 | `InitializeComponent()` 内**只能出现设计器可序列化的语句** | 解析失败（打不开）或保存时静默丢失 |
| H6 | 整个项目**必须能编译通过** | 设计器加载的是设计期构建产物，编译错误 = 打不开 |
| H7 | `*.Designer.cs` 必须被 `csproj` 纳入编译（文件名/`DependentUpon` 正确） | 同上 |
| H8 | `InitializeComponent()` 内**不得有设计期副作用**（IO / DB / 网络 / 弹窗） | 设计器卡死、超时、或弹出对话框 |
| H9 | 逻辑代码**不得写在 `*.Designer.cs`** 里 | 下次保存被设计器抹掉 |

### 2.2 建议约定（不影响往返，但让 diff 稳定、风格统一）

| # | 约定 | 理由 |
|:--|:---|:---|
| C1 | 保留 `//` + `// 控件名` + `//` 注释块 | 设计器会重新生成它；保留可让 AI 改动与设计器改动 diff 对齐 |
| C2 | 保留 `#region Windows Form Designer generated code` | 同上；UserControl 用 `#region Component Designer generated code` |
| C3 | 控件字段声明放在 `#endregion` **之后** | 设计器就是这个位置 |
| C4 | 全文加 `this.` 前缀 | 设计器风格；避免与局部名冲突 |
| C5 | 属性值用全限定类型（`new System.Drawing.Point(3, 3)`） | 不依赖 `using`，跨文件安全 |
| C6 | 事件订阅写 `new System.EventHandler(this.M)` | 设计器规范形态 |
| C7 | `Name` 属性与字段名保持一致 | 设计器靠 `Name` 序列化 |
| C8 | 字段全部 `private`，不加 `readonly` / `static` | 设计器无法序列化 `readonly`/`static` 控件字段 |

> **注意 C1/C2/C3**：注释块和 region 本身不是解析硬要求，但没有它们，AI 生成的文件和设计器保存后的文件会长得不一样，之后每次人机交替编辑都会产生大片假 diff。保持一致是低成本高收益的。

### 2.3 首次保存的"归一化"是正常的

即使你完全照 §3 模板写，设计器**第一次保存时仍可能**重写一些纯格式细节，产生一次性 diff。已知的有：

| 你写的 | 设计器可能改成 |
|:---|:---|
| `new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize)` | `new System.Windows.Forms.RowStyle()` |
| 控件字段声明顺序 | 按内部顺序重排 |
| 属性赋值顺序 | 按序列化器顺序重排 |
| 注释块的缩进/空行 | 统一格式 |

**判断标准**：diff 里**只有格式和顺序变化、没有控件增删、没有属性值变化** → 正常，提交即可。**出现控件消失或属性值被重置** → 是 §4 的违规，回查。

（本文档统一使用 `new RowStyle(System.Windows.Forms.SizeType.AutoSize)` 这种显式写法——它更易读，且完全合法；接受首次保存的一次性归一化。）

---

## 3. 标准模板

> **关于下面范例里的 `Location` / `Size` 数值**：它们只是让设计器能正常渲染的**占位值**，布局引擎会在运行时按容器规则重新计算。
> **加控件时不要手工推算坐标**——用 `TableLayoutPanel` 的行列或 `Dock`/`Anchor` 表达位置（见 [layout-and-decomposition.md](./layout-and-decomposition.md)）。
> 范例里的数值已尽量自洽，但不必逐像素对齐；设计器首次保存时也会自己重算。

### 3.1 Form

```csharp
namespace Demo.Forms;

partial class MainForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
        this.lblTitle = new System.Windows.Forms.Label();
        this.txtName = new System.Windows.Forms.TextBox();
        this.rootLayout.SuspendLayout();
        this.SuspendLayout();
        //
        // rootLayout
        //
        this.rootLayout.ColumnCount = 2;
        this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
        this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
        this.rootLayout.Location = new System.Drawing.Point(0, 0);
        this.rootLayout.Name = "rootLayout";
        this.rootLayout.RowCount = 2;
        this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this.rootLayout.Padding = new System.Windows.Forms.Padding(12);
        this.rootLayout.Size = new System.Drawing.Size(484, 261);
        this.rootLayout.TabIndex = 0;
        //
        // lblTitle
        //
        this.lblTitle.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this.lblTitle.AutoSize = true;
        this.lblTitle.Location = new System.Drawing.Point(15, 18);
        this.lblTitle.Name = "lblTitle";
        this.lblTitle.Size = new System.Drawing.Size(43, 17);
        this.lblTitle.TabIndex = 0;
        this.lblTitle.Text = "名称";
        //
        // txtName
        //
        this.txtName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
        this.txtName.Location = new System.Drawing.Point(105, 15);
        this.txtName.Name = "txtName";
        this.txtName.Size = new System.Drawing.Size(364, 23);
        this.txtName.TabIndex = 1;
        //
        // rootLayout
        //
        this.rootLayout.Controls.Add(this.lblTitle, 0, 0);
        this.rootLayout.Controls.Add(this.txtName, 1, 0);
        //
        // MainForm
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(484, 261);
        this.Controls.Add(this.rootLayout);
        this.MinimumSize = new System.Drawing.Size(320, 200);
        this.Name = "MainForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.Text = "示例";
        this.rootLayout.ResumeLayout(false);
        this.rootLayout.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.TableLayoutPanel rootLayout;
    private System.Windows.Forms.Label lblTitle;
    private System.Windows.Forms.TextBox txtName;
}
```

### 3.2 UserControl

只有三处与 Form 不同，**别抄错**：

```csharp
    #region Component Designer generated code      // ← 不是 "Windows Form Designer"

    private void InitializeComponent()
    {
        // ...
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.Name = "CustomerEditorView";
        this.Size = new System.Drawing.Size(420, 240);   // ← UserControl 用 Size，没有 ClientSize
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion
```

`UserControl` **没有** `ClientSize`、`StartPosition`、`FormBorderStyle`、`Text`（有 `Text` 属性但不显示为标题栏）。写了不会编译错，但设计器保存时会删掉——属于噪音。

### 3.3 `InitializeComponent()` 内部顺序

严格按此顺序，这是设计器的生成顺序：

```csharp
private void InitializeComponent()
{
    // ① 非可视组件容器（有 ToolTip/ImageList/Timer 等才需要）
    this.components = new System.ComponentModel.Container();

    // ② 所有控件 new 出来（父容器也要 new）
    this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
    this.btnOk = new System.Windows.Forms.Button();

    // ③ SuspendLayout：子容器在前，this 最后
    this.rootLayout.SuspendLayout();
    this.SuspendLayout();

    // ④ 逐控件属性赋值，每个控件前加 //\n// 名字\n// 注释块
    // ...
    // 非可视组件（ToolTip 等）也在这里设置

    // ⑤ 组装：子容器收自己的子控件，this 最后收顶层容器
    this.rootLayout.Controls.Add(this.btnOk, 1, 1);
    this.Controls.Add(this.rootLayout);

    // ⑥ this 自身的属性（Text / ClientSize / Name / AutoScaleMode ...）
    this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
    this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
    this.ClientSize = new System.Drawing.Size(800, 450);
    this.Name = "MainForm";
    this.Text = "标题";

    // ⑦ ResumeLayout：子容器在前，this 最后；需要时补 PerformLayout
    this.rootLayout.ResumeLayout(false);
    this.rootLayout.PerformLayout();
    this.ResumeLayout(false);
    this.PerformLayout();
}
```

**`PerformLayout()` 什么时候要**：容器启用了 `AutoSize`、或尺寸由布局引擎推导时，设计器会补上。**规则**：如果你写了 `SuspendLayout()`，就成对写 `ResumeLayout(false)`；凡是 `AutoSize = true` 的容器，跟着补 `PerformLayout()`。多写一个 `PerformLayout()` 无害，漏写可能导致设计期尺寸不对。

**`SuspendLayout`/`ResumeLayout` 什么时候要**：有子控件时。空窗体不需要。

---

## 4. 禁止清单（`InitializeComponent()` 内）

### 4.1 绝对禁止

```csharp
// ❌ 循环 / 条件 —— CodeDOM 解析失败，设计器直接打不开
for (int i = 0; i < 10; i++)
{
    var btn = new Button();          // ❌ 局部变量：控件不在组件图里
    btn.Text = $"按钮 {i}";           // ❌ 字符串插值：设计器只存字面量
    btn.Click += (s, e) => { };      // ❌ lambda：无法序列化
    this.Controls.Add(btn);
}

// ❌ 对象初始化器：属性在往返中丢失
this.btnOk = new System.Windows.Forms.Button { Text = "确定", Dock = DockStyle.Bottom };

// ❌ 调用自定义方法：设计器看不到里面建了什么控件
BuildToolbar();
this.Controls.Add(CreateGrid());

// ❌ LINQ / nameof / 三元
this.Text = nameof(MainForm);
this.lbl.Text = list.First().Name;

// ❌ 设计期副作用：设计器一打开就执行
this.txtPath.Text = System.IO.File.ReadAllText("config.json");
this.dgv.DataSource = _repository.GetAll();     // 设计器里连不上库 → 报错
```

### 4.2 允许且设计器自己就会生成的

```csharp
this.components = new System.ComponentModel.Container();
this.btnOk = new System.Windows.Forms.Button();
this.btnOk.Text = "确定";
this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
this.rootLayout.Controls.Add(this.btnOk, 0, 1);
this.Controls.Add(this.rootLayout);
this.rootLayout.SuspendLayout();
this.SuspendLayout();
this.rootLayout.ResumeLayout(false);
this.rootLayout.PerformLayout();
this.ResumeLayout(false);
this.PerformLayout();
((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
this.toolTip1.SetToolTip(this.btnOk, "保存");
this.txtName.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.customerBindingSource, "Name", true));
this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
resources.ApplyResources(this.lblTitle, "lblTitle");   // 本地化模式
```

### 4.3 想批量建控件怎么办

这是 AI 最容易犯的错，也是需求上最常见的场景（比如"生成 20 个按钮"）。**正确做法：把动态部分挪出设计器。**

```csharp
// ✅ Designer.cs：只声明容器，保持纯净
this.pnlButtons = new System.Windows.Forms.FlowLayoutPanel();
this.pnlButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
this.pnlButtons.Name = "pnlButtons";

// ✅ MainForm.cs：运行时填充。设计器里看到的是空容器，但完全合法且可编辑
public MainForm()
{
    InitializeComponent();
    BuildDynamicButtons();
}

private void BuildDynamicButtons()
{
    for (int i = 0; i < 20; i++)
    {
        var btn = new Button { Text = $"按钮 {i}", AutoSize = true };
        btn.Click += DynamicButton_Click;
        pnlButtons.Controls.Add(btn);
    }
}
```

**判据**：控件数量在设计期固定 → 写进 Designer.cs；数量/内容运行期才知道 → 运行时生成，Designer.cs 只留容器。

---

## 5. 完整范例：带事件、容器、非可视组件

```csharp
// ============ CustomerEditForm.Designer.cs ============
namespace Demo.Forms;

partial class CustomerEditForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
        this.lblName = new System.Windows.Forms.Label();
        this.txtName = new System.Windows.Forms.TextBox();
        this.lblEmail = new System.Windows.Forms.Label();
        this.txtEmail = new System.Windows.Forms.TextBox();
        this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
        this.flowButtons = new System.Windows.Forms.FlowLayoutPanel();
        this.btnSave = new System.Windows.Forms.Button();
        this.btnCancel = new System.Windows.Forms.Button();
        this.rootLayout.SuspendLayout();
        this.flowButtons.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
        this.SuspendLayout();
        //
        // rootLayout
        //
        this.rootLayout.ColumnCount = 2;
        this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
        this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
        this.rootLayout.Location = new System.Drawing.Point(0, 0);
        this.rootLayout.Name = "rootLayout";
        this.rootLayout.Padding = new System.Windows.Forms.Padding(12);
        this.rootLayout.RowCount = 3;
        this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this.rootLayout.Size = new System.Drawing.Size(464, 181);
        this.rootLayout.TabIndex = 0;
        //
        // lblName
        //
        this.lblName.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this.lblName.AutoSize = true;
        this.lblName.Location = new System.Drawing.Point(15, 18);
        this.lblName.Name = "lblName";
        this.lblName.Size = new System.Drawing.Size(43, 17);
        this.lblName.TabIndex = 0;
        this.lblName.Text = "姓名";
        //
        // txtName
        //
        this.txtName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
        this.txtName.Location = new System.Drawing.Point(98, 15);
        this.txtName.Name = "txtName";
        this.txtName.Size = new System.Drawing.Size(351, 23);
        this.txtName.TabIndex = 1;
        //
        // lblEmail
        //
        this.lblEmail.Anchor = System.Windows.Forms.AnchorStyles.Left;
        this.lblEmail.AutoSize = true;
        this.lblEmail.Location = new System.Drawing.Point(15, 50);
        this.lblEmail.Name = "lblEmail";
        this.lblEmail.Size = new System.Drawing.Size(43, 17);
        this.lblEmail.TabIndex = 2;
        this.lblEmail.Text = "邮箱";
        //
        // txtEmail
        //
        this.txtEmail.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
        this.txtEmail.Location = new System.Drawing.Point(98, 47);
        this.txtEmail.Name = "txtEmail";
        this.txtEmail.Size = new System.Drawing.Size(351, 23);
        this.txtEmail.TabIndex = 3;
        //
        // errorProvider
        //
        this.errorProvider.ContainerControl = this;
        //
        // flowButtons
        //
        this.flowButtons.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.flowButtons.AutoSize = true;
        this.flowButtons.Controls.Add(this.btnSave);
        this.flowButtons.Controls.Add(this.btnCancel);
        this.flowButtons.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
        this.flowButtons.Location = new System.Drawing.Point(278, 84);
        this.flowButtons.Name = "flowButtons";
        this.flowButtons.Size = new System.Drawing.Size(171, 31);
        this.flowButtons.TabIndex = 4;
        this.flowButtons.WrapContents = false;
        //
        // btnSave
        //
        this.btnSave.AutoSize = true;
        this.btnSave.Location = new System.Drawing.Point(3, 3);
        this.btnSave.Name = "btnSave";
        this.btnSave.Size = new System.Drawing.Size(75, 25);
        this.btnSave.TabIndex = 0;
        this.btnSave.Text = "保存";
        this.btnSave.UseVisualStyleBackColor = true;
        this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
        //
        // btnCancel
        //
        this.btnCancel.AutoSize = true;
        this.btnCancel.Location = new System.Drawing.Point(84, 3);
        this.btnCancel.Name = "btnCancel";
        this.btnCancel.Size = new System.Drawing.Size(75, 25);
        this.btnCancel.TabIndex = 1;
        this.btnCancel.Text = "取消";
        this.btnCancel.UseVisualStyleBackColor = true;
        this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
        //
        // rootLayout
        //
        this.rootLayout.Controls.Add(this.lblName, 0, 0);
        this.rootLayout.Controls.Add(this.txtName, 1, 0);
        this.rootLayout.Controls.Add(this.lblEmail, 0, 1);
        this.rootLayout.Controls.Add(this.txtEmail, 1, 1);
        this.rootLayout.Controls.Add(this.flowButtons, 0, 2);
        this.rootLayout.SetColumnSpan(this.flowButtons, 2);
        //
        // CustomerEditForm
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(464, 181);
        this.Controls.Add(this.rootLayout);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "CustomerEditForm";
        this.ShowInTaskbar = false;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "编辑客户";
        this.rootLayout.ResumeLayout(false);
        this.rootLayout.PerformLayout();
        this.flowButtons.ResumeLayout(false);
        this.flowButtons.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.TableLayoutPanel rootLayout;
    private System.Windows.Forms.Label lblName;
    private System.Windows.Forms.TextBox txtName;
    private System.Windows.Forms.Label lblEmail;
    private System.Windows.Forms.TextBox txtEmail;
    private System.Windows.Forms.ErrorProvider errorProvider;
    private System.Windows.Forms.FlowLayoutPanel flowButtons;
    private System.Windows.Forms.Button btnSave;
    private System.Windows.Forms.Button btnCancel;
}
```

```csharp
// ============ CustomerEditForm.cs ============
using System;
using System.Windows.Forms;

namespace Demo.Forms;

public partial class CustomerEditForm : Form
{
    private readonly Customer _customer;

    public CustomerEditForm(Customer customer)
    {
        _customer = customer ?? throw new ArgumentNullException(nameof(customer));
        InitializeComponent();          // 必须：第一件事（或尽早）
        LoadCustomer();
    }

    private void LoadCustomer()
    {
        txtName.Text = _customer.Name;
        txtEmail.Text = _customer.Email;
    }

    private void btnSave_Click(object sender, EventArgs e)
    {
        if (!ValidateInput())
        {
            return;
        }

        _customer.Name = txtName.Text.Trim();
        _customer.Email = txtEmail.Text.Trim();
        DialogResult = DialogResult.OK;
        Close();
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    private bool ValidateInput()
    {
        errorProvider.Clear();
        var ok = true;

        if (string.IsNullOrWhiteSpace(txtName.Text))
        {
            errorProvider.SetError(txtName, "姓名不能为空");
            ok = false;
        }

        if (!string.IsNullOrWhiteSpace(txtEmail.Text) && !txtEmail.Text.Contains('@'))
        {
            errorProvider.SetError(txtEmail, "邮箱格式不正确");
            ok = false;
        }

        return ok;
    }
}
```

**注意 `InitializeComponent()` 的位置**：在构造函数里，且**赋值 `_customer` 之后**——因为 `LoadCustomer()` 需要它。但 `InitializeComponent()` 必须在任何触碰控件的代码之前。如果构造函数里有多个步骤，`InitializeComponent()` 永远排在最前面（字段赋值不算触碰控件）。

---

## 6. `.resx` 与资源

**原则：不要让 AI 手写 `.resx` 里的二进制资源。**

`.resx` 里图片/图标是 base64 编码的二进制 blob，手写几乎必错，且损坏后设计器会直接报错、整个窗体打不开。

| 场景 | 做法 |
|:---|:---|
| 窗体图标、`PictureBox.Image`、`BackgroundImage` | **让用户在设计器里拖图片**，或提供文件路径让用户自己设 |
| 运行时按条件切换图片 | 图片放项目 `Resources/`，用代码赋值：`pictureBox1.Image = Image.FromFile(path);` 或 `Properties.Resources.Logo` |
| 图标字体/矢量 | 用 `Graphics` 绘制或在 `OnPaint` 里画，别塞 `.resx` |
| 本地化字符串 | 可让设计器生成 `resources.ApplyResources(...)` 模式，但**必须由设计器生成**，不要手写 |

如果 AI 确实需要引资源，**在 Designer.cs 里写一行注释标明 TODO，让用户去设计器设置**：

```csharp
//
// picLogo
//
this.picLogo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
this.picLogo.Location = new System.Drawing.Point(12, 12);
this.picLogo.Name = "picLogo";
this.picLogo.Size = new System.Drawing.Size(64, 64);
this.picLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
this.picLogo.TabIndex = 0;
this.picLogo.TabStop = false;
// TODO(人): 请在设计器中为 picLogo 选择 Image（避免手写 .resx）
```

**本地化模式（`Localizable = true`）**：一旦开启，设计器改用 `ComponentResourceManager` + `resources.ApplyResources(this.ctrl, "ctrlName")` 序列化，所有属性都走 `.resx`。**不要中途手工切换**，整窗体重写成本高。若项目已用该模式，AI 生成的 Designer.cs 也必须沿用 `resources.ApplyResources(...)` 风格，不能再直接赋值。

---

## 7. 故障对照表

| 现象 | 原因 | 修法 |
|:---|:---|:---|
| "设计器无法显示此文件" / 黄色错误页 | 解析失败：循环、lambda、局部变量、语法错误、编译不过 | 按 §4 清理；先确保 `dotnet build` 通过 |
| 设计器能打开，但保存后某些控件消失 | 那些控件不在组件图里（没 `Controls.Add`），或是循环/lambda 建的 | 改成平铺的字段 + `Controls.Add` |
| 设计器能打开，但保存后属性被重置 | 属性是代码里算出来的（插值/三元/方法调用） | 改成字面量赋值 |
| 窗体运行时空白 | `InitializeComponent()` 没被调用，或调用在控件访问之后 | 构造函数里第一件事就调用 |
| 控件运行时看不见，设计器里也看不见 | 忘了 `Controls.Add` | 补上；注意加进**正确的父容器** |
| 控件重叠 | 加进了错误的父容器，或容器没用 `Dock`/行列 | 检查 `Controls.Add` 的接收者 |
| 打开设计器时弹出对话框 / 卡死 | `InitializeComponent()` 里有 IO/DB/网络/`MessageBox` | 全部挪到 `OnLoad` 或构造函数后段 |
| 设计器里控件位置和运行时不一致 | `AutoScaleDimensions` 与项目字体不匹配 | 抄项目内已有窗体的值 |
| 改完 Designer.cs 后设计器覆盖了我的修改 | 改的是设计器管辖的属性 | 属于预期行为；要运行时改就在 `*.cs` 里改 |
| 编译警告 CS8625 / CS8618 | 项目开了 `<Nullable>enable</Nullable>` | 见 §7.1，别和设计器较劲 |
| 打开设计器提示"基类无法实例化" | 窗体基类构造函数有参数，或抽象 | 设计期需要无参构造；用 `DesignMode` 判断或设计期构造函数 |

### 7.1 `<Nullable>` 与 Designer.cs（实测结论）

WinForms 设计器生成的代码是**按"未开启 Nullable"写的**。项目一旦开启 `<Nullable>enable</Nullable>`，就会冒出一批警告。下面是实测结果（`net8.0-windows`，`<Nullable>enable</Nullable>`）：

| 设计器风格写法 | 警告 | 说明 |
|:---|:---|:---|
| `private System.ComponentModel.IContainer components = null;` | **CS8625** | 无法将 null 字面量转换为非 null 的引用类型 |
| `private System.Windows.Forms.Button btnOk;` | **CS8618** | 退出构造函数时不可为 null 的字段必须包含非 null 值 |
| `public event EventHandler Saved;` | **CS8618** | 不可为 null 的事件必须包含非 null 值 |
| `private void Btn_Click(object sender, EventArgs e)`（订阅 `EventHandler`） | **CS8622** | 参数 `sender` 的为 Null 性与目标委托不匹配 |

而**未开启** Nullable 的项目里，反过来写 `EventHandler?` / `object?` 会报 **CS8632**。

**推荐处理方式**：

1. **`*.Designer.cs` 文件头加一行 `#nullable disable`** —— 最省事，且把"设计器管辖区域"和"你的逻辑"隔离开：
   ```csharp
   #nullable disable
   namespace Demo.Forms;

   partial class MainForm
   {
       // ... 设计器生成的代码，不受 Nullable 影响
   }
   ```
2. 或者在 `csproj` 里整体放行：`<NoWarn>$(NoWarn);CS8618;CS8625</NoWarn>`。
3. **逻辑文件**（`*.cs`）保持 Nullable 开启，按 §3 的说明写 `object? sender`、`event EventHandler?`。

**绝对不要**为了消警告把控件字段改成可空类型：

```csharp
private System.Windows.Forms.Button? btnOk;   // ❌ 设计器序列化会出问题
private System.Windows.Forms.Button btnOk;    // ✅ 保持设计器生成的写法
```

**判断原则**：`*.Designer.cs` 里的代码是**设计器的地盘**，不要为了迎合 Nullable 去改它；要么在文件级关掉分析，要么在项目级放行警告。

---

## 8. 怎么验证往返安全

**唯一可信的验证方法是让设计器真的保存一次。** 编译通过 ≠ 往返安全。

推荐流程：

1. `dotnet build` —— 必须零错误（H6）。
2. VS 里**双击 `*.cs`** 打开窗体 → "查看设计器" —— 必须正常渲染，无黄色错误条。
3. 在画布上**随便拖动一个控件**，然后 `Ctrl+S` 保存。
4. **看 diff**：
   - 只有你拖动那一下的坐标变化 → ✅ 往返安全。
   - 出现大段删除/重排 → ⚠️ 你有代码没被设计器接受，对照 §7 排查。
   - 控件消失 → ❌ 它不在组件图里，回 §4 检查 `Controls.Add`。
5. 把第 3 步的拖动**撤销掉**（`Ctrl+Z` 再保存），恢复原始状态。

**没有 VS 时**：至少保证 `dotnet build` 通过，并在交付说明里明确告诉用户"未经过设计器往返验证，请在 VS 中打开设计器确认"——**不要谎称已验证**。

---

## 9. 给 AI 的最后提醒

- 你改 Designer.cs 时，**默认人之后会打开设计器并保存**。任何"设计器不认识"的写法都会在那时消失。所以：**要么完全守规矩，要么别碰 Designer.cs。**
- 加控件是**三件套**，缺一不可：① 字段声明 ② `new` + 属性赋值 ③ `Controls.Add` 到正确的父容器。漏第 ③ 项是最常见的静默 bug。
- 加事件是**两件套**：① Designer.cs 里 `+= new System.EventHandler(this.X)` ② `*.cs` 里 `private void X(object sender, EventArgs e)`。签名不一致 = 编译错。
- 不确定某个属性设计器是否支持？**用最朴素的写法**（字面量赋值），别用"聪明"的写法。
