# 布局与拆分：让 WinForms 长期可维护

[SKILL.md](../SKILL.md) 的 R4（布局容器）和 R5（UserControl 拆分）展开。核心论点：

> **WinForms 难维护，几乎从不是因为 WinForms 本身，而是因为满屏的 `Location = new Point(137, 264)`。**
> 换掉绝对定位，WinForms 的维护性可以追上 WPF。

---

## 1. 绝对定位为什么是原罪

设计器拖出来的控件默认带 `Location` + `Size`。这带来四个连锁问题：

| 问题 | 具体表现 |
|:---|:---|
| **改不动** | 想给顶部插一行，下面 40 个控件的 `Location.Y` 全要重算 |
| **不缩放** | 拉大窗口，控件原地不动，右下角一片空白 |
| **不抗 DPI** | 125% 缩放下文字撑破控件、按钮错位 |
| **不抗本地化** | 德语标签比中文长一倍，全部截断 |

**目标**：`Location`/`Size` 只在设计器自动生成时存在，**不要由 AI 手工推导**。AI 手算坐标是 bug 的稳定来源——算错一个数字，界面上就是控件重叠或飞出可视区，而且很难 review。

**判据**：如果你在 Designer.cs 里为了"对齐"而计算坐标，说明布局方案选错了。

---

## 2. 容器选型决策表

| 需求 | 用什么 | 关键属性 |
|:---|:---|:---|
| 表单：标签 + 输入框，逐行排列 | `TableLayoutPanel` | `ColumnCount=2`，第 0 列 `Absolute`，第 1 列 `Percent 100` |
| 上下分区：工具栏 / 内容 / 状态栏 | `TableLayoutPanel` | 3 行：`AutoSize` / `Percent 100` / `AutoSize` |
| 左右分栏，宽度可调 | `SplitContainer` | `Orientation`、`FixedPanel`、`SplitterDistance` |
| 左右分栏，宽度固定 | `TableLayoutPanel` | `Absolute` + `Percent 100` |
| 一排按钮 / 标签，自动换行 | `FlowLayoutPanel` | `FlowDirection`、`WrapContents`、`AutoSize` |
| 同尺寸卡片网格 | `TableLayoutPanel` | 全部 `Percent` 等分 |
| 分页 | `TabControl` | 每个 `TabPage` 内部再放 `TableLayoutPanel` |
| 单区域填满 | `Panel` + `Dock=Fill` | 最简单，无需容器 |
| 内容超长要滚动 | `Panel` + `AutoScroll=true` | 或 `FlowLayoutPanel` + `AutoScroll` |

**默认选择：`TableLayoutPanel`。** 它确定性最强（位置由行列决定，不依赖 z 序），设计器支持最好，抗 DPI 最好。

---

## 3. `TableLayoutPanel` 三个标准配方

### 3.1 表单（标签 + 输入）

```csharp
// 2 列：标签固定 90px，输入占满剩余
this.rootLayout.ColumnCount = 2;
this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));

// N 行：每个字段 AutoSize，最后一行 Percent 100 吸收多余空间（可选）
this.rootLayout.RowCount = 4;
this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));

this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
this.rootLayout.Padding = new System.Windows.Forms.Padding(12);
```

- **标签**：`Anchor = Left` + `AutoSize = true`（垂直居中用 `AnchorStyles.Left`，让它随行高居中）。
- **输入框**：`Anchor = Left | Right`，随列宽拉伸。
- **最后一行 `Percent 100`**：把内容顶到上方。不要用一堆空 `Label` 占位。

### 3.2 三区页面（工具栏 / 内容 / 状态栏）— **推荐首选**

```csharp
this.rootLayout.ColumnCount = 1;
this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));

this.rootLayout.RowCount = 3;
this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));      // 工具栏
this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F)); // 内容
this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));      // 状态栏

this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
this.rootLayout.Padding = new System.Windows.Forms.Padding(0);
```

每一格里放一个 `Dock = Fill` 的子控件（`FlowLayoutPanel` 工具栏、`DataGridView` 或 `UserControl` 内容、`StatusStrip`）。

**为什么推荐**：位置完全由行列决定，**不依赖 Dock 顺序、不依赖 z 序**，是 AI 生成时最不容易出错的方案。写进 Designer.cs 后行为可预测。

