# Build Windows x64 publish folder for Inno Setup
# Run: .\installer\build-windows.ps1

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "MPKDocumentsMAUI\MPKDocumentsMAUI\MPKDocumentsMAUI.csproj"
$outDir = Join-Path $repoRoot "publish\MPKDocumentsMAUI-win-x64"

Write-Host "Publishing MAUI (Windows x64, self-contained)..." -ForegroundColor Cyan
Write-Host "Project: $project"
Write-Host "Output:  $outDir"

dotnet publish $project `
    -f net9.0-windows10.0.19041.0 `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -p:PublishSingleFile=false `
    -p:WindowsPackageType=None `
    -o $outDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$exe = Join-Path $outDir "MPKDocumentsMAUI.exe"
if (-not (Test-Path $exe)) {
    throw "Missing executable: $exe"
}

Write-Host ""
Write-Host "Done: $exe" -ForegroundColor Green
Write-Host "Next: compile installer\MPKDocuments.iss in Inno Setup (F9)." -ForegroundColor Yellow
