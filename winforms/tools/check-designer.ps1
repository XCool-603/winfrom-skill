# 自检脚本：核对 Designer.cs 的「三件套」与事件处理器一致性。
#
# 用法：
#   powershell -ExecutionPolicy Bypass -File check-designer.ps1 -Root <skill 目录>
#
# 检查项：
#   1. 控件字段：声明 / new / Controls.Add 三件套是否齐全
#   2. InitializeComponent() 内是否有违反往返契约的写法
#   3. 结构要点：partial / 方法签名 / Dispose / region / AutoScaleMode
#   4. 事件订阅与逻辑文件里的处理器是否一一对应
#   5. 逻辑文件引用的控件名是否真实存在
#   6. 花括号配平
#
# 退出码：0 = 无问题，1 = 有问题
#
# 注意：本文件必须以 UTF-8 with BOM 保存，否则 Windows PowerShell 5.1 会按 ANSI 读取而乱码。

param(
    [string]$Root = $PSScriptRoot
)

$ErrorActionPreference = 'Stop'

$script:problems = 0
$script:checked = 0

function Report([string]$msg) {
    $script:problems++
    Write-Host ("  [问题] {0}" -f $msg) -ForegroundColor Yellow
}

# 读取文件：强制 UTF-8，避免 PS 5.1 按 ANSI 读 UTF-8 文件导致中文乱码
function Read-Text([string]$path) {
    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
}

# 去掉 // 行注释（够用即可；Designer.cs 里没有含 // 的字符串字面量）
function Strip-Comments([string]$text) {
    return ($text -split "`n" | ForEach-Object { $_ -replace '//.*$', '' }) -join "`n"
}

# 从 openBraceIndex 处的 '{' 开始做花括号配平，返回方法体（不含最外层花括号）
function Get-Body([string]$text, [int]$openBraceIndex) {
    $depth = 0
    for ($i = $openBraceIndex; $i -lt $text.Length; $i++) {
        $ch = $text[$i]
        if ($ch -eq '{') { $depth++ }
        elseif ($ch -eq '}') {
            $depth--
            if ($depth -eq 0) {
                return $text.Substring($openBraceIndex + 1, $i - $openBraceIndex - 1)
            }
        }
    }
    return $null
}

function Get-LineNumber([string]$text, [int]$index) {
    return ($text.Substring(0, $index) -split "`n").Count
}

# 控件字段命名约定：camelCase（小写字母开头）。PascalCase 的是窗体自身属性（ClientSize 等）。
$controlName = '^[a-z][A-Za-z0-9]*$'

# 逻辑文件里可能引用的控件名前缀
$prefixes = 'btn|txt|lbl|chk|rdo|cmb|lst|dgv|tv|pnl|grp|tab|tpg|flow|root|split|menu|tsm|status|pb|prg|num|dtp|err|tip|tmr|bs|col|img|lvw'

$files = Get-ChildItem -Path $Root -Recurse -File |
    Where-Object { $_.Name -like '*.Designer.cs' -or $_.Name -like '*.Designer.cs.template' } |
    Sort-Object FullName