### 3.3 固定侧栏 + 自适应主区

```csharp
this.rootLayout.ColumnCount = 2;
this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 200F)); // 侧栏固定
this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F)); // 主区自适应
```

需要用户能拖动分隔条时，改用 `SplitContainer`（`FixedPanel = Panel1` 保持侧栏宽度）。

### 3.4 配方通用检查项

- `RowCount` / `ColumnCount` 的数值必须和 `RowStyles` / `ColumnStyles` 的**条目数一致**。设计器强制这一点；手写时不一致会导致布局行为诡异（多余行列按默认 `AutoSize` 处理）。
- `SetColumnSpan` / `SetRowSpan` **必须写在对应的 `Controls.Add` 之后**：
  ```csharp
  this.rootLayout.Controls.Add(this.flowButtons, 0, 2);
  this.rootLayout.SetColumnSpan(this.flowButtons, 2);   // 之后
  ```
- 单元格内的间距由控件的 `Margin` 决定，不是 `Padding`。想统一间距就在每个控件上设 `Margin`（如 `new Padding(3, 6, 3, 6)`）。

---

## 4. `Dock` 与 `Anchor`

### 4.1 `Anchor` 规则表

| 想要的缩放行为 | `Anchor` 设置 |
|:---|:---|
| 跟着父容器一起变宽 | `Top \| Left \| Right` |
| 跟着父容器一起变高 | `Top \| Left \| Bottom` |
| 宽高都跟着变 | `Top \| Left \| Right \| Bottom` |
| 钉在右下角 | `Bottom \| Right` |
| 钉在右上角 | `Top \| Right` |
| 垂直居中（行高变化时） | `Left`（`AnchorStyles.Left` 在 `TableLayoutPanel` 单元格里表现为垂直居中） |
| 完全不动（默认） | `Top \| Left` |

**注意**：`Anchor` 的默认值是 `Top | Left`。`AnchorStyles.None` 会让控件在单元格里居中——这是 `TableLayoutPanel` 里做"居中按钮"的正确方式。

### 4.2 `Dock` 顺序（重要陷阱）

停靠布局按 **z 序反向**解析：`Controls` 集合里靠后的（后 `Add` 的、在"文档大纲"里靠上的）控件**先占位**。`Dock = Fill` 的控件由布局引擎延后处理，始终填充**剩余区域**。

实务结论：

- **`Fill` 与边缘停靠混用时，顺序通常不会出错**（因为 `Fill` 被延后）。常见写法是先 `Controls.Add` 那个 `Fill` 面板，再 `Add` `Top`/`Bottom` 面板。
- **边缘与边缘之间**（`Top` vs `Bottom`，`Left` vs `Right`）谁先占位**取决于 z 序**，这里容易翻车。
- **不要靠手工推导 Dock 顺序。** 顺序拿不准时：
  1. **改用 `TableLayoutPanel` 分格**（§3.2）——彻底消除这个不确定性，**首选**；
  2. 或者在 VS 里用 **"视图 → 其他窗口 → 文档大纲"** 直观调整 z 序，这是唯一可靠的确认方式。

> **给 AI 的硬建议**：需要两个以上边缘停靠面板时，**默认用 `TableLayoutPanel` 的行列代替**，不要赌 `Controls.Add` 顺序。这不是风格偏好，是避免不确定性。

### 4.3 `Dock` 与 `Anchor` 互斥

同一个控件上同时设 `Dock` 和 `Anchor`，`Dock` 生效、`Anchor` 被忽略。设计器里切换 `Dock` 会自动清掉 `Anchor`。**别两个都写**——设计器保存时会删掉一个，产生假 diff。

---

## 5. 间距：用 `Margin` / `Padding`，不要用空白控件

| 工具 | 作用域 | 用法 |
|:---|:---|:---|
| `Padding` | 容器**内部**四周留白 | 整个表单留白：`rootLayout.Padding = new Padding(12)` |
| `Margin` | 控件**外部**四周留白 | 单元格内控件间距：`txtName.Margin = new Padding(3, 6, 3, 6)` |

