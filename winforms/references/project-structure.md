# 项目结构与分层架构

[SKILL.md](../SKILL.md) 的 R6 展开。**这份文档回答一个 AI 最常忽略、新手最需要的问题：文件到底该放哪个文件夹？**

---

## 0. 两条原则

1. **目录即架构。** 文件夹结构就是分层本身。AI 和人看同一张地图找文件；结构清晰，改需求时不用先读一遍全部代码。
2. **依赖只能单向。** `Forms/Views → Presenters → Services → Data`。反向引用一次，架构就开始腐化。

配套一条**可机械检查**的硬规则，这是分层的试金石：

> ### 🚨 只有 `Forms/` 和 `Views/` 允许出现 `using System.Windows.Forms;`
> `Models/`、`Services/`、`Data/`、`Presenters/`、`Common/` 里**一律不允许**。
> 违反即说明你把界面逻辑写进了业务层——分层已经名存实亡。

这条规则的价值：它把"分层"从口号变成**能自动验证的事实**，也是新手唯一需要记住的一句话。
配套脚本见 [tools/check-structure.ps1](../tools/check-structure.ps1)。

---

## 1. 先选方案

| | **方案 A：单项目分层**（默认） | **方案 B：多项目解决方案** |
|:---|:---|:---|
| **何时用** | 1~15 个窗体、1~3 人、快速起步、学习 | 15+ 窗体、多人协作、需要独立测试/打包 |
| **优点** | 好上手、编译快、改动轻、没有项目引用配置 | 依赖由**编译器**强制，无法违规；可单独测试与复用 |
| **缺点** | 依赖方向靠自觉（用检查脚本兜底） | 上手重，改一个类型可能动多个项目 |
| **新手** | ✅ **从这里开始** | 熟悉后再迁移（§7 有平滑路径） |

**默认选 A。** 本文档 §2~§6 讲 A，§7 讲 B 与迁移。

> 别一上来就搭四层多项目解决方案。**新手死在过度设计上的概率，远高于死在架构不够"高级"上。**
> 方案 A 的目录结构本身就是分层的，将来要升级成 B，只是把文件夹变成项目而已。

---

## 2. 方案 A：完整目录（照抄即可）

```
MyApp/
├── MyApp.csproj
├── Program.cs                    ← 程序入口 / 组装根
│
├── Forms/                        ← ① 表现层：窗体
│   ├── MainForm.cs
│   └── MainForm.Designer.cs
│
├── Views/                        ← ① 表现层：可复用界面区块（UserControl）
│   ├── CustomerListView.cs
│   └── CustomerListView.Designer.cs
│
├── Presenters/                   ← ② 表现逻辑层：每个 View 一个 Presenter
│   ├── ICustomerListView.cs
│   └── CustomerListPresenter.cs
│
├── Models/                       ← ③ 领域层：实体、枚举、事件参数
│   ├── Customer.cs
│   └── CustomerEventArgs.cs
│
├── Services/                     ← ④ 应用层：业务规则、用例
│   ├── Abstractions/
│   │   └── ICustomerService.cs
│   └── CustomerService.cs
│
├── Data/                         ← ⑤ 基础设施层：数据库 / API / 文件
│   ├── Abstractions/
│   │   └── ICustomerRepository.cs
│   └── FakeCustomerRepository.cs
│
├── Common/                       ← 横切关注点（谁都能用，但它不碰业务）
│   ├── Constants/
│   │   └── AppConstants.cs
│   ├── Extensions/
│   │   └── StringExtensions.cs
│   └── Helpers/
│
└── Resources/                    ← 图标、图片（设计器引用）
```

**一键生成这个结构**：

```powershell
powershell -ExecutionPolicy Bypass -File tools\new-winforms-scaffold.ps1 -Target . -Name MyApp
```

---

## 3. 每层是干什么的

用"人体"类比最好记：**Models 是名词，Services 是动词和规则，Data 是手脚，Presenters 是大脑，Forms/Views 是脸。**

