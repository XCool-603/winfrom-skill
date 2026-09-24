---
name: winforms
description: >-
  WinForms 桌面应用开发规范技能，强制把界面代码写进 *.Designer.cs 的 InitializeComponent()，
  保证 Visual Studio 设计器可往返编辑（round-trip），而不是把控件堆在构造函数里。
  覆盖：InitializeComponent 生成契约、往返安全红线、TableLayoutPanel/FlowLayoutPanel/Dock/Anchor 布局、
  UserControl 拆分、事件与 async 逻辑、数据绑定、DPI 适配、跨线程 Invoke、Form 生命周期与资源释放。
  Use this skill when building or modifying Windows Forms (WinForms) UI, generating or reviewing
  InitializeComponent() and *.Designer.cs files, laying out a form with TableLayoutPanel/Dock/Anchor,
  splitting a form into UserControls, wiring event handlers, or checking whether generated UI code
  survives the Visual Studio designer round-trip.
---

# WinForms 开发规范（Designer.cs 优先）

本技能的核心是一条硬性约束：**界面代码必须落在 `*.Designer.cs` 的 `InitializeComponent()` 里，且必须能被 Visual Studio 设计器往返打开与保存。**

这条约束不是为了"好看"，而是为了**可维护性**：只有 Designer.cs 保持纯净，人才能随时打开设计器拖控件、看效果、改布局，AI 才能安全地只改逻辑。一旦把控件塞进构造函数，设计器就打不开这个窗体了，项目从此只能靠读代码猜界面——这是 AI 辅助 WinForms 开发最常见的长期债务。

---

## 0. 五条铁律

违反任意一条，产出即视为不合格。

| # | 铁律 | 说明 |
|:--|:---|:---|
| **R1** | **界面代码只写在 `*.Designer.cs` 的 `InitializeComponent()` 内** | 控件字段声明、`new` 控件、属性赋值、`Controls.Add`、事件订阅，全部在此。不在构造函数、不在 `OnLoad`、不在字段初始化器里建控件。 |
| **R2** | **Designer.cs 必须设计器往返安全** | 只用设计器认得的构造（见 §3 红线）。不用 lambda、循环、条件、局部变量、对象初始化器。 |
| **R3** | **业务逻辑只写在 `*.cs`，Designer.cs 里零逻辑** | Designer.cs 是"生成物"，人可随时重写它。任何逻辑写进去都会被设计器覆盖丢失。 |
| **R4** | **优先用布局容器，不写死 `Location`/`Size`** | 用 `TableLayoutPanel` / `FlowLayoutPanel` / `Dock` / `Anchor` / `Padding` / `Margin`。绝对定位是 WinForms 难维护的头号原因。 |
| **R5** | **界面复杂就拆 `UserControl`** | 单个 Form 超过约 15~20 个控件，或存在可复用的区块，就拆成 UserControl，每个都带自己的 Designer.cs。 |

> **AI 最常见的三个翻车点**，每次交付前必须自查：
> 1. 声明了控件字段却忘了 `this.Controls.Add(...)` → 控件不显示。
> 2. 在 `InitializeComponent()` 里写了 `foreach` 批量建控件 → 设计器直接打不开。
> 3. 忘了在构造函数里调用 `InitializeComponent()` → 窗体一片空白。

---

## 1. 两种协作模式：先选模式，再动手

长期维护的 WinForms 项目，**默认走模式 A**。只有用户明确要求"你直接把界面也生成了"，才走模式 B。

| | **模式 A：人拖界面，AI 写逻辑**（长期维护首选） | **模式 B：AI 生成 Designer.cs** |
|:--|:---|:---|
| 谁写 Designer.cs | 人在 VS 设计器里拖，设计器自动生成 | AI 手写完整 `InitializeComponent()` |
| AI 的职责 | 只写 `*.cs`：事件处理、校验、绑定、异步、服务调用 | 写 Designer.cs **和** `*.cs` |
| 对 Designer.cs 的态度 | **只读**。绝不重写、不重排、不"顺手优化" | 必须逐条满足 §3 红线 |
| 加控件的方式 | 让用户去设计器加，或按现有命名风格补最少的字段+初始化 | 直接补字段 + 初始化 + `Controls.Add` |
| 风险 | 几乎没有；设计器始终可用 | 手写易违反契约，需 §5 自检 |
| 适合 | 界面会长期演进、多人协作、交付给客户维护 | 原型、一次性工具、无 VS 环境、批量生成相似界面 |

