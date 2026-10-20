param([string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$sourceRoot = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $sourceRoot 'runtime' }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$files = 'SettingsStore.cs','Program.cs','Theme.cs','SettingsForm.Design.cs' | ForEach-Object { Join-Path $PSScriptRoot $_ }
# 使用 Windows 自带的编译器。 / Use the compiler included with Windows.
& $compiler /nologo /target:winexe /optimize+ /codepage:65001 ("/win32icon:" + (Join-Path $PSScriptRoot 'Assets\DesktopAlbum.ico')) ("/out:" + (Join-Path $OutputDirectory 'DesktopAlbum.exe')) ("/win32manifest:" + (Join-Path $PSScriptRoot 'app.manifest')) /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll $files
if ($LASTEXITCODE -ne 0) { throw '编译失败。 / Build failed.' }
Write-Output (Join-Path $OutputDirectory 'DesktopAlbum.exe')