| 文件夹 | 一句话 | 类比 | 允许依赖 | 典型内容 |
|:---|:---|:---|:---|:---|
| `Forms/` | 窗体的**布局**与事件转发 | 脸 | Presenters、Models、Views | `MainForm` |
| `Views/` | 可复用界面区块 | 表情 | Presenters、Models | `CustomerListView` |
| `Presenters/` | 响应事件、调服务、驱动界面 | 大脑 | Services、Models | `CustomerListPresenter` |
| `Models/` | **数据结构**，不含行为逻辑 | 名词 | **无** | `Customer`、枚举、事件参数 |
| `Services/` | **业务规则**：能对数据做什么 | 动词 | Models、Data 的接口 | `ICustomerService` |
| `Data/` | 数据从哪来、存到哪 | 手脚 | Models | `ICustomerRepository` |
| `Common/` | 常量、扩展方法、工具 | 工具腰带 | 无业务依赖 | `AppConstants` |
| `Resources/` | 图标、图片、本地化 | 衣服 | — | `.resx`、图片 |

**关键区分**（新手最容易搞混）：

- **Models vs Services**：`Customer` 是 Models（"客户有什么字段"）；"客户名不能为空、邮箱必须含 @"是 Services（规则）。
- **Services vs Data**：Services 说"要哪些客户"；Data 负责"怎么从数据库/API 拿"。
- **Presenter vs Form**：Form 说"按钮被点了"；Presenter 决定"点了该干什么"。

---

## 4. 依赖方向

**唯一允许的方向**（箭头 = "可以调用"）：

```
        ┌────────────────────────────────────────┐
        │   Forms/            Views/             │  ① 表现层
        │   画界面、收集输入、转发事件            │     可以引用 WinForms
        └───────────────────┬────────────────────┘
                            │ 调用
        ┌───────────────────▼────────────────────┐
        │   Presenters/                          │  ② 表现逻辑层
        │   响应事件、调服务、更新界面            │     ⛔ 不引用 WinForms
        └───────────────────┬────────────────────┘
                            │ 调用
        ┌───────────────────▼────────────────────┐
        │   Services/                            │  ④ 应用层
        │   业务规则、用例编排                    │     ⛔ 不引用 WinForms
        └───────────────────┬────────────────────┘
                            │ 调用（只调接口）
        ┌───────────────────▼────────────────────┐
        │   Data/                                │  ⑤ 基础设施层
        │   数据库 / API / 文件                   │     ⛔ 不引用 WinForms
        └────────────────────────────────────────┘

        ┌────────────────────────────────────────┐
        │   Models/                              │  ③ 领域层
        │   被上面所有层引用，自己谁也不引用       │     ⛔ 不引用任何层
        └────────────────────────────────────────┘

        ┌────────────────────────────────────────┐
        │   Common/                              │  横切
        │   谁都可以用；它不引用任何业务层         │
        └────────────────────────────────────────┘
```

### 四条硬规则

1. **`Models/` 不引用任何其他文件夹。** 纯 C#，零依赖。
2. **`Forms/`、`Views/` 之外不允许引用 WinForms。** 这是 §0 那条规则。
3. **只能逐层向下调用，不能反向。** Services 不许 `new MainForm()`，Models 不许引用 Services。
4. **跨层只依赖接口，不依赖实现。** Presenter 依赖 `ICustomerService`；`Program.cs` 负责把 `CustomerService` 装配进去。

### 第 4 条为什么重要（新手最该懂的一点）

```csharp
// ❌ 直接 new 实现 → Presenter 和数据库绑死，无法单元测试，无法换数据源
private readonly CustomerService _service = new CustomerService();

// ✅ 依赖接口，由外部注入 → 测试时塞一个假的，上线时塞真的
public CustomerListPresenter(ICustomerListView view, ICustomerService service)
```

这叫**依赖注入**，但不用记这个词。记住：**"我需要的那个东西，让别人给我，别自己造。"**
`Program.cs` 就是那个"别人"（组装根）：

```csharp
// Program.cs —— 全项目唯一 new 具体实现的地方
var repository = new FakeCustomerRepository();       // Data 层实现
ICustomerService service = new CustomerService(repository);  // Services 层实现
Application.Run(new MainForm(service));              // 交给界面
```