**禁止**：为了撑开间距而塞空的 `Label`、空的 `Panel`、`Text = " "` 的标签。这些在设计器里看不见用途，是维护灾难。

统一间距的做法：设计器里全选同类控件，一次设 `Margin`。

---

## 6. `AutoSize` 的交互

- `AutoSize = true` 的控件（`Label`、`Button`、`CheckBox`）尺寸由内容决定，**不要同时手设 `Size`**——设计器会保留 `Size` 但运行时被覆盖，产生"设计器里好好的、跑起来变了"的困惑。
- `Label` 默认 `AutoSize = true`。多行文本用 `AutoSize = true` + `MaximumSize = new Size(w, 0)` 让它在固定宽度内自动换行。
- `Button` 想让文字撑开：`AutoSize = true` + `AutoSizeMode = GrowAndShrink`。
- 容器（`Form`/`UserControl`/`Panel`）的 `AutoSize` 慎用：容易和 `Dock`/`Anchor` 打架，导致设计期尺寸跳变。**容器优先用 `Dock` 表达尺寸。**

---

## 7. UserControl 拆分准则（R5 展开）

### 7.1 什么时候必须拆

满足任意一条就该拆：

- 单个 `Form` 的控件数 **超过 15~20 个**。
- 存在**可复用**的区块（多个窗体都要用）。
- 存在**职责独立**的区块（有自己的数据源、自己的加载逻辑）。
- 存在**会独立演进**的区块（不同人/不同需求改它）。
- 一个区块的逻辑超过 **200 行**。

### 7.2 拆完长什么样

```
Forms/
  MainForm.cs
  MainForm.Designer.cs          ← 只负责：工具栏 + 内容宿主 + 状态栏
Views/
  CustomerListView.cs
  CustomerListView.Designer.cs  ← 只负责：搜索框 + 表格 + 分页
  CustomerEditorView.cs
  CustomerEditorView.Designer.cs← 只负责：字段 + 保存/取消
Models/
  Customer.cs
```

`MainForm.Designer.cs` 里只是一句 `this.Controls.Add(this.customerListView);`——**主窗体退化成布局壳**，这就是目标。

### 7.3 拆分的三个硬约束

1. **每个 `UserControl` 必须有 `public` 无参构造函数。** 否则 VS 设计器无法在设计期实例化它，报"基类无法实例化"。需要依赖注入时用属性注入或 `IServiceProvider` 静态定位，**不要**改成带参构造。
2. **不要向外暴露内部控件。** 别把 `txtName` 改成 `public`。暴露"意图"而不是"零件"：
   ```csharp
   // ✅ 好：暴露语义
   public string CustomerName
   {
       get => txtName.Text;
       set => txtName.Text = value ?? string.Empty;
   }

   public event EventHandler<CustomerSavedEventArgs>? Saved;

   // ❌ 坏：父窗体开始操作子控件的内部零件
   // public TextBox txtName;
   // → 父窗体写 view.txtName.Text = "..."; 布局一改就全线崩
   ```
3. **`UserControl` 不直接弹 `MessageBox` / 开新窗体。** 通过事件把决定权交给宿主，否则无法复用、无法测试。要提示就暴露事件让 `MainForm` 决定怎么显示。

### 7.4 组合方式

父容器用 `TableLayoutPanel` 的一格 + `Dock = Fill` 装载子 `UserControl`：

```csharp
// MainForm.Designer.cs
this.customerListView = new Demo.Views.CustomerListView();
this.customerListView.Dock = System.Windows.Forms.DockStyle.Fill;
this.customerListView.Name = "customerListView";
this.customerListView.TabIndex = 1;
// ...
this.rootLayout.Controls.Add(this.customerListView, 0, 1);
```

**注意**：`UserControl` 的字段类型用全限定名（`Demo.Views.CustomerListView`），和设计器风格一致。

---

## 8. 命名约定

设计器默认生成 `button1`、`label2`、`textBox3`——**这是可维护性的头号杀手**。拖完立刻改名，AI 生成时直接用有意义的名字。

