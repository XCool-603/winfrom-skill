# winfrom-skill

> 让 AI 写 WinForms 时**界面代码不堆进构造函数**、**项目目录不糊成一团**的规范技能。
> 兼容 DeepSeek Harness / Claude Skills 的 `SKILL.md` 目录包格式。

两条核心约束：

1. 界面代码必须落在 `*.Designer.cs` 的 `InitializeComponent()` 里，且能被 Visual Studio 设计器**往返打开与保存**。
2. 项目必须有**清晰的分层目录结构**，依赖只能单向向下。

---

## 解决什么问题

用 AI 写 WinForms，最常见的失败不是"跑不起来"，而是**跑得起来，但项目没法维护了**：

```csharp
// ❌ 问题 1：界面堆在构造函数里 —— 编译通过、运行正常，但 VS 设计器永久失效
public MainForm()
{
    InitializeComponent();
    var btn = new Button { Text = "保存", Dock = DockStyle.Bottom };
    btn.Click += (s, e) => Save();
    Controls.Add(btn);
}
```

```
❌ 问题 2：所有 .cs 平铺在项目根目录 —— 一个文件夹都没有
MyApp/
├── MyApp.csproj
├── Program.cs
├── MainForm.cs            ← 界面 + 业务 + 数据访问 全混在一起
├── MainForm.Designer.cs
├── CustomerListView.cs
├── Customer.cs
├── CustomerService.cs
├── SqlHelper.cs
├── StringHelper.cs
└── ...（再来 30 个文件）
```

两个问题的后果一样：**人没法接手**。前者人拖不动控件，后者人找不到文件。

WinForms 的设计器不是"解析"你的代码，而是**重新生成**你的代码。所以：

- 设计器不认识的东西（循环、lambda、局部变量、对象初始化器），保存时会**静默消失**；
- 控件没被 `Controls.Add` 进组件图，设计器里**看不到**，保存时还会被**删掉**；
- 把逻辑写进 `Designer.cs`，下次保存就**被抹掉**。

本技能把这些隐性规则写成 AI 必须遵守的硬约束 + **可自动核查**的清单和脚本。

---

## 六条铁律

| # | 铁律 |
|:--|:---|
| **R1** | 界面代码只写在 `*.Designer.cs` 的 `InitializeComponent()` 内 |
| **R2** | Designer.cs 必须设计器往返安全（禁用循环 / lambda / 局部变量 / 对象初始化器 / 设计期副作用） |
| **R3** | 业务逻辑只写在 `*.cs`，Designer.cs 里零逻辑 |
| **R4** | 优先用布局容器（`TableLayoutPanel` / `Dock` / `Anchor`），不写死 `Location` / `Size` |
| **R5** | 界面复杂就拆 `UserControl`，每个都带自己的 Designer.cs |
| **R6** | **先建目录结构，再写代码；依赖只能单向向下** |

---

## 目录规范与分层架构

### 标准目录（照抄即可）

```
MyApp/
├── MyApp.csproj
├── Program.cs                    ← 组装根：全项目唯一 new 具体实现的地方
│
├── Forms/                        ← ① 表现层：窗体（布局 + 事件转发）
├── Views/                        ← ① 表现层：可复用界面区块（UserControl）
├── Presenters/                   ← ② 表现逻辑层：每个 View 一个 Presenter
├── Models/                       ← ③ 领域层：实体、枚举、事件参数
├── Services/                     ← ④ 应用层：业务规则、用例
│   └── Abstractions/                 服务接口
├── Data/                         ← ⑤ 基础设施层：数据库 / API / 文件
│   └── Abstractions/                 仓储接口
├── Common/                       ← 横切：常量、扩展方法、工具
│   ├── Constants/
│   ├── Extensions/
│   └── Helpers/
└── Resources/                    ← 图标、图片
```

**一键生成**：

```powershell
powershell -ExecutionPolicy Bypass -File winforms\tools\new-winforms-scaffold.ps1 -Name MyApp
```

### 唯一必须记住的规则

> ### 🚨 只有 `Forms/` 和 `Views/` 允许出现 `using System.Windows.Forms;`
> `Models/`、`Services/`、`Data/`、`Presenters/`、`Common/` 里**一律不允许**。

这条规则把"分层"从口号变成**能自动验证的事实**。用 `check-structure.ps1` 一跑就知道有没有违反。

### 依赖方向

```
Forms/Views  →  Presenters  →  Services  →  Data
                    ↓              ↓          ↓
                  Models  ←────────+──────────+
```

只能向下调用。跨层依赖**接口**，不依赖实现——由 `Program.cs` 装配：

```csharp
// Program.cs —— 全项目唯一 new 具体实现的地方
ICustomerRepository repository = new FakeCustomerRepository();   // ⑤ Data
ICustomerService service = new CustomerService(repository);      // ④ Services
Application.Run(new MainForm(service));                          // ① 表现层
```

