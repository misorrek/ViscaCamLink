<#
.SYNOPSIS
    Builds and packages ViscaCamLink using Velopack (vpk).

.DESCRIPTION
    Optionally runs the tests, downloads the previous release from GitHub (so vpk can create delta
    packages), publishes the app as a self-contained win-x64 build and packages it with vpk pack.
    vpk produces in ./releases/velopack/:
      - ViscaCamLink.App-win-Setup.exe      (installer)
      - ViscaCamLink.App-win-Portable.zip   (self-updating Velopack portable zip)
      - the full .nupkg, a delta .nupkg (from 1.0.1 onward) and the releases feed files
    Nothing is uploaded; the upload command is printed at the end.

.PARAMETER Version
    The semantic version for this release (e.g., "1.0.0").

.PARAMETER Channel
    The release channel (default: "win").

.PARAMETER ReleaseNotes
    Path to a markdown file passed to vpk pack --releaseNotes (shown in the update dialog).

.PARAMETER SkipTests
    Skip running the unit tests before publishing.

.EXAMPLE
    .\Build-Velopack.ps1 -Version "1.0.0" -ReleaseNotes .\Docs\RELEASE-NOTES-1.0.0.md
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [string]$Channel = "win",
    [string]$ReleaseNotes,
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"

$repoUrl      = "https://github.com/misorrek/ViscaCamLink"
$projectDir   = "$PSScriptRoot\ViscaCamLink"
$testProject  = "$PSScriptRoot\ViscaCamLink.Tests\ViscaCamLink.Tests.csproj"
$publishDir   = "$PSScriptRoot\publish"
$releasesDir  = "$PSScriptRoot\releases\velopack"

if ($ReleaseNotes -and -not (Test-Path $ReleaseNotes)) {
    Write-Error "Release notes file not found: $ReleaseNotes"
    exit 1
}

Write-Host "=== ViscaCamLink Velopack Build ===" -ForegroundColor Cyan
Write-Host "Version: $Version"
Write-Host "Channel: $Channel"
Write-Host ""

# Step 1: Tests
if ($SkipTests) {
    Write-Host "[1/5] Tests skipped." -ForegroundColor DarkGray
} else {
    Write-Host "[1/5] Running tests..." -ForegroundColor Yellow
    dotnet test $testProject -c Release

    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet test failed with exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }
}

Write-Host ""

# Step 2: Previous release for delta packages
if (-not (Test-Path $releasesDir)) {
    New-Item -ItemType Directory -Path $releasesDir -Force | Out-Null
}

Write-Host "[2/5] Downloading the previous release (for delta packages)..." -ForegroundColor Yellow
vpk download github --repoUrl $repoUrl --channel $Channel --outputDir $releasesDir

if ($LASTEXITCODE -ne 0) {
    # Expected for the very first release (nothing published yet); vpk pack then creates a full package only.
    Write-Warning "vpk download github failed (exit code $LASTEXITCODE); continuing without delta package."
}

Write-Host ""

# Step 3: Publish
if (Test-Path $publishDir) {
    Remove-Item -Recurse -Force $publishDir
}

Write-Host "[3/5] Publishing ViscaCamLink..." -ForegroundColor Yellow
dotnet publish $projectDir\ViscaCamLink.csproj `
    -c Release `
    --self-contained `
    -r win-x64 `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host ""

# Step 4: Package
Write-Host "[4/5] Packaging with vpk..." -ForegroundColor Yellow
$packArguments = @(
    "pack",
    "--packId", "ViscaCamLink.App",
    "--packTitle", "ViscaCamLink",
    "--packAuthors", "Max Groiser",
    "--packVersion", $Version,
    "--packDir", $publishDir,
    "--mainExe", "ViscaCamLink.exe",
    "--icon", "$projectDir\Resources\program.ico",
    "--channel", $Channel,
    "--outputDir", $releasesDir
)

if ($ReleaseNotes) {
    $packArguments += @("--releaseNotes", (Resolve-Path $ReleaseNotes).Path)
}

& vpk @packArguments

if ($LASTEXITCODE -ne 0) {
    Write-Error "vpk pack failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host ""

# Step 5: Summary (upload is manual)
Write-Host "[5/5] Done! Releases are in: $releasesDir" -ForegroundColor Cyan
Write-Host ""
Write-Host "To upload to GitHub Releases, run:" -ForegroundColor DarkGray
Write-Host "  vpk upload github --repoUrl $repoUrl --token `$env:GITHUB_TOKEN --outputDir $releasesDir --channel $Channel --tag v$Version --releaseName `"ViscaCamLink $Version`" --publish" -ForegroundColor DarkGray
