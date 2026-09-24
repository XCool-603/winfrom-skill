# 骨架模板使用说明

这四个文件是**可直接改名使用的骨架**。用 `.template` 后缀是为了避免被误加进项目编译。

## 占位符

替换全部 `{{...}}`，然后把文件改名（去掉 `.template`）。

| 占位符 | 含义 | 示例 |
|:---|:---|:---|
| `{{Namespace}}` | 命名空间 | `Demo.Forms` / `Demo.Views` |
| `{{ClassName}}` | 类名（同时是 `Name` 属性值） | `MainForm` / `CustomerListView` |
| `{{Title}}` | 窗体标题（UserControl 无此项） | `客户管理` |
| `{{ClientWidth}}` / `{{ClientHeight}}` | 设计期初始尺寸 | `784` / `461` |

## 文件对应关系

| 模板 | 改名为 | 用途 |
|:---|:---|:---|
| `Form.Designer.cs.template` | `<ClassName>.Designer.cs` | 窗体布局 |
| `Form.cs.template` | `<ClassName>.cs` | 窗体逻辑 |
| `UserControl.Designer.cs.template` | `<ClassName>.Designer.cs` | 用户控件布局 |
| `UserControl.cs.template` | `<ClassName>.cs` | 用户控件逻辑 |

**两个文件必须同名同目录**，否则 VS 设计器不认（需要 `csproj` 里显式 `<DependentUpon>`，见 [SKILL.md](../SKILL.md) §4 第 6 步）。

## 使用步骤

1. 复制模板对，改名为 `<ClassName>.Designer.cs` 和 `<ClassName>.cs`，放进 `Forms/` 或 `Views/`。
2. 替换所有占位符。
3. **先只保留 Designer.cs 里已有的骨架**，跑一次 `dotnet build` 确认能编译。
4. 在 VS 里双击 `.cs` → "查看设计器"，确认骨架能正常渲染（**这一步验证模板没被改坏**）。
5. 再往里加控件：**三件套**——① 字段声明 ② `new` + 属性赋值 ③ `Controls.Add` 到正确父容器。
6. 加事件：① Designer.cs 里 `+= new System.EventHandler(this.X)` ② `.cs` 里 `private void X(object sender, EventArgs e)`。
7. 交付前过一遍 [references/review-checklist.md](../references/review-checklist.md)。

## 模板里已包含什么

**Form.Designer.cs**
- `components` 字段 + `Dispose(bool)` 重写
- `#region Windows Form Designer generated code`
- 三区 `TableLayoutPanel` 根布局（工具栏 / 内容 / 底部按钮条）
- 一个 `FlowLayoutPanel` 工具栏 + 两个按钮，演示正确写法
- `// TODO` 标记指示该填什么

**UserControl.Designer.cs**
- 同上，但用 `#region Component Designer generated code` 和 `Size`（**无 `ClientSize`**）
- 单列 `TableLayoutPanel` 根布局

**`.cs` 逻辑文件**
- 正确的 `using`、构造函数里 `InitializeComponent()` 的位置
- 设计期守卫（`LicenseManager.UsageMode`）
- 事件处理器骨架（签名与 Designer.cs 里订阅的一致）
- `OnFormClosed` 资源清理骨架（Form 版）

## 注意

- **模板里的控件坐标是占位值**，只为让设计器能正常渲染。加控件时**不要手工推坐标**——用 `TableLayoutPanel` 的行列或 `Dock`/`Anchor`。
- `AutoScaleDimensions = new SizeF(7F, 15F)` 对应 9pt 微软雅黑 / Segoe UI。**换项目时要抄项目里已有窗体的值**，别照抄这里。
- 模板未包含 `.resx`。需要图标/图片时**让用户在设计器里设**，不要手写 `.resx`（见 [designer-cs-contract.md](../references/designer-cs-contract.md) §6）。
