# Publish + compile Inno Setup installer
# Run: .\installer\build-installer.ps1

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$csproj = Join-Path $repoRoot "MPKDocumentsMAUI\MPKDocumentsMAUI\MPKDocumentsMAUI.csproj"
$iss = Join-Path $repoRoot "installer\MPKDocuments.iss"
$iscc = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"

function Get-CsprojValue([string]$tag) {
    if (-not (Test-Path $csproj)) { return $null }
    $xml = [xml](Get-Content $csproj -Raw)
    $node = $xml.Project.PropertyGroup.$tag | Select-Object -First 1
    if ($node) { return "$node".Trim() }
    return $null
}

$version = Get-CsprojValue "ApplicationDisplayVersion"
if (-not $version) { $version = "1.0.0" }

& (Join-Path $repoRoot "installer\build-windows.ps1")

if (-not (Test-Path $iscc)) {
    throw "Inno Setup not found: $iscc"
}

Write-Host ""
Write-Host "Compiling installer v$version ..." -ForegroundColor Cyan
& $iscc "/DMyAppVersion=$version" $iss

if ($LASTEXITCODE -ne 0) {
    throw "ISCC failed with exit code $LASTEXITCODE"
}

$setup = Join-Path $repoRoot "installer\output\MPKDocuments-Setup-$version.exe"
Write-Host ""
Write-Host "Installer ready: $setup" -ForegroundColor Green
