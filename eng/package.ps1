[CmdletBinding()]
param(
    [string] $Configuration = "Release",
    [string] $Version = "0.1.0",
    [string[]] $RuntimeIdentifiers = @(
        "win-x64",
        "linux-x64",
        "osx-x64",
        "osx-arm64"
    )
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$appProject = Join-Path $repoRoot "src/FeatureProof.App/FeatureProof.App.csproj"
$artifactsRoot = Join-Path $repoRoot "artifacts"
$publishRoot = Join-Path $artifactsRoot "publish"
$packageRoot = Join-Path $artifactsRoot "package/FeatureProof-$Version"
$archivePath = Join-Path $artifactsRoot "FeatureProof-$Version.zip"

if (Test-Path $packageRoot) {
    Remove-Item $packageRoot -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $publishRoot | Out-Null
New-Item -ItemType Directory -Force -Path $packageRoot | Out-Null

foreach ($rid in $RuntimeIdentifiers) {
    $ridPublish = Join-Path $publishRoot $rid
    $ridPackage = Join-Path $packageRoot $rid

    if (Test-Path $ridPublish) {
        Remove-Item $ridPublish -Recurse -Force
    }

    dotnet publish $appProject `
        --configuration $Configuration `
        --runtime $rid `
        --self-contained true `
        --output $ridPublish `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:DebugType=None `
        -p:DebugSymbols=false

    Copy-Item $ridPublish $ridPackage -Recurse

    if ($rid.StartsWith("win-")) {
        $launcher = @"
@echo off
set ASPNETCORE_URLS=http://localhost:5077
echo Starting FeatureProof at http://localhost:5077
start "" http://localhost:5077
FeatureProof.App.exe
"@
        Set-Content -Path (Join-Path $ridPackage "run-featureproof.cmd") -Value $launcher -Encoding ascii
    }
    else {
        $launcher = @"
#!/usr/bin/env sh
set -eu
export ASPNETCORE_URLS=http://localhost:5077
echo "Starting FeatureProof at http://localhost:5077"
if command -v xdg-open >/dev/null 2>&1; then xdg-open http://localhost:5077 >/dev/null 2>&1 || true; fi
if command -v open >/dev/null 2>&1; then open http://localhost:5077 >/dev/null 2>&1 || true; fi
DIR=`$(CDPATH= cd -- "`$(dirname -- "`$0")" && pwd)
exec "`$DIR/FeatureProof.App"
"@
        $launcherPath = Join-Path $ridPackage "run-featureproof.sh"
        Set-Content -Path $launcherPath -Value $launcher -Encoding ascii
    }
}

$readme = @"
# FeatureProof $Version

This archive contains self-contained FeatureProof builds. Users do not need to install .NET.

Choose the folder for your OS:

- win-x64: Windows 64-bit
- linux-x64: Linux 64-bit
- osx-x64: macOS Intel
- osx-arm64: macOS Apple Silicon

Run the launcher in that folder, then open http://localhost:5077 if the browser does not open automatically.

On macOS or Linux, you may need to run:

chmod +x FeatureProof.App run-featureproof.sh

The sample .fproof files are intentionally not included in this distribution.
"@

Set-Content -Path (Join-Path $packageRoot "README.txt") -Value $readme -Encoding ascii

$includedSamples = Get-ChildItem $packageRoot -Recurse -File -Filter "*.fproof"
if ($includedSamples.Count -gt 0) {
    throw "Package includes .fproof files, which are not allowed in team distribution artifacts."
}

if (Test-Path $archivePath) {
    Remove-Item $archivePath -Force
}

Compress-Archive -Path (Join-Path $packageRoot "*") -DestinationPath $archivePath -Force
Write-Host "Created $archivePath"
