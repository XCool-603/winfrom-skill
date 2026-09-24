# WinFormsSkillDemo —— 示例工程

演示 [SKILL.md](../../SKILL.md) 的推荐做法：**AI 生成 `InitializeComponent()` + `UserControl` 拆分 + 布局容器**（模式 B + R5 混合模式）。

---

## 验证状态

| 项目 | 状态 | 证据 |
|:---|:---|:---|
| **编译** | ✅ 已验证 | `dotnet build`（net8.0-windows）→ **0 个警告，0 个错误** |
| **启动不崩** | ✅ 已验证 | 启动进程并存活 5 秒，主窗口标题正确读出 `客户管理 —— WinForms Skill 示例`，说明 `InitializeComponent()` 执行、子 `UserControl` 装载、异步加载均无异常 |
| **Designer.cs 契约** | ✅ 已验证 | `tools/check-designer.ps1` 通过（三件套齐全、`InitializeComponent()` 内无违规写法、事件订阅与处理器一一对应） |
| **`assets/` 模板** | ✅ 已验证 | 替换占位符后单独建工程编译 → **0 个警告，0 个错误**（`<Nullable>disable</Nullable>`） |
| **Nullable 指引** | ✅ 已验证 | 按 `designer-cs-contract.md` §7.1 的配方（Designer.cs 加 `#nullable disable` + 逻辑文件用 `object?` / `EventHandler?`）在 `<Nullable>enable</Nullable>` 下编译 → **0 个警告** |
| **VS 设计器往返** | ❌ **未验证** | 本环境没有 Visual Studio，**无法**打开设计器并保存。请你按下方步骤自行确认 |
| **DPI 缩放 / 真实数据** | ❌ 未验证 | 需要你在实际环境下检查 |

> **重要**：编译通过 ≠ 设计器往返安全。前者只证明语法和 API 正确，后者要靠设计器真的保存一次才能证明。
> 请务必执行下面「怎么验证设计器往返安全」的步骤。

本地复现：

```powershell
cd samples\WinFormsSkillDemo
dotnet build
dotnet run
```

---

## 结构

```
WinFormsSkillDemo/
├── WinFormsSkillDemo.csproj      net8.0-windows + UseWindowsForms + PerMonitorV2
├── Program.cs                    ApplicationConfiguration.Initialize() + 全局异常兜底
├── Models/
│   └── Customer.cs               INotifyPropertyChanged
├── Services/
│   └── CustomerService.cs        ICustomerService + 假实现（延迟模拟 IO）
├── Views/
│   ├── CustomerListView.Designer.cs   UserControl 界面
│   └── CustomerListView.cs            UserControl 逻辑
└── Forms/
    ├── MainForm.Designer.cs      窗体界面（已退化成布局壳）
    └── MainForm.cs               窗体逻辑
```

**控件数**：`MainForm` 只有 6 个控件（1 个容器 + 1 个工具栏 + 2 个按钮 + 1 个子控件 + 1 个状态标签）；
客户列表区块（搜索框 + 表格）全部落在 `CustomerListView` 里。这就是 R5 想要的效果——**主窗体不再是一个巨型 `InitializeComponent()`**。

---

## 这个示例演示了什么

| 做法 | 位置 | 对应文档 |
|:---|:---|:---|
| 界面代码全部在 `*.Designer.cs` 的 `InitializeComponent()` | 两个 `.Designer.cs` | [SKILL.md](../../SKILL.md) R1 |
| 逻辑全部在 `*.cs`，Designer.cs 零逻辑 | `MainForm.cs` / `CustomerListView.cs` | R3 |
| `TableLayoutPanel` 三区布局，不赌 `Dock` 顺序 | `MainForm.Designer.cs` | [layout-and-decomposition.md](../../references/layout-and-decomposition.md) §3.2 |
| `UserControl` 拆分，主窗体退化成布局壳 | `Views/CustomerListView.*` | R5 / layout §7 |
| 对外暴露语义（属性 + 事件），不暴露控件字段 | `CustomerListView.SearchText` / `CustomerActivated` | layout §7.3 |
| 每个控件"三件套"齐全（字段 + new + `Controls.Add`） | 两个 `.Designer.cs` | [designer-cs-contract.md](../../references/designer-cs-contract.md) §2 |
| `DataGridView` 显式列 + `AutoGenerateColumns = false` | `CustomerListView.Designer.cs` | layout §10 |
| 设计期守卫用 `LicenseManager.UsageMode` | 两个 `.cs` 的构造函数 | [logic-and-events.md](../../references/logic-and-events.md) §2 |
| `InitializeComponent()` 早于任何控件访问 | 两个 `.cs` 的构造函数 | logic §1 |
| 异步加载放 `OnLoad`，不在构造函数 | `MainForm.OnLoad` | logic §4.3 |
| `IProgress<T>` 回报进度，不手写 `Invoke` | `MainForm.RefreshAsync` | logic §4.2 |
| 防重入（禁用触发按钮）+ `finally` 恢复 | `MainForm.RefreshAsync` | logic §4.2 |
| `BindingList<T>` + `BindingSource` | `CustomerListView` | logic §6.2 |
| 批量更新时关掉 `RaiseListChangedEvents` | `CustomerListView.ApplyFilter` | logic §6.2 |
| `OnFormClosed` 清理 `CancellationTokenSource` | `MainForm.OnFormClosed` | logic §8.2 |
| 全局异常兜底 | `Program.cs` | logic §8.3 |
| `ApplicationConfiguration.Initialize()` + PerMonitorV2 | `csproj` / `Program.cs` | layout §9.1 |
| 对象初始化器**只在逻辑文件**里用（Designer.cs 里禁止） | `MainForm.btnAdd_Click` | designer-cs-contract §4.1 |

---

## 运行效果

- 启动后自动异步加载 8 条假客户数据，状态栏显示 `加载中… 20%` → `100%` → `共 8 位客户`。
- "刷新" 重新加载（期间按钮禁用，防重入）。
- "新增" 追加一条客户，状态栏计数自动更新（由 `FilteredCountChanged` 事件驱动）。
- 搜索框输入关键字，客户端过滤姓名/邮箱。
- 双击某一行，弹出客户姓名（事件由 `MainForm` 处理，控件自己不弹窗）。

---

## 怎么验证"设计器往返安全"

这是本示例最值得亲自跑一遍的部分：

1. `dotnet build` —— 必须零错误。
2. 在 VS 里双击 `Forms\MainForm.cs` → **查看设计器**：
   - 应该能看到工具栏（刷新/新增）+ 客户列表控件 + 状态标签。
   - **不应该**出现黄色错误条。
3. 在画布上**拖动一个按钮**，`Ctrl+S` 保存。
4. 看 `MainForm.Designer.cs` 的 diff：
   - 只有按钮坐标变化 → ✅ 往返安全。
   - 控件消失或大段重排 → ❌ 回查 [review-checklist.md](../../references/review-checklist.md) §7。
5. `Ctrl+Z` 撤销拖动，再保存，恢复原状。
6. 同样步骤验证 `Views\CustomerListView.cs`（它会作为独立设计器打开）。

---

## 关于字体与 `AutoScaleDimensions`

`csproj` 里**没有**设置 `ApplicationDefaultFont`，所以用系统默认字体（Segoe UI 9pt），
此时各 `.Designer.cs` 里的 `AutoScaleDimensions = new SizeF(7F, 15F)` 是正确的。

若你想换成中文字体，取消 `csproj` 里 `ApplicationDefaultFont` 的注释，**并同步核对所有 `AutoScaleDimensions`**——
不匹配会导致设计器里看到的布局和运行时不一致。
