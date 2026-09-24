# winfrom-skill

> 一个让 AI 写 WinForms 时**不把界面代码堆进构造函数**的规范技能。
> 兼容 DeepSeek Harness / Claude Skills 的 `SKILL.md` 目录包格式。

**核心约束**：界面代码必须落在 `*.Designer.cs` 的 `InitializeComponent()` 里，且必须能被 Visual Studio 设计器**往返打开与保存**（round-trip）。

---

## 解决什么问题

用 AI 写 WinForms，最常见的失败不是"代码跑不起来"，而是**代码跑得起来，但设计器再也打不开了**：

```csharp
// ❌ AI 很爱这么写 —— 编译通过、运行正常，但 VS 设计器永久失效
public MainForm()
{
    InitializeComponent();
    var btn = new Button { Text = "保存", Dock = DockStyle.Bottom };
    btn.Click += (s, e) => Save();
    Controls.Add(btn);
}
```

一旦这么做，这个窗体就只能靠读代码猜界面了：**人没法拖控件、没法看效果、没法接手维护**。这是 AI 辅助 WinForms 开发最大的长期债务。

WinForms 的设计器不是"解析"你的代码，而是**重新生成**你的代码。所以：

- 设计器不认识的东西（循环、lambda、局部变量、对象初始化器），保存时会**静默消失**；
- 控件没被 `Controls.Add` 进组件图，设计器里**看不到**，保存时还会被**删掉**；
- 把逻辑写进 `Designer.cs`，下次保存就**被抹掉**。

本技能把这些隐性规则写成 AI 必须遵守的硬约束 + 可自动核查的清单。

---

## 五条铁律

| # | 铁律 |
|:--|:---|
| **R1** | 界面代码只写在 `*.Designer.cs` 的 `InitializeComponent()` 内 |
| **R2** | Designer.cs 必须设计器往返安全（禁用循环 / lambda / 局部变量 / 对象初始化器 / 设计期副作用） |
| **R3** | 业务逻辑只写在 `*.cs`，Designer.cs 里零逻辑 |
| **R4** | 优先用布局容器（`TableLayoutPanel` / `Dock` / `Anchor`），不写死 `Location` / `Size` |
| **R5** | 界面复杂就拆 `UserControl`，每个都带自己的 Designer.cs |

---

## 两种协作模式

长期维护的项目，**默认走模式 A**。

| | **模式 A：人拖界面，AI 写逻辑** | **模式 B：AI 生成 Designer.cs** |
|:--|:---|:---|
| 谁写 Designer.cs | 人在 VS 设计器里拖 | AI 手写完整 `InitializeComponent()` |
| AI 的职责 | 只写 `*.cs`：事件、校验、绑定、异步 | Designer.cs **和** `*.cs` |
| 对 Designer.cs | **只读**，绝不重写 | 必须逐条满足往返契约 |
| 适合 | 长期演进、多人协作、交付客户维护 | 原型、无 VS 环境、批量生成相似界面 |

**混合模式（推荐给复杂项目）**：AI 生成 Designer.cs，但同步按 R5 拆成若干 `UserControl`，每个只有 5~10 个控件——短到人一眼看懂、随时能接管。既拿到生成速度，又保留人的接手能力。

---

## 目录结构

```
winfrom-skill/
├── README.md                        ← 本文件
└── winforms/                        ← 技能包（可直接作为 skills 根使用）
    ├── SKILL.md                     ← 技能主体：铁律、模式、强制模板、工作流
    ├── references/
    │   ├── designer-cs-contract.md  ← 核心：往返契约、范例、Nullable 实测表、故障对照表
    │   ├── layout-and-decomposition.md  ← 布局配方、UserControl 拆分、命名、DPI
    │   ├── logic-and-events.md      ← 事件、async、跨线程、数据绑定、资源释放、轻量 MVP
    │   └── review-checklist.md      ← 自动化扫描 + 逐项清单 + 12 个翻车案例
    ├── assets/                      ← 可直接改名的骨架模板（Form / UserControl）
    ├── samples/WinFormsSkillDemo/   ← 可运行示例工程
    └── tools/check-designer.ps1     ← 自检脚本
```

---

## 安装

### 方式一：作为 Skill 安装（推荐）