| 前缀 | 控件 | 示例 |
|:---|:---|:---|
| `btn` | `Button` | `btnSave`、`btnCancel` |
| `txt` | `TextBox` | `txtName`、`txtSearch` |
| `lbl` | `Label` | `lblTitle`、`lblStatus` |
| `chk` / `rdo` | `CheckBox` / `RadioButton` | `chkEnabled`、`rdoMale` |
| `cmb` | `ComboBox` | `cmbCategory` |
| `lst` | `ListBox` / `ListView` | `lstResults` |
| `dgv` | `DataGridView` | `dgvCustomers` |
| `tv` | `TreeView` | `tvFolders` |
| `pnl` | `Panel` | `pnlHeader` |
| `grp` | `GroupBox` | `grpOptions` |
| `tab` / `tpg` | `TabControl` / `TabPage` | `tabMain`、`tpgGeneral` |
| `layout` | `TableLayoutPanel` / `FlowLayoutPanel` | `rootLayout`、`flowButtons` |
| `split` | `SplitContainer` | `splitMain` |
| `menu` / `tsm` | `MenuStrip` / `ToolStripMenuItem` | `menuMain`、`tsmFile` |
| `status` | `StatusStrip` | `statusMain` |
| `pb` / `prg` | `PictureBox` / `ProgressBar` | `pbLogo`、`prgLoad` |
| `num` / `dtp` | `NumericUpDown` / `DateTimePicker` | `numQty`、`dtpStart` |
| `err` / `tip` / `tmr` | `ErrorProvider` / `ToolTip` / `Timer` | `errInput`、`tipMain`、`tmrRefresh` |
| `bs` | `BindingSource` | `bsCustomers` |

**事件处理器命名**：`控件名_事件名`，如 `btnSave_Click`、`txtName_Validating`、`dgvCustomers_CellDoubleClick`。这是设计器双击生成的名字，保持一致就不用改 Designer.cs。

---

## 9. DPI 适配

### 9.1 现代项目（.NET 6+，推荐）

`csproj`：

```xml
<PropertyGroup>
  <TargetFramework>net8.0-windows</TargetFramework>
  <UseWindowsForms>true</UseWindowsForms>
  <ApplicationHighDpiMode>PerMonitorV2</ApplicationHighDpiMode>
  <ApplicationDefaultFont>Microsoft YaHei UI, 9pt</ApplicationDefaultFont>
</PropertyGroup>
```

这两条 MSBuild 属性会生成 `ApplicationConfiguration.Initialize()`，`Program.cs` 里直接调用：

```csharp
[STAThread]
static void Main()
{
    ApplicationConfiguration.Initialize();   // 含 SetHighDpiMode / EnableVisualStyles / SetCompatibleTextRenderingDefault / SetDefaultFont
    Application.Run(new MainForm());
}
```

**不要**再手写 `Application.SetHighDpiMode(...)` + `Application.EnableVisualStyles()` + `Application.SetCompatibleTextRenderingDefault(false)`——会和 `ApplicationConfiguration.Initialize()` 重复，且顺序敏感。

### 9.2 `.NET Framework` / 老项目

```csharp
[STAThread]
static void Main()
{
    Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);   // 需要 .NET Framework 4.7+ 且 App.config 里加 System.Windows.Forms.ApplicationConfigurationSection
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    Application.Run(new MainForm());
}
```

并在 `app.manifest` 里声明 DPI 感知：

```xml
<application xmlns="urn:schemas-microsoft-com:asm.v3">
  <windowsSettings>
    <dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">true/pm</dpiAware>
    <dpiAwareness xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">PerMonitorV2</dpiAwareness>
  </windowsSettings>
</application>
```

### 9.3 控件层面

- **`AutoScaleMode = AutoScaleMode.Font`**（设计器默认），配 `AutoScaleDimensions` 与项目字体一致。**别改成 `Dpi`**，`Font` 模式在现代 .NET 上表现最好。
- **`AutoScaleDimensions` 的数值要匹配字体**：9pt 微软雅黑 / Segoe UI 通常生成 `7F, 15F`。**抄项目里已有窗体的值**，别自己编——不匹配会导致设计器与运行时缩放不一致。
- **不要给单个控件设 `Font`**，除非是刻意的视觉层次（如标题加粗）。让字体从父容器继承，DPI 缩放才一致。
- **避免固定像素尺寸**：`AutoSize = true`、`Dock`、`Percent` 行列表都天然抗缩放。
- **图标用矢量或 `SizeMode = Zoom`**：`PictureBox.SizeMode = PictureBoxSizeMode.Zoom` 让位图随 DPI 缩放而不裁切。