### ⚠️ 依赖注入与设计器的冲突（必读）

给窗体加带参构造函数，会**和 VS 设计器打架**——设计器需要一个无参构造来实例化窗体。
标准解法是**提供两个构造函数**：

```csharp
public partial class MainForm : Form
{
    private CustomerListPresenter _presenter;

    /// <summary>设计器专用：VS 设计器走这个。只跑 InitializeComponent。</summary>
    public MainForm()
    {
        InitializeComponent();
    }

    /// <summary>运行时专用：由 Program.cs 注入服务。</summary>
    public MainForm(ICustomerService service) : this()   // ← 链到无参构造
    {
        if (service == null) throw new ArgumentNullException(nameof(service));

        // 设计期守卫
        if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;

        _presenter = new CustomerListPresenter(customerListView, service);
    }
}
```

**三个要点**：

1. **无参构造必须存在**，且只做 `InitializeComponent()`。设计器只走这一条路。
2. **带参构造用 `: this()` 链过去**，避免重复调用 `InitializeComponent()`（否则控件建两遍、事件订阅翻倍）。
3. **`UserControl` 同理**，但它更严格——设计器会真的实例化 UserControl 来托管它，所以**带参构造基本不可行**。
   UserControl 需要依赖时，用属性注入或让 Presenter 从外部把数据推进来。

示例工程 `samples/WinFormsSkillDemo/Forms/MainForm.cs` 就是这个写法。

---

## 5. 「这个文件该放哪？」速查表

**拿不准就看这张表，从上往下找第一条匹配的。**

| 我要写的东西 | 放哪 | 示例文件名 |
|:---|:---|:---|
| 窗体长什么样（控件、布局） | `Forms/XxxForm.Designer.cs` | `MainForm.Designer.cs` |
| 窗体的事件转发 | `Forms/XxxForm.cs` | `MainForm.cs` |
| 一个可复用的界面区块 | `Views/XxxView.cs` + `.Designer.cs` | `CustomerListView.cs` |
| 点击按钮后该干什么 | `Presenters/XxxPresenter.cs` | `CustomerListPresenter.cs` |
| 界面需要暴露给 Presenter 的能力 | `Presenters/IXxxView.cs` | `ICustomerListView.cs` |
| 数据结构（实体、DTO） | `Models/` | `Customer.cs` |
| 枚举、事件参数 | `Models/` | `CustomerEventArgs.cs` |
| 业务规则、校验、计算 | `Services/` | `CustomerService.cs` |
| 业务能力的接口 | `Services/Abstractions/` | `ICustomerService.cs` |
| 数据库 / API / 文件读写 | `Data/` | `SqlCustomerRepository.cs` |
| 数据访问接口 | `Data/Abstractions/` | `ICustomerRepository.cs` |
| 常量、配置键 | `Common/Constants/` | `AppConstants.cs` |
| 扩展方法 | `Common/Extensions/` | `StringExtensions.cs` |
| 通用工具方法 | `Common/Helpers/` | `CsvHelper.cs` |
| 图标、图片 | `Resources/` | `logo.png` |
| 程序入口、依赖装配 | `Program.cs` | `Program.cs` |

**找不到匹配的？** 说明这个文件职责不清，先想清楚它属于哪一层，再放。**别新建一个 `Misc/` 或 `Utils/` 当垃圾桶。**

---

## 6. 命名与命名空间

**命名空间跟随文件夹**（一条规则省掉所有纠结）：

| 文件夹 | 命名空间 | 类名 |
|:---|:---|:---|
| `Forms/` | `MyApp.Forms` | `XxxForm` |
| `Views/` | `MyApp.Views` | `XxxView` / `XxxPanel` |
| `Presenters/` | `MyApp.Presenters` | `XxxPresenter` / `IXxxView` |
| `Models/` | `MyApp.Models` | `Customer` / `XxxEventArgs` |
| `Services/` | `MyApp.Services` | `XxxService` / `IXxxService` |
| `Services/Abstractions/` | `MyApp.Services.Abstractions` | `IXxxService` |
| `Data/` | `MyApp.Data` | `XxxRepository` |
| `Data/Abstractions/` | `MyApp.Data.Abstractions` | `IXxxRepository` |
| `Common/Constants/` | `MyApp.Common.Constants` | `AppConstants` |
| `Common/Extensions/` | `MyApp.Common.Extensions` | `StringExtensions` |