**模式 A 的操作要点**：读 Designer.cs 只为知道控件名和类型；改逻辑时不动 Designer.cs 一行；需要新控件就明确告诉用户"请在设计器里拖一个 `Button`，命名为 `btnExport`"，然后 AI 只写 `btnExport_Click`。

**模式 B 的操作要点**：先按 §2 模板搭骨架，再逐控件填，最后跑 §5 清单。**永远不要**用循环/字典"优雅地"批量建控件。

### 混合模式（推荐给复杂项目）

AI 生成 Designer.cs，但**同时**按 R5 把界面拆成若干 UserControl——每个 UserControl 只有 5~10 个控件，短小到人一眼能看懂、能接管。这样既享受 AI 的生成速度，又保留了人后续接手的能力。示例工程 `samples/WinFormsSkillDemo` 演示的就是这个模式。

---

## 2. `InitializeComponent()` 强制模板

**模式 B 下必须严格照此结构产出**。以下就是 VS 设计器自己会生成的样子，逐字对齐才能保证往返。

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
        this.components = new System.ComponentModel.Container();
        this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
        this.btnSave = new System.Windows.Forms.Button();
        this.rootLayout.SuspendLayout();
        this.SuspendLayout();
        //
        // rootLayout
        //
        this.rootLayout.ColumnCount = 2;
        this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
        this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
        this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
        this.rootLayout.Name = "rootLayout";
        this.rootLayout.RowCount = 1;
        this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
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
        // rootLayout 收子控件
        //
        this.rootLayout.Controls.Add(this.btnSave, 0, 0);
        //
        // MainForm
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(800, 450);
        this.Controls.Add(this.rootLayout);
        this.Name = "MainForm";
        this.Text = "示例窗体";
        this.rootLayout.ResumeLayout(false);
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.TableLayoutPanel rootLayout;
    private System.Windows.Forms.Button btnSave;
}
```

### 结构要点（顺序不能乱）

1. `components` 字段 + `Dispose(bool)` 重写，放在类顶部、`#region` 之前。
2. `#region Windows Form Designer generated code` 包住 `InitializeComponent()`。
3. `InitializeComponent()` **内部顺序**：
   1. `this.components = new System.ComponentModel.Container();`（有非可视组件时才需要）
   2. 所有控件 `new`（**先父后子无所谓，但必须都在属性赋值之前**）
   3. `SuspendLayout()`：子容器先调，`this.SuspendLayout()` 最后调
   4. 逐控件属性赋值，用 `//\n// 控件名\n//` 注释分隔（设计器靠这个注释定位，**别省**）
   5. 子容器 `Controls.Add(child, col, row)`，Form 最后 `this.Controls.Add(root)`
   6. 属性赋值块结束后：子容器 `ResumeLayout(false)`，`this.ResumeLayout(false)`，必要时 `this.PerformLayout()`
4. **控件字段声明放在 `#endregion` 之后**（设计器就是这么放的），`private`，类型**全限定名**。
5. 全文用 `this.` 前缀，属性值用全限定类型（`new System.Drawing.Point(3, 3)`）。

### 关键细节

- **`InitializeComponent()` 必须是 `private void`、无参、无返回值**，名字一字不差。设计器按名字查找。
- 构造函数在 `*.cs` 里，**调用 `InitializeComponent()` 之后再挂逻辑**：
  ```csharp
  public MainForm()
  {
      InitializeComponent();   // 必须
      // 之后才能写逻辑：绑定、加载数据、订阅自己写的非设计器事件
  }
  ```
