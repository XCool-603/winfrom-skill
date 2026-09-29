# WinFormsSkillDemo —— 分层示例工程

演示 [SKILL.md](../../SKILL.md) 的推荐做法：**标准目录结构 + 完整分层 + AI 生成 `InitializeComponent()` + UserControl 拆分**。

---

## 目录结构

```
WinFormsSkillDemo/
├── WinFormsSkillDemo.csproj          net8.0-windows + UseWindowsForms + PerMonitorV2
├── Program.cs                        组装根：全项目唯一 new 具体实现的地方
├── README.md                         本文件
│
├── Forms/                            ① 表现层：窗体（布局 + 事件转发）
│   ├── MainForm.Designer.cs              界面（只有 6 个控件，是布局壳）
│   └── MainForm.cs                       逻辑（只有转发和状态刷新）
│
├── Views/                            ① 表现层：可复用界面区块
│   ├── CustomerListView.Designer.cs      搜索框 + 表格
│   └── CustomerListView.cs               实现 ICustomerListView，零业务逻辑
│
├── Presenters/                       ② 表现逻辑层
│   ├── ICustomerListView.cs              界面契约（不含任何 WinForms 类型）
│   └── CustomerListPresenter.cs          加载 / 筛选 / 新增 / 状态管理
│
├── Models/                           ③ 领域层：纯数据
│   ├── Customer.cs                       INotifyPropertyChanged
│   └── CustomerEventArgs.cs              事件参数
│
├── Services/                         ④ 应用层：业务规则
│   ├── Abstractions/ICustomerService.cs
│   └── CustomerService.cs                排序、筛选、新建命名规则
│
├── Data/                             ⑤ 基础设施层：数据访问
│   ├── Abstractions/ICustomerRepository.cs
│   └── FakeCustomerRepository.cs         假数据 + 延迟（换真库只改这里）
│
└── Common/                           横切关注点
    ├── Constants/AppConstants.cs
    └── Extensions/StringExtensions.cs
```

**依赖方向**（只能向下）：

```
Forms/Views  →  Presenters  →  Services  →  Data
                    ↓              ↓          ↓
                  Models  ←────────+──────────+
```

---

## 这个示例演示了什么

| 做法 | 位置 | 对应文档 |
|:---|:---|:---|
| **标准分层目录结构** | 整个工程 | [project-structure.md](../../references/project-structure.md) §2 |
| **业务层零 WinForms 依赖** | `Models/` `Services/` `Data/` `Presenters/` `Common/` | project-structure §0 |
| **依赖注入 + 组装根** | `Program.cs` | project-structure §4 |
| **DI 与设计器共存的双构造函数** | `Forms/MainForm.cs` | project-structure §4 ⚠️ |
| **Presenter 面向 View 接口（可单元测试）** | `Presenters/ICustomerListView.cs` | [logic-and-events.md](../../references/logic-and-events.md) §9 |
| 界面代码全部在 `*.Designer.cs` 的 `InitializeComponent()` | 两个 `.Designer.cs` | [SKILL.md](../../SKILL.md) R1 |
| 逻辑全部在 `*.cs`，Designer.cs 零逻辑 | 各 `.cs` | R3 |
| `TableLayoutPanel` 三区布局，不赌 `Dock` 顺序 | `MainForm.Designer.cs` | [layout-and-decomposition.md](../../references/layout-and-decomposition.md) §3.2 |
| `UserControl` 拆分，主窗体退化成布局壳 | `Views/CustomerListView.*` | R5 / layout §7 |
| 对外暴露语义（属性 + 事件），不暴露控件字段 | `CustomerListView` | layout §7.3 |
| 每个控件"三件套"齐全（字段 + new + `Controls.Add`） | 两个 `.Designer.cs` | [designer-cs-contract.md](../../references/designer-cs-contract.md) §2 |
| `DataGridView` 显式列 + `AutoGenerateColumns = false` | `CustomerListView.Designer.cs` | layout §10 |
| 设计期守卫用 `LicenseManager.UsageMode` | `MainForm.cs` / `CustomerListView.cs` | logic §2 |
| 异步加载放 `OnLoad`，不在构造函数 | `MainForm.OnLoad` | logic §4.3 |
| `IProgress<T>` 回报进度，不手写 `Invoke` | `Presenters/CustomerListPresenter.cs` | logic §4.2 |
| 防重入（Presenter 持有 `IsBusy`，界面据此禁用） | `CustomerListPresenter.RefreshAsync` | logic §4.2 |
| `BindingList<T>` + `BindingSource` | `CustomerListView` | logic §6.2 |
| 批量更新时关掉 `RaiseListChangedEvents` | `CustomerListView.SetCustomers` | logic §6.2 |
| `OnFormClosed` 清理 Presenter 与 `CancellationTokenSource` | `MainForm.OnFormClosed` | logic §8.2 |
| 全局异常兜底 | `Program.cs` | logic §8.3 |
| 对象初始化器**只在逻辑文件**里用（Designer.cs 里禁止） | `CustomerService.CreateNewCustomer` | designer-cs-contract §4.1 |