其余命名约定（控件前缀 `btn`/`txt`/`lbl`、事件处理器 `控件名_事件名`）见
[layout-and-decomposition.md](./layout-and-decomposition.md) §8。

---

## 7. 方案 B：多项目解决方案（进阶）

**同一个分层，只是把文件夹升级成项目**，让编译器替你把关。

```
MyApp.sln
├── src/
│   ├── MyApp.Domain/              ← 实体、枚举、领域规则（= Models，可含领域服务）
│   │   └── MyApp.Domain.csproj
│   ├── MyApp.Application/         ← 用例、服务接口（= Services）
│   │   └── MyApp.Application.csproj
│   ├── MyApp.Infrastructure/      ← 数据访问实现（= Data）
│   │   └── MyApp.Infrastructure.csproj
│   └── MyApp.WinForms/            ← 界面（= Forms + Views + Presenters + Common）
│       ├── Forms/
│       ├── Views/
│       ├── Presenters/
│       └── MyApp.WinForms.csproj
└── tests/
    ├── MyApp.Application.Tests/
    └── MyApp.Infrastructure.Tests/
```

**项目引用规则**（`A → B` 表示 A 引用 B）：

| 项目 | 引用 | 目标框架 |
|:---|:---|:---|
| `MyApp.Domain` | *无* | `net8.0`（**不需要 `-windows`**） |
| `MyApp.Application` | Domain | `net8.0` |
| `MyApp.Infrastructure` | Application, Domain | `net8.0` |
| `MyApp.WinForms` | Application, Domain | `net8.0-windows` + `UseWindowsForms` |
| 测试项目 | 被测项目 | `net8.0` |

**要点**：

- **`Domain` / `Application` / `Infrastructure` 用 `net8.0`，不是 `net8.0-windows`。** 一旦它们编译不过，就说明你偷偷引了 WinForms——这是**编译器级别的分层保证**，比任何规范都硬。
- **`WinForms` 项目不引用 `Infrastructure`。** 只在 `Program.cs` 里引用一次来装配：
  ```csharp
  // MyApp.WinForms/Program.cs
  var repository = new SqlCustomerRepository(connectionString);   // Infrastructure
  ICustomerService service = new CustomerService(repository);      // Application
  Application.Run(new MainForm(service));
  ```
  这样界面层永远看不到数据库细节。
- **`Infrastructure` 里的类必须是 `public`**，否则 `WinForms` 装配不了。

### 从方案 A 迁移到 B

1. 在解决方案里新建 4 个项目（上面的名字）。
2. 把 `Models/` 的文件**剪切**到 `MyApp.Domain/`，命名空间 `MyApp.Models` → `MyApp.Domain`（IDE 的"重命名"能一次改完）。
3. `Services/` → `MyApp.Application/`；`Data/` → `MyApp.Infrastructure/`；其余（`Forms/`、`Views/`、`Presenters/`、`Common/`）→ `MyApp.WinForms/`。
4. 按上表加项目引用。
5. 编译。**报错的地方就是原来违反依赖方向的地方**——这正是迁移的价值。
6. 把 `Domain`/`Application` 的 `TargetFramework` 改成 `net8.0`，再编译一次。又报错的地方就是偷用 WinForms 的地方。

---

## 8. 新手友好的三个台阶

**别一步到位。** 按台阶走，每步都能编译能运行。

### 台阶 1：目录先对，逻辑暂时留在 Form 里

新手可以先在 `Forms/XxxForm.cs` 里写逻辑，但**目录结构必须是 §2 的样子**。
唯一要守的硬规则：`Models/` 和 `Services/` 里不出现 `using System.Windows.Forms;`。