- **`AutoScaleDimensions` 的数值要匹配字体**（默认 9pt 微软雅黑 → `7F, 15F`；Segoe UI 9pt → `7F, 15F`）。别凭空编，拿不准就抄项目里已有窗体的值。
- **事件订阅用 `new System.EventHandler(this.Xxx_Click)`**，这是设计器的规范写法；方法组 `this.Xxx_Click` 也能往返，但统一用前者。
- **`Name` 属性必写**，且与字段名一致。设计器靠它序列化。

完整契约、逐条禁止清单和更多范例见 → [references/designer-cs-contract.md](./references/designer-cs-contract.md)

---

## 3. 往返安全红线（禁止清单）

`InitializeComponent()` 里**绝对不能出现**：

| 禁止 | 后果 | 正确做法 |
|:---|:---|:---|
| `for` / `foreach` / `while` / `if` / `switch` / `?:` | 设计器解析失败，窗体打不开 | 逐个控件平铺写出来 |
| 局部变量（`var b = new Button();`） | 控件无法序列化，设计器丢控件 | 一律用字段 `this.btnXxx` |
| lambda / 匿名委托（`+= (s, e) => ...`） | 设计器无法序列化事件 | 用命名方法 `this.btnXxx_Click` |
| 对象初始化器 `new Button { Text = "x" }` | 属性丢失 | 分开写 `this.btnXxx.Text = "x";` |
| 集合/数组初始化器、LINQ、`nameof()` | 解析失败或值丢失 | 字面量逐条赋值 |
| 字符串插值 `$"第{i}项"` | 设计器存的是字面量，无法往返 | 写死字面量，动态文本在逻辑里设 |
| 调用自定义方法（`BuildUi();`） | 设计器看不到控件 | 展开写平 |
| 访问数据库/文件/网络/`Environment.Exit` | **设计期就会执行**，设计器卡死或崩溃 | 挪到 `OnLoad` / 构造函数 `InitializeComponent()` 之后 |
| 业务逻辑、校验、计算 | 会被设计器覆盖丢失 | 挪到 `*.cs` |
| 手写 `.resx` 里的图片/图标 base64 | 极易写坏，设计器报错 | 让用户在设计器里设 `Image`，或用代码在运行时赋值 |

**允许且设计器自己就会生成的**：控件构造、属性赋值、`Controls.Add` / `AddRange`、`SuspendLayout`/`ResumeLayout`/`PerformLayout`、`((ISupportInitialize)x).BeginInit()/EndInit()`、`new System.EventHandler(this.M)`、`DataBindings.Add(new Binding(...))`、`new ComponentResourceManager(typeof(X))` + `resources.ApplyResources(...)`、`new System.ComponentModel.Container()`、`new ToolTip(this.components)` 等非可视组件。

---

## 4. 标准工作流（模式 B）

1. **读现有代码**：项目目标框架、`csproj` 是否 `UseWindowsForms`、已有窗体的命名风格与 `AutoScaleDimensions` 取值、字体。**照抄现有风格**，别引入第二套。
2. **规划控件清单**：列出每个控件的字段名、类型、父容器、行列位置。命名用有意义的前缀：`btn` `txt` `lbl` `cmb` `chk` `dgv` `pnl` `tab` `layout` `_` 开头私有字段。
3. **设计布局**：默认 `TableLayoutPanel` 打底 + `Dock`/`Anchor`。超过 15 个控件就按 R5 拆 UserControl。
4. **写 `*.Designer.cs`**：按 §2 模板，先字段声明，再 `InitializeComponent()`。
5. **写 `*.cs`**：构造函数调用 `InitializeComponent()`；事件处理方法与 Designer 里订阅的名字**完全一致**；逻辑、异步、绑定、释放写这里。
6. **核对 `csproj`**：SDK 风格项目按 `*.Designer.cs` 约定自动嵌套；文件名不匹配时需显式声明：
   ```xml
   <Compile Update="Forms\MainForm.Designer.cs">
     <DependentUpon>MainForm.cs</DependentUpon>
   </Compile>
   ```
