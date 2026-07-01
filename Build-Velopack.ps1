<#
.SYNOPSIS
    Builds and packages ViscaCamLink using Velopack (vpk).

.DESCRIPTION
    Publishes the ViscaCamLink app as a self-contained win-x64 executable,
    then packages it with vpk to produce:
      - A full NUPKG installer
      - Delta packages (on subsequent builds)
      - A RELEASES JSON file
    All output goes to ./releases/

.PARAMETER Version
    The semantic version for this release (e.g., "1.0.0").

.PARAMETER Channel
    The release channel (default: "win"). Used for multi-channel setups.

.EXAMPLE
    .\Build-Velopack.ps1 -Version "1.0.0"
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [string]$Channel = "win"
)

$ErrorActionPreference = "Stop"

$projectDir   = "$PSScriptRoot\ViscaCamLink"
$publishDir   = "$PSScriptRoot\publish"
$releasesDir  = "$PSScriptRoot\releases\velopack"

Write-Host "=== ViscaCamLink Velopack Build ===" -ForegroundColor Cyan
Write-Host "Version: $Version"
Write-Host "Channel: $Channel"
Write-Host ""

# Step 1: Clean publish directory
if (Test-Path $publishDir) {
    Remove-Item -Recurse -Force $publishDir
}

# Step 2: Publish the app (self-contained, single-file not used — Velopack handles bundling)
Write-Host "[1/3] Publishing ViscaCamLink..." -ForegroundColor Yellow
dotnet publish $projectDir\ViscaCamLink.csproj `
    -c Release `
    --self-contained `
    -r win-x64 `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "[1/3] Publish complete." -ForegroundColor Green
Write-Host ""

# Step 3: Ensure releases directory exists
if (-not (Test-Path $releasesDir)) {
    New-Item -ItemType Directory -Path $releasesDir -Force | Out-Null
}

# Step 4: Package with vpk
Write-Host "[2/3] Packaging with vpk..." -ForegroundColor Yellow
vpk pack `
    --packId "ViscaCamLink.App" `
    --packTitle "ViscaCamLink" `
    --packAuthors "Max Groiser" `
    --packVersion $Version `
    --packDir $publishDir `
    --mainExe "ViscaCamLink.exe" `
    --icon "$projectDir\Resources\program.ico" `
    --channel $Channel `
    --outputDir $releasesDir

if ($LASTEXITCODE -ne 0) {
    Write-Error "vpk pack failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host "[2/3] Packaging complete." -ForegroundColor Green
Write-Host ""

# Step 5: Summary
Write-Host "[3/3] Done! Releases are in: $releasesDir" -ForegroundColor Cyan
Write-Host ""
Write-Host "To upload to GitHub Releases, run:" -ForegroundColor DarkGray
Write-Host "  vpk upload github --repoUrl https://github.com/misorrek/ViscaCamLink --token `$env:GITHUB_TOKEN --outputDir $releasesDir --channel $Channel" -ForegroundColor DarkGray
