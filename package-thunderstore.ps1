[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GameDir,

    [Parameter(Mandatory = $true)]
    [string]$ProfileDir,

    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$game = (Resolve-Path -LiteralPath $GameDir).Path
$profile = (Resolve-Path -LiteralPath $ProfileDir).Path
$manifestPath = Join-Path $PSScriptRoot 'thunderstore\manifest.json'
$iconPath = Join-Path $PSScriptRoot 'thunderstore\icon.png'
$readmePath = Join-Path $PSScriptRoot 'thunderstore\README.md'
$changelogPath = Join-Path $PSScriptRoot 'CHANGELOG.md'
$projectPath = Join-Path $PSScriptRoot 'Stonewards.ParticleOptimizer.csproj'
$pluginSource = Join-Path $PSScriptRoot 'src\ParticleOptimizerPlugin.cs'
$builtDll = Join-Path $PSScriptRoot 'bin\Release\netstandard2.1\Stonewards.ParticleOptimizer.dll'

foreach ($path in @($manifestPath, $iconPath, $readmePath, $changelogPath, $projectPath, $pluginSource)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required package file missing: $path"
    }
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
[xml]$project = Get-Content -LiteralPath $projectPath -Raw
$projectVersion = [string]$project.Project.PropertyGroup.Version
$sourceText = Get-Content -LiteralPath $pluginSource -Raw
if ($sourceText -notmatch 'public const string PluginVersion = "([^"]+)"') {
    throw 'Cannot determine plugin version from ParticleOptimizerPlugin.cs'
}
$pluginVersion = $Matches[1]
if ($manifest.version_number -ne $projectVersion -or $manifest.version_number -ne $pluginVersion) {
    throw "Version mismatch: manifest=$($manifest.version_number), project=$projectVersion, plugin=$pluginVersion"
}
if ([string]::IsNullOrWhiteSpace([string]$manifest.name)) {
    throw 'Thunderstore manifest name must not be empty.'
}

Add-Type -AssemblyName System.Drawing
$iconImage = [System.Drawing.Image]::FromFile($iconPath)
try {
    if ($iconImage.Width -ne 256 -or $iconImage.Height -ne 256) {
        throw "Thunderstore icon must be exactly 256x256, got $($iconImage.Width)x$($iconImage.Height)."
    }
}
finally {
    $iconImage.Dispose()
}

if (-not $SkipBuild) {
    Write-Host 'Building and installing the plugin...' -ForegroundColor Cyan
    $builder = Join-Path $PSScriptRoot 'build-and-install.ps1'
    & powershell -NoProfile -ExecutionPolicy Bypass -File $builder -GameDir $game -ProfileDir $profile
    if ($LASTEXITCODE -ne 0) {
        throw "Plugin build failed (exit code $LASTEXITCODE). ZIP was not created."
    }
}

if (-not (Test-Path -LiteralPath $builtDll)) {
    throw "Compiled DLL not found: $builtDll"
}

$distDir = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Path $distDir -Force | Out-Null
$zipPath = Join-Path $distDir ("thereisnouser-{0}-{1}.zip" -f $manifest.name, $manifest.version_number)

$files = @(
    @{ Source = $manifestPath; Target = 'manifest.json' },
    @{ Source = $readmePath; Target = 'README.md' },
    @{ Source = $iconPath; Target = 'icon.png' },
    @{ Source = $changelogPath; Target = 'CHANGELOG.md' },
    @{ Source = $builtDll; Target = 'BepInEx/plugins/StonewardsParticleOptimizer/Stonewards.ParticleOptimizer.dll' }
)

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

$zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in $files) {
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $zip,
            [string]$file.Source,
            [string]$file.Target,
            [System.IO.Compression.CompressionLevel]::Optimal
        ) | Out-Null
    }
}
finally {
    $zip.Dispose()
}

$check = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $entries = @($check.Entries | ForEach-Object { $_.FullName })
    foreach ($file in $files) {
        if ($entries -notcontains [string]$file.Target) {
            throw "Missing ZIP entry: $($file.Target)"
        }
    }
}
finally {
    $check.Dispose()
}

Write-Host ""
Write-Host "Thunderstore package created:" -ForegroundColor Green
Get-Item -LiteralPath $zipPath | Select-Object FullName, Length, LastWriteTime
Write-Host 'Review the ZIP contents before uploading to Thunderstore.'