### 关键：主窗体有多薄

`MainForm` 一共只有 **6 个控件**（根布局 + 工具栏 + 2 个按钮 + 子 UserControl + 状态标签），
它的 `.cs` 里**没有一行业务逻辑**——只有事件转发和"把 Presenter 的状态画到界面上"。
业务全在 `CustomerListPresenter`，数据全在 `CustomerService` / `FakeCustomerRepository`。

**换数据库时，只需改 `Program.cs` 里的一行**，界面、Presenter、Service 一行都不用动。

---

## 运行效果

- 启动后自动异步加载 8 条假客户数据，状态栏 `加载中… 20%` → `100%` → `共 8 位客户`。
- "刷新" 重新加载（期间按钮和列表禁用，防重入）。
- "新增" 追加一条客户，状态栏计数自动更新。
- 搜索框输入关键字，按姓名/邮箱筛选（筛选规则在 `CustomerService.Search`，不在界面里）。
- 双击某一行，弹窗显示客户详情（由 `CustomerListView.ShowCustomerDetail` 决定怎么显示，Presenter 不碰 `MessageBox`）。

---

## 验证状态

| 项目 | 状态 | 证据 |
|:---|:---|:---|
| **编译** | ✅ 已验证 | `dotnet build`（net8.0-windows）→ **0 个警告，0 个错误** |
| **启动不崩** | ✅ 已验证 | 启动进程并存活 5 秒，主窗口标题正确读出 `客户管理 —— WinForms Skill 示例` |
| **目录与分层** | ✅ 已验证 | `tools/check-structure.ps1` 通过：7 个分层目录齐全、业务层零 WinForms 依赖、Models 只依赖 System、Designer 配对完整、命名空间与文件夹一致 |
| **Designer.cs 契约** | ✅ 已验证 | `tools/check-designer.ps1` 通过 |
| **VS 设计器往返** | ❌ **未验证** | 本环境没有 Visual Studio，**无法**打开设计器并保存。请你按下方步骤自行确认 |
| **DPI 缩放 / 真实数据** | ❌ 未验证 | 需要你在实际环境下检查 |

> **编译通过 ≠ 设计器往返安全。** 前者只证明语法和 API 正确，后者要靠设计器真的保存一次才能证明。

本地复现：

```powershell
cd samples\WinFormsSkillDemo
dotnet build
dotnet run
```

---

## 怎么验证"设计器往返安全"

这是本示例最值得亲自跑一遍的部分：

1. `dotnet build` —— 必须零错误。
2. 在 VS 里双击 `Forms\MainForm.cs` → **查看设计器**：
   - 应该能看到工具栏（刷新/新增）+ 客户列表控件 + 状态标签。
   - **不应该**出现黄色错误条。
   - 注意：`MainForm` 有**两个构造函数**，设计器走的是无参那个（见 `MainForm.cs` 注释）。
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
