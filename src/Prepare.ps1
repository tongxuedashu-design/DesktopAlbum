param([string]$RainmeterDirectory)
$ErrorActionPreference = 'Stop'
$sourceRoot = Split-Path -Parent $PSScriptRoot
$appRoot = Join-Path $sourceRoot 'runtime'
$engine = Join-Path $appRoot 'Engine\Rainmeter'
# 指定独立的官方 64 位便携目录。 / Supply a separate official 64-bit portable installation.
if (-not (Test-Path -LiteralPath (Join-Path $engine 'Rainmeter.exe'))) {
    if (-not $RainmeterDirectory) { throw '请指定 -RainmeterDirectory。 / Supply -RainmeterDirectory.' }
    foreach ($file in @('Rainmeter.exe','Rainmeter.dll','Plugins\ActionTimer.dll','Plugins\FileView.dll','Plugins\QuotePlugin.dll')) {
        if (-not (Test-Path -LiteralPath (Join-Path $RainmeterDirectory $file))) { throw ('缺少文件 / Missing file: '+$file) }
    }
    New-Item -ItemType Directory -Path $engine -Force | Out-Null
    Get-ChildItem -LiteralPath $RainmeterDirectory -Force | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $engine $_.Name) -Recurse -Force
    }
}
$library = Join-Path $appRoot 'Photos'
New-Item -ItemType Directory -Path $library -Force | Out-Null
$settings = Join-Path $appRoot 'Config\PhotoFrame.inc'
if (-not (Test-Path -LiteralPath $settings)) {
    $text = [IO.File]::ReadAllText((Join-Path $appRoot 'Config\PhotoFrame.example.inc'))
    [IO.File]::WriteAllText($settings,$text.Replace('PhotosPath=__PHOTO_LIBRARY__',('PhotosPath='+$library)),[Text.Encoding]::Unicode)
}
$config = Join-Path $appRoot 'Config\Rainmeter.ini'
if (-not (Test-Path -LiteralPath $config)) {
    $text = [IO.File]::ReadAllText((Join-Path $appRoot 'Config\Rainmeter.example.ini'))
    [IO.File]::WriteAllText($config,$text.Replace('__SKIN_PATH__',((Join-Path $appRoot 'Config\Skins')+'\')),[Text.Encoding]::Unicode)
}
if (-not (Test-Path -LiteralPath (Join-Path $engine 'Rainmeter.ini'))) {
    [IO.File]::WriteAllText((Join-Path $engine 'Rainmeter.ini'),"[Rainmeter]`r`n",[Text.Encoding]::Unicode)
}
& (Join-Path $PSScriptRoot 'Build.ps1')
if ($LASTEXITCODE -ne 0) { throw '编译失败。 / Build failed.' }
Write-Output '准备完成。 / Runtime prepared.'
Write-Output 'Run: .\runtime\DesktopAlbum.exe --start-frame'
