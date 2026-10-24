[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$GameDir,

    [Parameter(Mandatory = $true)]
    [string]$ProfileDir
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$game = (Resolve-Path -LiteralPath $GameDir).Path
$profilePath = (Resolve-Path -LiteralPath $ProfileDir).Path
$project = Join-Path $PSScriptRoot 'Stonewards.ParticleOptimizer.csproj'

$managed = Join-Path $game 'Stonewards_Data\Managed'
$assembly = Join-Path $managed 'Assembly-CSharp.dll'
if (-not (Test-Path -LiteralPath $assembly)) {
    throw "Game assembly not found: $assembly"
}

$coreCandidates = @(
    (Join-Path $profilePath 'BepInEx\core'),
    (Join-Path $game 'BepInEx\core')
)
$core = $null
foreach ($candidate in $coreCandidates) {
    if ((Test-Path -LiteralPath (Join-Path $candidate 'BepInEx.dll')) -and
        (Test-Path -LiteralPath (Join-Path $candidate '0Harmony.dll'))) {
        $core = $candidate
        break
    }
}
if (-not $core) {
    throw 'BepInEx.dll / 0Harmony.dll not found in game or profile BepInEx\core. Install BepInEx 5 first.'
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'dotnet SDK not found. Install the .NET SDK and re-run this script.'
}

$env:STONEWARDS_GAME_DIR = $game
$env:STONEWARDS_BEPINEX_CORE = $core

Write-Host "Building Particle Optimizer 0.1.0..." -ForegroundColor Cyan
& dotnet build $project -c Release --nologo
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed (exit code $LASTEXITCODE)"
}

$builtDll = Join-Path $PSScriptRoot 'bin\Release\netstandard2.1\Stonewards.ParticleOptimizer.dll'
if (-not (Test-Path -LiteralPath $builtDll)) {
    throw "Build reported success but DLL is missing: $builtDll"
}

$pluginDir = Join-Path $profilePath 'BepInEx\plugins\StonewardsParticleOptimizer'
New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null
Copy-Item -LiteralPath $builtDll -Destination (Join-Path $pluginDir 'Stonewards.ParticleOptimizer.dll') -Force

Write-Host "Installed to $pluginDir" -ForegroundColor Green
Write-Host 'Launch Stonewards and check BepInEx\LogOutput.log.'
