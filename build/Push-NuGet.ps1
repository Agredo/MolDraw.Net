<#
.SYNOPSIS
    Publishes the packages from artifacts/packages to nuget.org (or a private feed).

.DESCRIPTION
    Pushes the four MolDraw.Net packages in dependency order (Depiction -> Core -> Skia -> Maui) and then their
    symbol packages (.snupkg). nuget.org routes the symbols to its symbol server automatically; a private feed has to
    be able to accept snupkg files.

    The API key can be passed with -ApiKey or set in the NUGET_API_KEY environment variable. Create it at
    https://www.nuget.org/account/apikeys with the scopes "Push new packages and package versions" and
    "Push symbol packages", glob pattern "MolDraw.*" (see docs/PUBLISHING.md).

.PARAMETER ApiKey
    nuget.org API key. Defaults to $env:NUGET_API_KEY.

.PARAMETER Version
    Version to publish. Defaults to the version in Directory.Build.props.

.PARAMETER Source
    Push endpoint. Defaults to nuget.org.

.PARAMETER SkipSymbols
    Push only the .nupkg files.

.PARAMETER Force
    Do not ask for confirmation.

.EXAMPLE
    $env:NUGET_API_KEY = 'oy2...'; pwsh ./build/Push-NuGet.ps1

.EXAMPLE
    pwsh ./build/Push-NuGet.ps1 -ApiKey $key -Source https://my.feed/nuget/v3/index.json
#>
[CmdletBinding()]
param(
    [string] $ApiKey = $env:NUGET_API_KEY,
    [string] $Version,
    [string] $Source = 'https://api.nuget.org/v3/index.json',
    [switch] $SkipSymbols,
    [switch] $Force
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path $PSScriptRoot -Parent
$packageDir = Join-Path $repoRoot 'artifacts/packages'

if (-not $Version) {
    $props = [xml](Get-Content (Join-Path $repoRoot 'Directory.Build.props'))
    $Version = @($props.SelectNodes('//PropertyGroup/Version') | ForEach-Object { $_.InnerText.Trim() })[0]
}

# Dependency order: a package is uploaded only after everything it depends on exists on the feed.
$order = @('MolDraw.Depiction', 'MolDraw.Core', 'MolDraw.Skia', 'MolDraw.Maui')

$nupkgs = foreach ($id in $order) {
    $path = Join-Path $packageDir "$id.$Version.nupkg"
    if (-not (Test-Path $path)) { throw "Missing package: $path (run ./build/Pack.ps1 first)." }
    Get-Item $path
}
$snupkgs = if ($SkipSymbols) { @() } else {
    foreach ($id in $order) {
        $path = Join-Path $packageDir "$id.$Version.snupkg"
        if (Test-Path $path) { Get-Item $path } else { Write-Warning "No symbol package for $id $Version." }
    }
}

Write-Host "Publishing MolDraw.Net $Version to $Source" -ForegroundColor Cyan
($nupkgs + $snupkgs) | Select-Object Name, @{n = 'Size (KB)'; e = { [math]::Round($_.Length / 1KB) } } |
    Format-Table -AutoSize | Out-String | Write-Host

if ($Source -like 'https://api.nuget.org*' -and [string]::IsNullOrWhiteSpace($ApiKey)) {
    throw 'A nuget.org API key is required: pass -ApiKey or set NUGET_API_KEY.'
}

if (-not $Force) {
    $answer = Read-Host "Push these $($nupkgs.Count) package(s) and $($snupkgs.Count) symbol package(s)? Published versions cannot be overwritten. [y/N]"
    if ($answer -notin 'y', 'Y', 'yes', 'Yes') { Write-Host 'Aborted.'; return }
}

foreach ($file in $nupkgs + $snupkgs) {
    Write-Host "`n==> $($file.Name)" -ForegroundColor Cyan
    $arguments = @('nuget', 'push', $file.FullName, '--source', $Source, '--skip-duplicate')
    if (-not [string]::IsNullOrWhiteSpace($ApiKey)) { $arguments += @('--api-key', $ApiKey) }
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) { throw "Push of $($file.Name) failed (exit code $LASTEXITCODE)." }
}

Write-Host "`nDone. Check https://www.nuget.org/packages/MolDraw.Maui/$Version (indexing takes a few minutes)." -ForegroundColor Green
