# 结构自检脚本：校验 WinForms 项目的目录分层与依赖方向。
#
# 用法：
#   powershell -ExecutionPolicy Bypass -File check-structure.ps1 -Root .\MyApp
#
# 检查项：
#   1. 目录完整性：Forms / Views / Presenters / Models / Services / Data / Common
#   2. 依赖方向：Models/Services/Data/Presenters/Common 里不允许 using System.Windows.Forms
#   3. Models 纯净：Models/ 里只允许 using System...（不引用任何其他层）
#   4. Designer 配对：Forms/XxxForm.cs 必须有 XxxForm.Designer.cs，反之亦然
#   5. 命名空间跟随文件夹
#   6. 根目录不允许平铺 .cs（Program.cs 除外）
#   7. 不允许 Utils / Misc / Helper 这类垃圾桶文件夹（Common/Helpers 合法）
#
# 退出码：0 = 通过，1 = 有问题
#
# 注意：本文件必须以 UTF-8 with BOM 保存，否则 Windows PowerShell 5.1 会按 ANSI 读取而乱码。

param(
    [Parameter(Mandatory = $true)]
    [string]$Root,

    # 允许缺失的目录，例如 -Optional @('Views','Presenters')
    [string[]]$Optional = @()
)

$ErrorActionPreference = 'Stop'

$script:problems = 0
$script:warnings = 0

function Fail([string]$msg) {
    $script:problems++
    Write-Host ("  [错误] {0}" -f $msg) -ForegroundColor Red
}

function Warn([string]$msg) {
    $script:warnings++
    Write-Host ("  [提示] {0}" -f $msg) -ForegroundColor DarkYellow
}

function Read-Text([string]$path) {
    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
}

if (-not (Test-Path -LiteralPath $Root)) {
    Write-Host "目录不存在：$Root" -ForegroundColor Red
    exit 1
}

$Root = (Resolve-Path -LiteralPath $Root).Path
Write-Host "检查项目结构：$Root" -ForegroundColor Cyan
Write-Host ''

# ---------- 找出项目根（含 .csproj 的目录） ----------
$csproj = Get-ChildItem -LiteralPath $Root -Filter *.csproj -File -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $csproj) {
    Fail "在 $Root 下找不到 .csproj"
    exit 1
}
$projectDir = $csproj.DirectoryName

# 从 csproj 推断根命名空间
$rootNs = $null
$projText = Read-Text $csproj.FullName
$m = [regex]::Match($projText, '<RootNamespace>\s*([^<\s]+)\s*</RootNamespace>')
if ($m.Success) { $rootNs = $m.Groups[1].Value }
else {
    $m = [regex]::Match($projText, '<AssemblyName>\s*([^<\s]+)\s*</AssemblyName>')
    if ($m.Success) { $rootNs = $m.Groups[1].Value }
    else { $rootNs = [System.IO.Path]::GetFileNameWithoutExtension($csproj.Name) }
}
Write-Host "根命名空间：$rootNs"

# ---------- 1. 目录完整性 ----------
$expected = @('Forms', 'Views', 'Presenters', 'Models', 'Services', 'Data', 'Common')
Write-Host ''
Write-Host '1. 目录完整性' -ForegroundColor Cyan
foreach ($folder in $expected) {
    $path = Join-Path $projectDir $folder
    if (Test-Path -LiteralPath $path) {
        $count = (Get-ChildItem -LiteralPath $path -Recurse -File -Filter *.cs).Count
        Write-Host ("   OK  {0}/  ({1} 个 .cs)" -f $folder, $count) -ForegroundColor Green
    }
    elseif ($Optional -contains $folder) {
        Warn "$folder/ 缺失（已声明为可选）"
    }
    else {
        Fail "$folder/ 缺失 —— 见 references/project-structure.md §2"
    }
}

# ---------- 2. 根目录平铺 ----------
Write-Host ''
Write-Host '2. 根目录平铺检查' -ForegroundColor Cyan
$allowedRoot = @('Program.cs', 'AssemblyInfo.cs')
$flat = Get-ChildItem -LiteralPath $projectDir -File -Filter *.cs |
    Where-Object { $allowedRoot -notcontains $_.Name }
if ($flat) {
    foreach ($f in $flat) {
        Fail "$($f.Name) 平铺在项目根目录 —— 应按职责移入 Forms/Views/Presenters/Models/Services/Data/Common"
    }
}
else {
    Write-Host '   OK  根目录只有 Program.cs' -ForegroundColor Green
}

# ---------- 3. 垃圾桶文件夹 ----------
Write-Host ''
Write-Host '3. 垃圾桶文件夹' -ForegroundColor Cyan
$junkNames = @('Utils', 'Util', 'Misc', 'Helper', 'Helpers', 'CommonHelper', 'Temp', 'Test')
$junkFound = $false
Get-ChildItem -LiteralPath $projectDir -Directory -Recurse |
    Where-Object { $junkNames -contains $_.Name -and $_.Parent.FullName -eq $projectDir } |
    ForEach-Object {
        Fail "$($_.Name)/ 是垃圾桶文件夹 —— 按 project-structure.md §5 速查表归位（Common/Helpers 是合法的）"
        $junkFound = $true
    }
if (-not $junkFound) { Write-Host '   OK  没有垃圾桶文件夹' -ForegroundColor Green }

# ---------- 收集所有 .cs ----------
$allCs = Get-ChildItem -LiteralPath $projectDir -Recurse -File -Filter *.cs |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }

function Get-Layer([string]$fullPath) {
    $rel = $fullPath.Substring($projectDir.Length).TrimStart('\', '/')
    $parts = $rel -split '[\\/]'
    if ($parts.Count -ge 2) { return $parts[0] }
    return '(root)'
}

# ---------- 4. 依赖方向：只有 Forms / Views 能用 WinForms ----------
Write-Host ''
Write-Host '4. 依赖方向（只有 Forms/ 和 Views/ 可以引用 WinForms）' -ForegroundColor Cyan
$winformsLayers = @('Models', 'Services', 'Data', 'Presenters', 'Common')
$violations = 0
foreach ($f in $allCs) {
    $layer = Get-Layer $f.FullName
    if ($winformsLayers -notcontains $layer) { continue }

    $lines = (Read-Text $f.FullName) -split "`n"
    for ($i = 0; $i -lt $lines.Count; $i++) {
        # 先去掉行注释：源码注释里出现 "System.Windows.Forms" 是合法的（说明性文字）
        $code = $lines[$i] -replace '//.*$', ''
        if ($code -match 'System\.Windows\.Forms') {
            $rel = $f.FullName.Substring($projectDir.Length).TrimStart('\', '/')
            Fail ("{0}:{1} 出现 System.Windows.Forms —— 业务层不允许依赖界面" -f $rel, ($i + 1))
            $violations++
        }
    }
}
if ($violations -eq 0) { Write-Host '   OK  业务层零 WinForms 依赖' -ForegroundColor Green }

# ---------- 5. Models 纯净 ----------
Write-Host ''
Write-Host '5. Models 纯净度（只允许 using System...）' -ForegroundColor Cyan
$modelsDir = Join-Path $projectDir 'Models'
$modelViolations = 0
if (Test-Path -LiteralPath $modelsDir) {
    foreach ($f in (Get-ChildItem -LiteralPath $modelsDir -Recurse -File -Filter *.cs)) {
        $lines = (Read-Text $f.FullName) -split "`n"
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $line = $lines[$i]
            if ($line -notmatch '^\s*using\s+([\w\.]+)\s*;') { continue }
            $ns = $Matches[1]
            if ($ns -like 'System*') { continue }
            if ($ns -like "$rootNs.Models*") { continue }
            Fail "Models/$($f.Name):$($i + 1) 引用了 '$ns' —— Models 不应依赖任何其他层"
            $modelViolations++
        }
    }
}
if ($modelViolations -eq 0) { Write-Host '   OK  Models 只依赖 System' -ForegroundColor Green }

# ---------- 6. Designer 配对 ----------
Write-Host ''
Write-Host '6. Designer 文件配对' -ForegroundColor Cyan
$pairProblems = 0
foreach ($f in $allCs) {
    if ($f.Name -like '*.Designer.cs') {
        $main = $f.FullName -replace '\.Designer\.cs$', '.cs'
        if (-not (Test-Path -LiteralPath $main)) {
            Fail "$($f.Name) 没有配对的逻辑文件 $([System.IO.Path]::GetFileName($main))"
            $pairProblems++
        }
        continue
    }

    $text = Read-Text $f.FullName
    if ($text -notmatch ':\s*(Form|UserControl)\b') { continue }

    $designer = $f.FullName -replace '\.cs$', '.Designer.cs'
    if (-not (Test-Path -LiteralPath $designer)) {
        Fail "$($f.Name) 是 Form/UserControl 但没有配对的 .Designer.cs —— 界面代码必须在 Designer.cs 里"
        $pairProblems++
    }
}
if ($pairProblems -eq 0) { Write-Host '   OK  所有 Form/UserControl 都有 Designer.cs' -ForegroundColor Green }

# ---------- 7. 命名空间跟随文件夹 ----------
Write-Host ''
Write-Host '7. 命名空间跟随文件夹' -ForegroundColor Cyan
$nsProblems = 0
foreach ($f in $allCs) {
    $layer = Get-Layer $f.FullName
    if ($layer -eq '(root)') { continue }
    if ($layer -eq 'Resources') { continue }

    $rel = $f.FullName.Substring($projectDir.Length).TrimStart('\', '/')
    $relDir = [System.IO.Path]::GetDirectoryName($rel)
    $expectedNs = $rootNs + '.' + ($relDir -replace '[\\/]', '.')

    $text = Read-Text $f.FullName
    $m = [regex]::Match($text, '(?m)^\s*namespace\s+([\w\.]+)\s*[;{]')
    if (-not $m.Success) { continue }

    $actualNs = $m.Groups[1].Value
    if ($actualNs -ne $expectedNs) {
        Fail "$rel 的命名空间是 '$actualNs'，按文件夹应为 '$expectedNs'"
        $nsProblems++
    }
}
if ($nsProblems -eq 0) { Write-Host '   OK  命名空间与文件夹一致' -ForegroundColor Green }

# ---------- 汇总 ----------
Write-Host ''
Write-Host ('扫描了 {0} 个 .cs 文件。' -f $allCs.Count)
if ($script:problems -eq 0) {
    Write-Host ("通过：0 个错误，{0} 个提示。" -f $script:warnings) -ForegroundColor Green
    exit 0
}
Write-Host ("发现 {0} 个错误，{1} 个提示。" -f $script:problems, $script:warnings) -ForegroundColor Red
exit 1