**换成真实数据库时，只改上面第一行，界面 / Presenter / Service 一行都不用动。**

### 「这个文件该放哪？」

| 我要写的东西 | 放哪 |
|:---|:---|
| 窗体布局 | `Forms/XxxForm.Designer.cs` |
| 窗体事件转发 | `Forms/XxxForm.cs` |
| 可复用的界面区块 | `Views/XxxView.cs` + `.Designer.cs` |
| 点击按钮后该干什么 | `Presenters/XxxPresenter.cs` |
| 数据结构、枚举、事件参数 | `Models/` |
| 业务规则、校验、计算 | `Services/` |
| 数据库 / API / 文件读写 | `Data/` |
| 常量 | `Common/Constants/` |
| 扩展方法 | `Common/Extensions/` |
| 通用工具 | `Common/Helpers/` |
| 图标、图片 | `Resources/` |

### 对新手友好：三个台阶

**别一步到位。** 按台阶走，每步都能编译能运行。

| 台阶 | 做法 | 说明 |
|:---|:---|:---|
| **1. 目录先对** | 逻辑暂时留在 `Forms/` 里，但目录结构必须建对 | **已经合格**。唯一硬规则：`Models/`、`Services/` 里不出现 `using System.Windows.Forms;` |
| **2. 逻辑搬进 Presenter** | Form 只留"转发 + 显示" | 逻辑可单元测试 |
| **3. Presenter 面向 View 接口** | Presenter 不依赖具体窗体 | 脱离 UI 也能测试 |

> 新手死在**过度设计**上的概率，远高于死在架构不够"高级"上。
> 别一上来就搭四层多项目解决方案——**目录结构对了，项目就已经可维护了**。

### 进阶：多项目解决方案

同一套分层，把文件夹升级成项目，让**编译器**替你把关：

```
MyApp.sln
├── src/
│   ├── MyApp.Domain/          net8.0（不需要 -windows）
│   ├── MyApp.Application/     net8.0
│   ├── MyApp.Infrastructure/  net8.0
│   └── MyApp.WinForms/        net8.0-windows + UseWindowsForms
└── tests/
```

关键技巧：**`Domain` / `Application` / `Infrastructure` 用 `net8.0` 而不是 `net8.0-windows`**。
一旦编译不过，就说明你偷偷引了 WinForms——这是编译器级别的分层保证。

完整规则与从单项目迁移的步骤见 [`winforms/references/project-structure.md`](winforms/references/project-structure.md) §7。

---

## 两种协作模式

长期维护的项目，**默认走模式 A**。

| | **模式 A：人拖界面，AI 写逻辑** | **模式 B：AI 生成 Designer.cs** |
|:--|:---|:---|
| 谁写 Designer.cs | 人在 VS 设计器里拖 | AI 手写完整 `InitializeComponent()` |
| AI 的职责 | 只写 `*.cs`：事件、校验、绑定、异步 | Designer.cs **和** `*.cs` |
| 对 Designer.cs | **只读**，绝不重写 | 必须逐条满足往返契约 |
| 适合 | 长期演进、多人协作、交付客户维护 | 原型、无 VS 环境、批量生成相似界面 |

**混合模式（推荐给复杂项目）**：AI 生成 Designer.cs，但同步按 R5 拆成若干 `UserControl`，每个只有 5~10 个控件——短到人一眼看懂、随时能接管。

---

## 仓库结构

```
winfrom-skill/
├── README.md                        ← 本文件
└── winforms/                        ← 技能包（可直接作为 skills 根使用）
    ├── SKILL.md                     ← 技能主体：六条铁律、模式、强制模板、工作流
    ├── references/
    │   ├── project-structure.md     ← 目录规范、分层架构、依赖方向、速查表、新手台阶
    │   ├── designer-cs-contract.md  ← 核心：往返契约、范例、Nullable 实测表、故障对照表
    │   ├── layout-and-decomposition.md  ← 布局配方、UserControl 拆分、命名、DPI
    │   ├── logic-and-events.md      ← 事件、async、跨线程、数据绑定、资源释放、MVP
    │   └── review-checklist.md      ← 自动化扫描 + 逐项清单 + 12 个翻车案例
    ├── tools/
    │   ├── new-winforms-scaffold.ps1  ← 一键生成标准目录骨架
    │   ├── check-structure.ps1        ← 校验目录完整性与依赖方向
    │   └── check-designer.ps1         ← 校验 Designer.cs 三件套与往返契约
    ├── assets/                      ← 可直接改名的骨架模板（Form / UserControl）
    └── samples/WinFormsSkillDemo/   ← 可运行的分层示例工程
```

---

## 安装

### 方式一：作为 Skill 安装（推荐）

```bash
git clone https://github.com/XCool-603/winfrom-skill.git
```

把 `winforms/` 放进任意被扫描的技能根目录：

