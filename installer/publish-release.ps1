# Build Windows installer and publish to API (admin upload)
# Usage:
#   .\installer\publish-release.ps1 -ApiBaseUrl "https://mpk-docs.ru.tuna.am" -Phone "+79148012594" -Password "secret"
# Optional env: MPK_API_BASE_URL, MPK_ADMIN_PHONE, MPK_ADMIN_PASSWORD

param(
    [string]$ApiBaseUrl = $env:MPK_API_BASE_URL,
    [string]$Phone = $env:MPK_ADMIN_PHONE,
    [string]$Password = $env:MPK_ADMIN_PASSWORD,
    [string]$Version,
    [int]$Build = 0,
    [string]$Notes = "",
    [switch]$Mandatory,
    [string]$Platform = "windows",
    [string]$InstallerPath
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$csproj = Join-Path $repoRoot "MPKDocumentsMAUI\MPKDocumentsMAUI\MPKDocumentsMAUI.csproj"
$iss = Join-Path $repoRoot "installer\MPKDocuments.iss"

function Get-CsprojValue([string]$tag) {
    if (-not (Test-Path $csproj)) { return $null }
    $xml = [xml](Get-Content $csproj -Raw)
    $node = $xml.Project.PropertyGroup.$tag | Select-Object -First 1
    if ($node) { return "$node".Trim() }
    return $null
}

if (-not $Version) { $Version = Get-CsprojValue "ApplicationDisplayVersion" }
if ($Build -lt 1) {
    $appVer = Get-CsprojValue "ApplicationVersion"
    if ($appVer -and [int]::TryParse($appVer, [ref]$null)) { $Build = [int]$appVer }
    else { $Build = 1 }
}

function Set-CsprojValue([string]$tag, [string]$value) {
    $xml = [xml](Get-Content $csproj -Raw)
    $group = $xml.Project.PropertyGroup | Where-Object { $_.$tag } | Select-Object -First 1
    if (-not $group) {
        $group = $xml.CreateElement("PropertyGroup", $xml.DocumentElement.NamespaceURI)
        $xml.Project.AppendChild($group) | Out-Null
    }
    $group.$tag = $value
    $xml.Save($csproj)
}

$curVersion = Get-CsprojValue "ApplicationDisplayVersion"
$curBuild = Get-CsprojValue "ApplicationVersion"
if ($curVersion -ne $Version -or "$curBuild" -ne "$Build") {
    Write-Host "Syncing csproj -> v$Version (build $Build) before build..." -ForegroundColor Yellow
    Set-CsprojValue "ApplicationDisplayVersion" $Version
    Set-CsprojValue "ApplicationVersion" "$Build"
}

if (-not $ApiBaseUrl) { throw "Set -ApiBaseUrl or MPK_API_BASE_URL" }
if (-not $Phone -or -not $Password) { throw "Set -Phone/-Password or MPK_ADMIN_PHONE/MPK_ADMIN_PASSWORD" }

Write-Host "Building installer for v$Version (build $Build)..." -ForegroundColor Cyan
& (Join-Path $repoRoot "installer\build-installer.ps1")

if (-not $InstallerPath) {
    $InstallerPath = Join-Path $repoRoot "installer\output\MPKDocuments-Setup-$Version.exe"
}
if (-not (Test-Path $InstallerPath)) {
    $fallback = Get-ChildItem (Join-Path $repoRoot "installer\output\*.exe") | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($fallback) { $InstallerPath = $fallback.FullName }
}
if (-not (Test-Path $InstallerPath)) {
    throw "Installer not found. Build failed or path wrong: $InstallerPath"
}

function Get-PublishedAppVersion([string]$publishDir) {
    $jsonPath = Join-Path $publishDir "appversion.json"
    if (-not (Test-Path $jsonPath)) {
        throw "В папке сборки нет appversion.json ($jsonPath). Пересоберите установщик."
    }
    $v = Get-Content $jsonPath -Raw | ConvertFrom-Json
    return [pscustomobject]@{
        Version = [string]$v.version
        Build   = [int]$v.build
    }
}

$publishDir = Join-Path $repoRoot "publish\MPKDocumentsMAUI-win-x64"
$built = Get-PublishedAppVersion $publishDir
if ($built.Version -ne $Version -or $built.Build -ne $Build) {
    throw @"
Версия внутри установщика: v$($built.Version) (build $($built.Build))
Публикуете на сервер:      v$Version (build $Build)

Сначала обновите csproj и пересоберите:
  .\installer\publish-release.ps1 -ApiBaseUrl ... -Version "$Version" -Build $Build
"@
}
Write-Host "Verified build: v$($built.Version) (build $($built.Build))" -ForegroundColor Green

Write-Host "Logging in to $ApiBaseUrl ..." -ForegroundColor Cyan
$loginBody = @{ phone_number = $Phone; password = $Password } | ConvertTo-Json
$login = Invoke-RestMethod -Method Post -Uri "$($ApiBaseUrl.TrimEnd('/'))/auth/login/password" -ContentType "application/json" -Body $loginBody
$token = $login.access_token
if (-not $token) { throw "Login failed: no access_token" }

Write-Host "Uploading $InstallerPath ..." -ForegroundColor Cyan
$boundary = [System.Guid]::NewGuid().ToString()
$fileBytes = [System.IO.File]::ReadAllBytes($InstallerPath)
$fileName = [System.IO.Path]::GetFileName($InstallerPath)

$fields = @{
    version = $Version
    build = "$Build"
    platform = $Platform
    min_build = "0"
    mandatory = $(if ($Mandatory) { "true" } else { "false" })
}
if ($Notes) { $fields.notes = $Notes }

$bodyLines = New-Object System.Collections.Generic.List[string]
foreach ($kv in $fields.GetEnumerator()) {
    $bodyLines.Add("--$boundary")
    $bodyLines.Add("Content-Disposition: form-data; name=""$($kv.Key)""")
    $bodyLines.Add("")
    $bodyLines.Add([string]$kv.Value)
}
$bodyLines.Add("--$boundary")
$bodyLines.Add("Content-Disposition: form-data; name=""file""; filename=""$fileName""")
$bodyLines.Add("Content-Type: application/octet-stream")
$bodyLines.Add("")
$header = ($bodyLines -join "`r`n") + "`r`n"
$footer = "`r`n--$boundary--`r`n"
$headerBytes = [System.Text.Encoding]::UTF8.GetBytes($header)
$footerBytes = [System.Text.Encoding]::UTF8.GetBytes($footer)
$bodyBytes = New-Object byte[] ($headerBytes.Length + $fileBytes.Length + $footerBytes.Length)
[Array]::Copy($headerBytes, 0, $bodyBytes, 0, $headerBytes.Length)
[Array]::Copy($fileBytes, 0, $bodyBytes, $headerBytes.Length, $fileBytes.Length)
[Array]::Copy($footerBytes, 0, $bodyBytes, $headerBytes.Length + $fileBytes.Length, $footerBytes.Length)

$publish = Invoke-RestMethod -Method Post `
    -Uri "$($ApiBaseUrl.TrimEnd('/'))/admin/app-release/publish" `
    -Headers @{ Authorization = "Bearer $token" } `
    -ContentType "multipart/form-data; boundary=$boundary" `
    -Body $bodyBytes

Write-Host ""
Write-Host "Published successfully!" -ForegroundColor Green
Write-Host "Download URL: $($publish.download_url)"
Write-Host "Version: $($publish.version)  Build: $($publish.build)"
