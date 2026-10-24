# 重新编译 桌面键盘.exe（使用系统自带的 .NET Framework 编译器，无需安装 SDK）
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { throw "找不到 csc.exe：$csc" }

$cscArgs = @(
    '/nologo'
    '/target:winexe'
    '/platform:anycpu'
    '/optimize+'
    '/codepage:65001'
    '/utf8output'
    '/r:System.dll'
    '/r:System.Drawing.dll'
    '/r:System.Windows.Forms.dll'
    ('/out:' + (Join-Path $root '桌面键盘.exe'))
)
$icon = Join-Path $root '桌面键盘.ico'
if (Test-Path $icon) { $cscArgs += ('/win32icon:' + $icon) }
$cscArgs += (Join-Path $root 'src\KeyStats.cs')

& $csc @cscArgs
if ($LASTEXITCODE -ne 0) { throw "编译失败（exit $LASTEXITCODE）" }
Write-Host "编译完成：$root\桌面键盘.exe"
