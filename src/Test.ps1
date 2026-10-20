param([switch]$IncludeLive)
$ErrorActionPreference = 'Stop'
$sourceRoot = Split-Path -Parent $PSScriptRoot
$outputDirectory = Join-Path $sourceRoot 'artifacts\tests'
$fixture = Join-Path $outputDirectory 'fixture'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
New-Item -ItemType Directory -Path (Join-Path $fixture 'Config'),(Join-Path $fixture 'Photos') -Force | Out-Null
# 普通检查使用临时配置。 / Regular checks use a fixture, separate from the runtime configuration.
$text = [IO.File]::ReadAllText((Join-Path $sourceRoot 'runtime\Config\PhotoFrame.example.inc'))
[IO.File]::WriteAllText((Join-Path $fixture 'Config\PhotoFrame.inc'),$text.Replace('__PHOTO_LIBRARY__',(Join-Path $fixture 'Photos')),[Text.Encoding]::Unicode)
$cases = @('SettingsTests','VisualTests','PortableTests')
if ($IncludeLive) { $cases += 'LiveTests' }
foreach ($case in $cases) {
    $files = 'SettingsStore.cs','Program.cs','Theme.cs','SettingsForm.Design.cs',($case+'.cs') | ForEach-Object { Join-Path $PSScriptRoot $_ }
    $exe = Join-Path $outputDirectory ($case+'.exe')
    & $compiler /nologo /target:exe /codepage:65001 ("/main:PhotoFrameSettings."+$case) ("/out:"+$exe) ("/win32manifest:"+(Join-Path $PSScriptRoot 'app.manifest')) /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll $files
    if ($LASTEXITCODE -ne 0) { throw ($case+' 编译失败 / Compile failed.') }
    $testRoot=$fixture
    if ($case -eq 'LiveTests') { $testRoot=Join-Path $sourceRoot 'runtime' }
    & $exe $testRoot $outputDirectory
    if ($LASTEXITCODE -ne 0) { throw ($case+' 检查未通过 / Check failed.') }
}
