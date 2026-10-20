param([string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$sourceRoot = Split-Path -Parent $PSScriptRoot
if (-not $OutputDirectory) { $OutputDirectory=Join-Path $sourceRoot 'docs\screenshots' }
$build = Join-Path $sourceRoot 'artifacts\screenshots'
New-Item -ItemType Directory -Path $build -Force | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$files = 'SettingsStore.cs','Program.cs','Theme.cs','SettingsForm.Design.cs','ReleaseScreenshots.cs' | ForEach-Object { Join-Path $PSScriptRoot $_ }
$exe = Join-Path $build 'ReleaseScreenshots.exe'
& $compiler /nologo /target:exe /codepage:65001 /main:PhotoFrameSettings.ReleaseScreenshots ("/out:"+$exe) ("/win32manifest:"+(Join-Path $PSScriptRoot 'app.manifest')) /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll $files
if ($LASTEXITCODE -ne 0) { throw '截图工具编译失败。 / Screenshot tool compilation failed.' }
# 演示图不读取个人照片或桌面。 / Demonstrations do not read personal photos or capture the desktop.
& $exe $OutputDirectory
if ($LASTEXITCODE -ne 0) { throw '演示图生成失败。 / Screenshot generation failed.' }