```csharp
// Forms/MainForm.cs —— 台阶 1：逻辑还在 Form 里，但目录已经分好
private async void btnRefresh_Click(object sender, EventArgs e)
{
    btnRefresh.Enabled = false;
    try
    {
        var customers = await _service.LoadAsync();
        customerListView.SetCustomers(customers);
    }
    finally { btnRefresh.Enabled = true; }
}
```

### 台阶 2：逻辑搬到 Presenter

Form 只留"转发 + 显示"，逻辑进 `Presenters/XxxPresenter.cs`。

```csharp
// Forms/MainForm.cs —— 台阶 2：Form 变成壳
private void btnRefresh_Click(object sender, EventArgs e) => _presenter.Refresh();

// Presenters/CustomerListPresenter.cs —— 逻辑在这里，可以单元测试
public async Task RefreshAsync()
{
    IsBusy = true; RaiseStateChanged();
    try { _view.SetCustomers(await _service.LoadAsync()); }
    finally { IsBusy = false; RaiseStateChanged(); }
}
```

### 台阶 3：Presenter 面向 View 接口

让 Presenter 不依赖具体窗体，可以脱离 UI 测试。见 [logic-and-events.md](./logic-and-events.md) §9 和示例工程 `Presenters/ICustomerListView.cs`。

> **台阶 1 已经合格。** 台阶 2/3 是"更好"，不是"必须"。**目录结构对了，项目就已经是可维护的。**

---

## 9. 反模式（看到就该改）

| 反模式 | 为什么错 | 怎么改 |
|:---|:---|:---|
| `Models/` 里 `using System.Windows.Forms;` | 领域模型绑死 UI，无法复用/测试 | 把 UI 相关代码挪到 `Views/` |
| `Utils/`、`Misc/`、`Helper/` 垃圾桶文件夹 | 职责不清，最后什么都往里扔 | 按 §5 速查表归位 |
| `Services/CustomerService.cs` 里 `MessageBox.Show(...)` | 业务层弹窗，无法测试、无法换 UI | 返回结果或抛异常，由 `Views/` 决定怎么显示 |
| 所有代码平铺在项目根目录 | 几十个文件找不到东西 | 按 §2 建目录 |
| `Forms/` 直接 `new SqlCustomerRepository()` | 界面绑死数据库实现 | 依赖 `ICustomerRepository`，在 `Program.cs` 装配 |
| 一个 `MainForm.cs` 2000 行 | 改一处要读全文件 | 拆 `Views/`（见 layout §7） |
| 文件夹建了但全是空的 | 结构没落地 | 用 scaffold 脚本一次建对 |
| 反向引用：`Services/` 引用 `Forms/` | 循环依赖，架构腐化 | 用事件或接口回调，方向只能向下 |

---

## 10. 自动检查

```powershell
powershell -ExecutionPolicy Bypass -File tools\check-structure.ps1 -Root .\MyApp
```

检查项：

1. **目录完整性**：`Forms/`、`Views/`、`Presenters/`、`Models/`、`Services/`、`Data/`、`Common/` 是否齐全（缺哪个报哪个）
2. **依赖方向**：`Models/`、`Services/`、`Data/`、`Presenters/`、`Common/` 里是否出现 `using System.Windows.Forms;` 或 `System.Windows.Forms.` 用法
3. **Designer 配对**：每个 `Forms/XxxForm.cs` 是否有对应的 `XxxForm.Designer.cs`，反之亦然
4. **命名空间一致性**：文件所在文件夹是否与 `namespace` 匹配
5. **垃圾桶文件夹**：是否出现 `Utils/`、`Misc/`、`Helpers/`（根级）

退出码 `0` = 通过，`1` = 有问题。

---

## 11. 一句话总结

> **文件夹就是架构图。**
> 把文件放对位置，依赖只朝一个方向走，`Models/` 和 `Services/` 里永远看不到 `using System.Windows.Forms;`——
> 做到这三件事，你的 WinForms 项目就已经比大多数同类项目干净了。