克隆后把 `winforms/` 放进任意被扫描的技能根目录：

```bash
git clone https://github.com/XCool-603/winfrom-skill.git
```

| 作用范围 | 目标路径 |
|:---|:---|
| 用户级（所有项目） | `~/.dsh/skills/winforms/`（Windows：`%USERPROFILE%\.dsh\skills\winforms\`） |
| 项目级 | `<项目根>/.dsh/skills/winforms/` |

```powershell
# Windows 用户级安装
Copy-Item -Recurse -Force .\winfrom-skill\winforms "$env:USERPROFILE\.dsh\skills\winforms"
```

也可以用 `customSkillDirs` 直接把仓库根指过去（`winforms/SKILL.md` 会被识别为名为 `winforms` 的技能）。

### 方式二：不装技能，当文档用

直接读 `winforms/SKILL.md` 和 `winforms/references/`，或把 `references/review-checklist.md` 当代码评审清单贴在团队里。

---

## 使用

装上后，跟 AI 提 WinForms 需求时它会自动加载本技能。也可以显式点名：

> 用 `winforms` 技能，帮我加一个客户编辑窗体

技能会引导 AI 先确认走模式 A 还是模式 B，再按契约产出，最后跑自检清单。

---

## 自检工具

`winforms/tools/check-designer.ps1` 会自动核对机械性错误——**这类问题不该靠人眼看**：

```powershell
powershell -ExecutionPolicy Bypass -File winforms\tools\check-designer.ps1 -Root .\winforms
```

检查项：

1. **三件套**：每个控件是否都有 ① 字段声明 ② `new` ③ `Controls.Add` 到容器
2. **往返契约**：`InitializeComponent()` 内是否有循环 / lambda / 局部变量 / 对象初始化器 / 设计期副作用
3. **结构要点**：`partial`、方法签名、`Dispose`、region 名、`AutoScaleMode`
4. **跨文件一致性**：事件订阅与逻辑文件里的处理器是否一一对应；逻辑引用的控件名是否真实存在
5. 花括号配平

退出码 `0` = 通过，`1` = 有问题。

---

## 示例工程

`winforms/samples/WinFormsSkillDemo/` 是一个可直接跑的 WinForms 工程，演示推荐做法：

```powershell
cd winforms\samples\WinFormsSkillDemo
dotnet build
dotnet run
```

要点：`MainForm` 只有 6 个控件（工具栏 + 内容宿主 + 状态标签），客户列表整个落在 `Views/CustomerListView.*`——**主窗体退化成布局壳**。同时演示了设计期守卫、`IProgress<T>` 进度、防重入、`BindingList<T>` 绑定、`OnFormClosed` 清理。

---

## 验证状态

写文档不谎称验证过。当前状态：

| 项目 | 状态 | 证据 |
|:---|:---|:---|
| 示例工程编译 | ✅ | `dotnet build`（net8.0-windows）→ 0 警告 0 错误 |
| 示例启动不崩 | ✅ | 启动进程存活，主窗口标题正确读出 |
| 骨架模板编译 | ✅ | 替换占位符后单独建工程编译 → 0 警告 0 错误 |
| 自检脚本 | ✅ | 4 个 Designer 文件全部通过；并用故意写坏的样例**反向验证**过（成功抓出 7 个问题） |
| Nullable 指引 | ✅ | 按文档配方在 `<Nullable>enable</Nullable>` 下编译 → 0 警告 |
| **VS 设计器往返** | ❌ **未验证** | 编写环境没有 Visual Studio，无法打开设计器并保存。请自行按 `samples/WinFormsSkillDemo/README.md` 的 5 步流程确认 |

> **编译通过 ≠ 往返安全。** 前者只证明语法和 API 正确；后者要靠设计器真的保存一次才能证明。

---

## 参考

- [Windows Forms 设计器](https://learn.microsoft.com/dotnet/desktop/winforms/designer/)
- [High DPI support in Windows Forms](https://learn.microsoft.com/dotnet/desktop/winforms/high-dpi-support-in-windows-forms)
- [TableLayoutPanel 概述](https://learn.microsoft.com/dotnet/desktop/winforms/controls/tablelayoutpanel-overview)

---

## License

MIT
