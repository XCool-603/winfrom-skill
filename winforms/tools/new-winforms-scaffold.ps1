# 脚手架脚本：一键生成符合本技能规范的分层 WinForms 项目骨架。
#
# 用法：
#   powershell -ExecutionPolicy Bypass -File new-winforms-scaffold.ps1 -Name MyApp
#   powershell -ExecutionPolicy Bypass -File new-winforms-scaffold.ps1 -Name MyApp -Target D:\Work
#
# 生成结果（可直接 dotnet run）：
#   <Target>/<Name>/
#   ├── <Name>.csproj
#   ├── Program.cs                 组装根（唯一 new 具体实现的地方）
#   ├── README.md                  分层说明 + "文件该放哪"速查表
#   ├── Forms/MainForm.cs + .Designer.cs
#   ├── Views/
#   ├── Presenters/
#   ├── Models/
#   ├── Services/Abstractions/
#   ├── Data/Abstractions/
#   ├── Common/Constants/ | Extensions/ | Helpers/
#   └── Resources/
#
# 注意：本文件必须以 UTF-8 with BOM 保存，否则 Windows PowerShell 5.1 会按 ANSI 读取而乱码。

param(
    [Parameter(Mandatory = $true)]
    [string]$Name,

    [string]$Target = '.',

    # 默认用项目名作为根命名空间
    [string]$RootNamespace = '',

    [switch]$Force
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($RootNamespace)) { $RootNamespace = $Name }

if ($Name -notmatch '^[A-Za-z_][A-Za-z0-9_]*$') {
    Write-Host "项目名必须是合法 C# 标识符（字母/下划线开头，只含字母数字下划线）：$Name" -ForegroundColor Red
    exit 1
}

$projectDir = Join-Path (Resolve-Path -LiteralPath $Target).Path $Name

if (Test-Path -LiteralPath $projectDir) {
    if (-not $Force) {
        Write-Host "目录已存在：$projectDir" -ForegroundColor Red
        Write-Host "加 -Force 覆盖，或换一个 -Name。" -ForegroundColor Red
        exit 1
    }
    Remove-Item -LiteralPath $projectDir -Recurse -Force
}

Write-Host "生成项目骨架：$projectDir" -ForegroundColor Cyan

$utf8Bom = New-Object System.Text.UTF8Encoding($true)