---

## 10. 数据密集控件

### `DataGridView`

```csharp
this.dgvCustomers.AllowUserToAddRows = false;
this.dgvCustomers.AllowUserToDeleteRows = false;
this.dgvCustomers.AllowUserToResizeRows = false;
this.dgvCustomers.AutoGenerateColumns = false;      // ★ 必须：列在 Designer 里显式定义
this.dgvCustomers.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
this.dgvCustomers.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
this.dgvCustomers.Dock = System.Windows.Forms.DockStyle.Fill;
this.dgvCustomers.ReadOnly = true;
this.dgvCustomers.RowHeadersVisible = false;         // 去掉左侧空白
this.dgvCustomers.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
this.dgvCustomers.MultiSelect = false;
```

**`AutoGenerateColumns = false` + 显式列**是必须的：否则每次数据结构一变，列就全乱，且列宽/标题无法维护。显式列由设计器生成 `DataGridViewTextBoxColumn`，放在 `components` 之外的字段里，并在 `InitializeComponent()` 里 `this.dgvCustomers.Columns.AddRange(new DataGridViewColumn[] { this.colName, this.colEmail });`。

> **注意**：`Columns.AddRange(new DataGridViewColumn[] { ... })` 里用了数组初始化器——**这是设计器自己会生成的写法**，属于白名单例外，可以照写。原因是列对象本身也是字段，数组只是把它们聚合起来。

**大数据量**：`VirtualMode = true` + `CellValueNeeded`，或绑 `BindingList<T>` 并关闭 `AutoSizeColumnsMode`（列自适应在大表上很慢）。

### `TreeView` / `ListView`

结构在设计期固定 → 写在 Designer.cs；数据驱动 → 运行时填充，Designer.cs 只留容器和列定义。

---

## 11. 布局故障对照表

| 现象 | 原因 | 修法 |
|:---|:---|:---|
| 控件重叠 | 加进了错误的父容器；或用了绝对定位算错 | 检查 `Controls.Add` 的接收者；改用容器 |
| 控件看不见 | 父容器尺寸为 0；或被同格控件遮挡 | 检查 `RowStyles`/`ColumnStyles` 数量；检查 z 序 |
| 拉大窗口控件不动 | 没设 `Anchor`/`Dock` | 按 §4.1 设 `Anchor` |
| `Dock=Fill` 的控件挤掉了别的控件 | `Fill` 与边缘停靠的 z 序冲突 | 改用 `TableLayoutPanel` 分格 |
| 设计器里正常，运行时位置变了 | `AutoSize` 覆盖了手设 `Size`；或 `AutoScaleDimensions` 不匹配字体 | 去掉冲突的 `Size`；对齐项目字体 |
| 125% DPI 下文字被截断 | 固定像素尺寸 + `AutoSize=false` | 改 `AutoSize=true` 或容器布局 |
| `SetColumnSpan` 不生效 | 写在 `Controls.Add` 之前 | 移到之后 |
| 行列数不对，多出一格 | `RowCount`/`ColumnCount` 与 Styles 条目数不一致 | 对齐两者 |
| 单元格里控件贴边 | 没设 `Margin` | 设 `Margin`，不要塞空 Label |
| 子 `UserControl` 在设计器里报"基类无法实例化" | 缺少 public 无参构造 | 补无参构造，改属性注入 |

---

## 12. 小结

1. **默认 `TableLayoutPanel`**，按行列分格，不赌 `Dock` 顺序。
2. **`Anchor` 管缩放，`Dock` 管填满**，两者不混用。
3. **间距用 `Margin`/`Padding`**，不用空白控件。
4. **控件 > 15~20 个就拆 `UserControl`**，主窗体退化成布局壳。
5. **暴露语义（属性 + 事件），不暴露零件（控件字段）**。
6. **`AutoScaleMode.Font` + 匹配字体的 `AutoScaleDimensions`**，其余交给容器。
7. **命名从第一天就有意义**，别留 `button1`。