foreach ($f in $files) {
    $script:checked++
    $rel = $f.FullName.Substring($Root.Length).TrimStart('\')
    Write-Host ("检查 {0}" -f $rel) -ForegroundColor Cyan

    $raw  = Read-Text $f.FullName
    $code = Strip-Comments $raw

    # ---------- 判定 Form 还是 UserControl ----------
    $isUserControl = $false
    $logicPath = $null
    if ($f.Name -like '*.Designer.cs') {
        $logicPath = Join-Path $f.DirectoryName ($f.Name -replace '\.Designer\.cs$', '.cs')
    }

    if ($logicPath -and (Test-Path -LiteralPath $logicPath)) {
        $logicRaw = Read-Text $logicPath
        if ($logicRaw -match ':\s*UserControl\b') { $isUserControl = $true }
        elseif ($logicRaw -match ':\s*Form\b')    { $isUserControl = $false }
    }
    elseif ($f.Name -like 'UserControl*') { $isUserControl = $true }

    # ---------- 1. 三件套 ----------
    $declared = @{}
    foreach ($m in [regex]::Matches($code, '(?m)^\s*private\s+([\w\.]+)\s+([A-Za-z_]\w*)\s*;')) {
        $name = $m.Groups[2].Value
        if ($name -eq 'components') { continue }
        if ($name -notmatch $controlName) { continue }
        $declared[$name] = $m.Groups[1].Value
    }

    $constructed = @{}
    foreach ($m in [regex]::Matches($code, 'this\.([a-z][A-Za-z0-9]*)\s*=\s*new\s')) {
        $constructed[$m.Groups[1].Value] = $true
    }

    $added = @{}
    foreach ($m in [regex]::Matches($code, '\.Controls\.Add\(this\.([a-z][A-Za-z0-9]*)')) {
        $added[$m.Groups[1].Value] = $true
    }
    foreach ($m in [regex]::Matches($code, '(?s)Columns\.AddRange\((.*?)\);')) {
        foreach ($c in [regex]::Matches($m.Groups[1].Value, 'this\.([a-z][A-Za-z0-9]*)')) {
            $added[$c.Groups[1].Value] = $true
        }
    }

    foreach ($name in ($declared.Keys | Sort-Object)) {
        if (-not $constructed.ContainsKey($name)) {
            Report "字段 '$name' 已声明但从未 new —— 运行时 NullReferenceException"
        }
    }
    foreach ($name in ($constructed.Keys | Sort-Object)) {
        if (-not $declared.ContainsKey($name)) {
            Report "'this.$name = new ...' 没有对应的字段声明"
        }
    }
    foreach ($name in ($declared.Keys | Sort-Object)) {
        if (-not $added.ContainsKey($name)) {
            Report "控件 '$name' 不在任何 Controls.Add / Columns.AddRange 中 —— 不会显示，设计器保存时会被删掉"
        }
    }

    # ---------- 2. InitializeComponent 内部：往返契约 ----------
    $ic = [regex]::Match($code, 'private\s+void\s+InitializeComponent\s*\(\s*\)')
    if (-not $ic.Success) {
        Report '缺少 private void InitializeComponent()'
    }
    else {
        $brace = $code.IndexOf('{', $ic.Index + $ic.Length)
        $body  = Get-Body $code $brace
        if ($null -eq $body) {
            Report 'InitializeComponent() 花括号不配平'
        }
        else {
            $banned = @(
                @{ P = '(?m)^\s*(for|foreach|while|switch)\s*\(';                    D = '循环 / switch' },
                @{ P = '(?m)^\s*if\s*\(';                                            D = 'if 条件' },
                @{ P = '=>';                                                         D = 'lambda / 表达式体' },
                @{ P = '\$"';                                                        D = '字符串插值' },
                @{ P = '(?m)^\s*(var|string|int|bool|double)\s+[A-Za-z_]\w*\s*=';    D = '局部变量' },
                @{ P = 'new\s+[\w\.]+\s*\{';                                         D = '对象初始化器' },
                @{ P = '\b(File|Directory|MessageBox)\s*\.|HttpClient|SqlConnection|Environment\.Exit'; D = '设计期副作用' },
                @{ P = 'nameof\s*\(';                                                D = 'nameof()' },
                @{ P = '\?\?';                                                       D = 'null 合并运算符' }
            )
            foreach ($b in $banned) {
                foreach ($m in [regex]::Matches($body, $b.P)) {
                    $line = Get-LineNumber $raw ($brace + $m.Index)
                    Report "第 $line 行（InitializeComponent 内）出现「$($b.D)」—— 违反往返契约"
                }
            }
        }
    }

    # ---------- 3. 结构要点 ----------
    if ($code -notmatch 'partial\s+class')                    { Report '缺少 partial class' }
    if ($code -notmatch 'protected\s+override\s+void\s+Dispose\s*\(\s*bool') { Report '缺少 Dispose(bool) 重写' }
    if ($code -notmatch '#endregion')                         { Report '缺少 #endregion' }
    if ($code -notmatch 'AutoScaleMode\s*=\s*System\.Windows\.Forms\.AutoScaleMode\.Font') {
        Report '缺少 AutoScaleMode = AutoScaleMode.Font'
    }

    if ($isUserControl) {
        if ($code -match 'region\s+Windows\s+Form\s+Designer') { Report 'UserControl 的 region 名应为 "Component Designer generated code"' }
        if ($code -match 'this\.ClientSize')                    { Report 'UserControl 不应有 ClientSize' }
        if ($code -match 'this\.StartPosition')                 { Report 'UserControl 不应有 StartPosition' }
    }
    else {
        if ($code -notmatch 'region\s+Windows\s+Form\s+Designer\s+generated\s+code') {
            Report 'Form 缺少 "Windows Form Designer generated code" region'
        }
        if ($code -notmatch 'this\.ClientSize') { Report 'Form 缺少 ClientSize' }
    }

    # ---------- 4/5. 跨文件核对 ----------
    if ($logicPath -and (Test-Path -LiteralPath $logicPath)) {
        $logicName = [System.IO.Path]::GetFileName($logicPath)
        $logicCode = Strip-Comments (Read-Text $logicPath)

        foreach ($m in [regex]::Matches($code, '\+=\s*new\s+[\w\.]+\s*\(\s*this\.([A-Za-z_]\w*)\s*\)')) {
            $handler = $m.Groups[1].Value
            if ($logicCode -notmatch ('void\s+' + [regex]::Escape($handler) + '\s*\(')) {
                Report "订阅了事件处理器 '$handler'，但 $logicName 里没有该方法"
            }
        }

        $pattern = '(?<![\w.])((?:' + $prefixes + ')[A-Z][A-Za-z0-9]*)(?![\w])'
        $referenced = [regex]::Matches($logicCode, $pattern) |
            ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
        foreach ($name in $referenced) {
            if (-not $declared.ContainsKey($name)) {
                Report "$logicName 引用了 '$name'，但 $($f.Name) 里没有该控件字段"
            }
        }
    }

    # ---------- 6. 花括号配平 ----------
    $open  = ([regex]::Matches($code, '\{')).Count
    $close = ([regex]::Matches($code, '\}')).Count
    if ($open -ne $close) { Report "花括号不配平：{ = $open, } = $close" }
}

Write-Host ''
if ($script:problems -eq 0) {
    Write-Host ("通过：检查了 $script:checked 个 Designer 文件，未发现问题。") -ForegroundColor Green
    exit 0
}
Write-Host ("检查了 $script:checked 个 Designer 文件，发现 $script:problems 个问题。") -ForegroundColor Yellow
exit 1