function Write-File([string]$relativePath, [string]$content) {
    $full = Join-Path $projectDir $relativePath
    $dir = [System.IO.Path]::GetDirectoryName($full)
    if (-not (Test-Path -LiteralPath $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
    [System.IO.File]::WriteAllText($full, $content, $utf8Bom)
    Write-Host ("  + {0}" -f $relativePath) -ForegroundColor DarkGray
}

# ---------------- 目录 ----------------
$folders = @(
    'Forms',
    'Views',
    'Presenters',
    'Models',
    'Services\Abstractions',
    'Data\Abstractions',
    'Common\Constants',
    'Common\Extensions',
    'Common\Helpers',
    'Resources'
)
foreach ($f in $folders) {
    New-Item -ItemType Directory -Force -Path (Join-Path $projectDir $f) | Out-Null
}

# 空目录放 .gitkeep，让 git 能保留结构
foreach ($f in @('Views', 'Presenters', 'Models', 'Services\Abstractions', 'Data\Abstractions',
                 'Common\Constants', 'Common\Extensions', 'Common\Helpers', 'Resources')) {
    [System.IO.File]::WriteAllText((Join-Path $projectDir "$f\.gitkeep"), '', $utf8Bom)
}

# ---------------- csproj ----------------
$csproj = @"
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <RootNamespace>$RootNamespace</RootNamespace>
    <AssemblyName>$Name</AssemblyName>
    <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>disable</Nullable>

    <!-- DPI：这两条会生成 ApplicationConfiguration.Initialize() -->
    <ApplicationHighDpiMode>PerMonitorV2</ApplicationHighDpiMode>
    <ApplicationVisualStyles>true</ApplicationVisualStyles>
    <ApplicationUseCompatibleTextRendering>false</ApplicationUseCompatibleTextRendering>
  </PropertyGroup>

</Project>
"@
Write-File "$Name.csproj" $csproj

# ---------------- Program.cs（组装根） ----------------
$program = @"
// ============================================================================
// 程序入口 + 组装根（Composition Root）
//
// 这是全项目【唯一】new 具体实现的地方。分层规则：
//   Forms/Views -> Presenters -> Services -> Data
// 谁都不许自己 new 下层实现，都由这里装配好再传进去（依赖注入）。
// ============================================================================

using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace $RootNamespace;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        // 全局异常兜底：必须在创建任何窗口之前设置
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

        Application.ThreadException += (sender, e) =>
            MessageBox.Show(`$"发生未处理的错误：{e.Exception.Message}", "错误",
                MessageBoxButtons.OK, MessageBoxIcon.Error);

        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                MessageBox.Show(`$"发生严重错误，程序即将退出：{ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        TaskScheduler.UnobservedTaskException += (sender, e) => e.SetObserved();

        // ===================== 组装（唯一 new 实现的地方） =====================
        // 示例：接真实数据库时只改这一行，上层一行都不用动。
        //   ICustomerRepository repository = new SqlCustomerRepository(connectionString);
        //   ICustomerService service = new CustomerService(repository);
        //   Application.Run(new MainForm(service));

        Application.Run(new Forms.MainForm());
    }
}
"@
Write-File 'Program.cs' $program

# ---------------- MainForm（从 assets 模板生成） ----------------
$assetsDir = Join-Path $PSScriptRoot '..\assets'
$designerTpl = Join-Path $assetsDir 'Form.Designer.cs.template'
$logicTpl = Join-Path $assetsDir 'Form.cs.template'

if (-not (Test-Path -LiteralPath $designerTpl) -or -not (Test-Path -LiteralPath $logicTpl)) {
    Write-Host "找不到 assets 模板（期望在 $assetsDir），跳过 MainForm 生成。" -ForegroundColor Yellow
}
else {
    $map = @{
        'Namespace'    = "$RootNamespace.Forms"
        'ClassName'    = 'MainForm'
        'Title'        = $Name
        'ClientWidth'  = '784'
        'ClientHeight' = '461'
    }

    foreach ($pair in @(@{ Tpl = $designerTpl; Out = 'Forms\MainForm.Designer.cs' },
                        @{ Tpl = $logicTpl;    Out = 'Forms\MainForm.cs' })) {
        $content = [System.IO.File]::ReadAllText($pair.Tpl, [System.Text.Encoding]::UTF8)
        foreach ($k in $map.Keys) { $content = $content.Replace("{{$k}}", $map[$k]) }
        if ($content -match '\{\{') { throw "模板 $($pair.Tpl) 里还有未替换的占位符" }
        Write-File $pair.Out $content
    }
}

# ---------------- 项目 README ----------------
$readme = @"
# $Name

由 `winforms` 技能的脚手架生成。

## 目录结构

``````
$Name/
├── $Name.csproj
├── Program.cs                    组装根：全项目唯一 new 具体实现的地方
│
├── Forms/                        ① 表现层：窗体（布局 + 事件转发）
├── Views/                        ① 表现层：可复用界面区块（UserControl）
├── Presenters/                   ② 表现逻辑层：每个 View 一个 Presenter
├── Models/                       ③ 领域层：实体、枚举、事件参数
├── Services/                     ④ 应用层：业务规则、用例
│   └── Abstractions/                 服务接口
├── Data/                         ⑤ 基础设施层：数据库 / API / 文件
│   └── Abstractions/                 仓储接口
├── Common/                       横切：常量、扩展方法、工具
│   ├── Constants/
│   ├── Extensions/
│   └── Helpers/
└── Resources/                    图标、图片
``````

## 唯一必须记住的规则

> **只有 ``Forms/`` 和 ``Views/`` 允许出现 ``using System.Windows.Forms;``。**
> ``Models/``、``Services/``、``Data/``、``Presenters/``、``Common/`` 里一律不允许。

## 依赖方向

``````
Forms/Views  ->  Presenters  ->  Services  ->  Data
                     |              |           |
                  Models  <---------+-----------+
``````

只能向下调用，不能反向。跨层依赖**接口**，不依赖实现。

## 文件该放哪？

| 我要写的东西 | 放哪 |
|:---|:---|
| 窗体布局 | ``Forms/XxxForm.Designer.cs`` |
| 窗体事件转发 | ``Forms/XxxForm.cs`` |
| 可复用的界面区块 | ``Views/XxxView.cs`` + ``.Designer.cs`` |
| 点击按钮后该干什么 | ``Presenters/XxxPresenter.cs`` |
| 数据结构、枚举 | ``Models/`` |
| 业务规则、校验、计算 | ``Services/`` |
| 数据库 / API / 文件 | ``Data/`` |
| 常量 | ``Common/Constants/`` |
| 扩展方法 | ``Common/Extensions/`` |
| 通用工具 | ``Common/Helpers/`` |

完整说明见技能的 ``references/project-structure.md``。

## 自检

``````powershell
powershell -ExecutionPolicy Bypass -File <技能目录>\tools\check-structure.ps1 -Root .
powershell -ExecutionPolicy Bypass -File <技能目录>\tools\check-designer.ps1  -Root .
``````

## 下一步

1. ``dotnet build`` 确认能编译。
2. 在 VS 里双击 ``Forms\MainForm.cs`` -> 查看设计器，确认能正常渲染。
3. 按上表把新文件放进对应文件夹，再填代码。
"@
Write-File 'README.md' $readme

# ---------------- 汇总 ----------------
$fileCount = (Get-ChildItem -LiteralPath $projectDir -Recurse -File).Count
Write-Host ''
Write-Host ("完成：{0} 个文件，{1} 个目录。" -f $fileCount, $folders.Count) -ForegroundColor Green
Write-Host ''
Write-Host '下一步：' -ForegroundColor Cyan
Write-Host "  cd `"$projectDir`""
Write-Host '  dotnet build'
Write-Host '  dotnet run'
Write-Host ''
Write-Host '自检：' -ForegroundColor Cyan
Write-Host "  powershell -ExecutionPolicy Bypass -File `"$PSScriptRoot\check-structure.ps1`" -Root `"$projectDir`""