| 作用范围 | 目标路径 |
|:---|:---|
| 用户级（所有项目） | `~/.dsh/skills/winforms/`（Windows：`%USERPROFILE%\.dsh\skills\winforms\`） |
| 项目级 | `<项目根>/.dsh/skills/winforms/` |

```powershell
# Windows 用户级安装
Copy-Item -Recurse -Force .\winfrom-skill\winforms "$env:USERPROFILE\.dsh\skills\winforms"
```

也可以用 `customSkillDirs` 直接把仓库根指过去（`winforms/SKILL.md` 会被识别为名为 `winforms` 的技能）。

### 方式二：不装技能，当文档 + 脚本用

直接读 `winforms/references/`，或把三个 `tools/*.ps1` 拷进项目当 CI 检查。

---

## 使用

装上后，跟 AI 提 WinForms 需求时它会自动加载本技能。也可以显式点名：

> 用 `winforms` 技能，帮我建一个新项目 / 加一个客户编辑窗体

技能会引导 AI 先建目录结构、确认走模式 A 还是模式 B，再按契约产出，最后跑两个自检脚本。

---

## 脚手架

一条命令生成符合规范的分层骨架（**生成即可 `dotnet run`**）：

```powershell
powershell -ExecutionPolicy Bypass -File winforms\tools\new-winforms-scaffold.ps1 -Name MyApp -Target .
```

生成 7 个分层目录 + `csproj` + `Program.cs` + 可编译的 `MainForm` + 项目 `README.md`（含分层说明和速查表）。

---

## 自检工具

两个脚本，**这类问题不该靠人眼看**：

```powershell
# ① 目录结构与依赖方向
powershell -ExecutionPolicy Bypass -File winforms\tools\check-structure.ps1 -Root .\MyApp

# ② Designer.cs 三件套与往返契约
powershell -ExecutionPolicy Bypass -File winforms\tools\check-designer.ps1 -Root .\MyApp
```

| 脚本 | 检查项 |
|:---|:---|
| `check-structure.ps1` | 目录完整性、根目录平铺、垃圾桶文件夹、**业务层是否偷用 WinForms**、Models 纯净度、Designer 配对、命名空间跟随文件夹 |
| `check-designer.ps1` | 每个控件的三件套（字段 / `new` / `Controls.Add`）、`InitializeComponent()` 内是否违反往返契约、事件订阅与处理器是否一一对应、结构要点、花括号配平 |

退出码 `0` = 通过，`1` = 有问题。

---

## 示例工程

`winforms/samples/WinFormsSkillDemo/` 是一个**完整分层**的可运行工程：

```powershell
cd winforms\samples\WinFormsSkillDemo
dotnet build
dotnet run
```

它演示了：标准分层目录、业务层零 WinForms 依赖、依赖注入与组装根、DI 与设计器共存的双构造函数、Presenter 面向 View 接口、UserControl 拆分、`TableLayoutPanel` 布局、`IProgress<T>` 进度、防重入、`BindingList<T>` 绑定、资源释放。

**主窗体 `MainForm` 只有 6 个控件**，`.cs` 里没有一行业务逻辑——业务全在 `CustomerListPresenter`，数据全在 `FakeCustomerRepository`。

---

## 验证状态

写文档不谎称验证过。当前状态：

| 项目 | 状态 | 证据 |
|:---|:---|:---|
| 示例工程编译 | ✅ | `dotnet build`（net8.0-windows）→ 0 警告 0 错误 |
| 示例启动不崩 | ✅ | 启动进程存活，主窗口标题正确读出 |
| 示例目录与分层 | ✅ | `check-structure.ps1` 通过：7 个分层目录齐全、业务层零 WinForms 依赖、Models 只依赖 System、Designer 配对完整 |
| 脚手架脚本 | ✅ | 生成的工程 `dotnet build` → 0 警告 0 错误，两个检查脚本均通过 |
| 骨架模板编译 | ✅ | 替换占位符后单独建工程编译 → 0 警告 0 错误 |
| 两个检查脚本 | ✅ | 对示例全部通过；并用**故意写坏的样例反向验证**过（`check-designer` 抓出 7 个、`check-structure` 抓出 4 个问题） |
| Nullable 指引 | ✅ | 按文档配方在 `<Nullable>enable</Nullable>` 下编译 → 0 警告 |
| **VS 设计器往返** | ❌ **未验证** | 编写环境没有 Visual Studio，无法打开设计器并保存。请自行按 `samples/WinFormsSkillDemo/README.md` 的步骤确认 |

> **编译通过 ≠ 往返安全。** 前者只证明语法和 API 正确；后者要靠设计器真的保存一次才能证明。

---

## 参考

- [Windows Forms 设计器](https://learn.microsoft.com/dotnet/desktop/winforms/designer/)
- [High DPI support in Windows Forms](https://learn.microsoft.com/dotnet/desktop/winforms/high-dpi-support-in-windows-forms)
- [TableLayoutPanel 概述](https://learn.microsoft.com/dotnet/desktop/winforms/controls/tablelayoutpanel-overview)

---

## License

MIT