7. **跑 §5 自检清单**。
8. **告知用户如何验证**：在 VS 里双击窗体 → "查看设计器"，确认能正常渲染且无黄色错误条；再随便拖动一下控件保存，确认设计器能重新序列化（这一步才真正验证了往返安全）。

布局与拆分细节 → [references/layout-and-decomposition.md](./references/layout-and-decomposition.md)
逻辑、事件、异步、绑定 → [references/logic-and-events.md](./references/logic-and-events.md)

---

## 5. 交付前自检清单

**结构**
- [ ] `InitializeComponent()` 在 `*.Designer.cs` 里，`private void`、无参。
- [ ] `*.cs` 构造函数第一行调用 `InitializeComponent()`。
- [ ] `components` 字段 + `Dispose(bool)` 重写存在（Form/UserControl）。
- [ ] `#region Windows Form Designer generated code` 包裹正确。
- [ ] 控件字段声明在 `#endregion` **之后**，全部 `private`，全限定类型。
- [ ] 类声明为 `partial class`。

**一致性（AI 最易错）**
- [ ] 每个用到的控件都有字段声明。
- [ ] 每个控件都 `new` 过。
- [ ] 每个控件都加进了某个容器的 `Controls`（或 Form），**无一遗漏**。
- [ ] Designer 里订阅的每个事件，`*.cs` 里都有签名匹配的方法（`void M(object sender, EventArgs e)`）。
- [ ] `*.cs` 里没有引用不存在的控件名。

**往返安全**
- [ ] 无循环 / 条件 / 局部变量 / lambda / 对象初始化器 / 字符串插值 / 自定义方法调用。
- [ ] 无设计期副作用（IO、DB、网络）。
- [ ] `AutoScaleDimensions` 与项目字体一致。

**布局**
- [ ] 主要靠容器 + `Dock`/`Anchor`，而非硬编码 `Location`/`Size`。
- [ ] 缩放窗体时控件排布正常（`Anchor` 或 `Dock` 生效）。
- [ ] `TabIndex` 顺序合理。

**逻辑**
- [ ] 事件处理器有 try/catch（或全局异常处理），不吞异常。
- [ ] `async void` 仅用于事件处理器；其余用 `async Task`。
- [ ] 跨线程更新 UI 走 `Invoke`/`BeginInvoke` 或 `IProgress<T>`。
- [ ] 定时器、事件订阅、`CancellationTokenSource` 在关闭时释放。

完整清单与常见翻车案例 → [references/review-checklist.md](./references/review-checklist.md)

---

## 6. 目录索引

| 文件 | 内容 |
|:---|:---|
| [references/designer-cs-contract.md](./references/designer-cs-contract.md) | **核心**：往返契约、完整模板、禁止清单、Form/UserControl/含资源三类完整范例、`.resx` 处理 |
| [references/layout-and-decomposition.md](./references/layout-and-decomposition.md) | TableLayoutPanel/FlowLayoutPanel 配方、Dock 顺序陷阱、UserControl 拆分准则、DPI 适配 |
| [references/logic-and-events.md](./references/logic-and-events.md) | 逻辑文件结构、事件签名、async/await、跨线程、数据绑定、校验、资源释放、MVP 轻量分层 |
| [references/review-checklist.md](./references/review-checklist.md) | 逐项自检清单 + 常见 AI 翻车案例与修法 |
| [assets/](./assets) | 可直接改名的骨架模板：`Form` / `UserControl` 的 Designer + 逻辑文件 |
| [samples/WinFormsSkillDemo/](./samples/WinFormsSkillDemo) | 可运行示例工程：TableLayoutPanel 布局 + UserControl 拆分 + 绑定 + async |

---

## 7. 一句话总结

> **Designer.cs 是给人看的、给人拖的、给人接管的。AI 碰它就必须守规矩；不守规矩，就别碰它——只写逻辑。**

人拖界面 + AI 写逻辑，是长期维护成本最低的分工；要 AI 生成界面，就用 UserControl 和布局容器把它拆小、拆到人愿意接手为止。
