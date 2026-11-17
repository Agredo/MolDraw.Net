<#
.SYNOPSIS
    Builds, tests and packs all MolDraw.Net NuGet packages into artifacts/packages.

.DESCRIPTION
    Packs MolDraw.Depiction, MolDraw.Core, MolDraw.Skia and MolDraw.Maui for every MAUI target framework
    (-p:MolDrawAllTargets=true), which requires the maui, maui-android, maui-ios and maui-maccatalyst workloads.
    The version comes from Directory.Build.props.

.PARAMETER Configuration
    Build configuration. Defaults to Release (required for publishable packages).

.PARAMETER SkipTests
    Skip the test run. Use only when the tests were just run.

.PARAMETER NoWarn
    Warning codes to suppress. NU1507 is suppressed by default because several package sources are configured on
    developer machines; the library projects treat warnings as errors.

.EXAMPLE
    pwsh ./build/Pack.ps1

.EXAMPLE
    pwsh ./build/Pack.ps1 -SkipTests
#>
[CmdletBinding()]
param(
    [string] $Configuration = 'Release',
    [switch] $SkipTests,
    [string[]] $NoWarn = @('NU1507')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path $PSScriptRoot -Parent
$solution = Join-Path $repoRoot 'MolDraw.Net.slnx'
$outputDir = Join-Path $repoRoot 'artifacts/packages'
$noWarnArg = "-p:NoWarn=$($NoWarn -join '%3B')"

$props = [xml](Get-Content (Join-Path $repoRoot 'Directory.Build.props'))
$version = @($props.SelectNodes('//PropertyGroup/Version') | ForEach-Object { $_.InnerText.Trim() })[0]

Push-Location $repoRoot
try {
    Write-Host "MolDraw.Net $version" -ForegroundColor Cyan
    Write-Host "SDK: $(dotnet --version)"

    if (-not $SkipTests) {
        Write-Host "`n==> Test" -ForegroundColor Cyan
        dotnet test $solution -c $Configuration $noWarnArg
        if ($LASTEXITCODE -ne 0) { throw "Tests failed (exit code $LASTEXITCODE)." }
    }

    Write-Host "`n==> Clean $outputDir" -ForegroundColor Cyan
    Remove-Item $outputDir -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

    Write-Host "`n==> Pack (all MAUI target frameworks)" -ForegroundColor Cyan
    dotnet pack $solution -c $Configuration -p:MolDrawAllTargets=true $noWarnArg
    if ($LASTEXITCODE -ne 0) { throw "Pack failed (exit code $LASTEXITCODE)." }

    Write-Host "`n==> Result" -ForegroundColor Cyan
    $packages = Get-ChildItem $outputDir -Filter *.nupkg | Sort-Object Name
    $symbols = Get-ChildItem $outputDir -Filter *.snupkg | Sort-Object Name
    if ($packages.Count -eq 0) { throw "No packages were produced in $outputDir." }

    $packages | Select-Object @{n = 'Package'; e = { $_.BaseName } },
                                @{n = 'Size (KB)'; e = { [math]::Round($_.Length / 1KB) } },
                                @{n = 'File'; e = { $_.Name } } |
        Format-Table -AutoSize | Out-String | Write-Host

    Write-Host ("{0} package(s), {1} symbol package(s), version {2}" -f $packages.Count, $symbols.Count, $version) -ForegroundColor Green

    $unexpected = $packages | Where-Object { $_.BaseName -notlike "*.$version" }
    if ($unexpected) { Write-Warning "Packages with a different version: $($unexpected.Name -join ', ')" }

    Write-Host "`nNext: verify the contents (pwsh ./build/Test-NuGetPackages.ps1), then publish (pwsh ./build/Push-NuGet.ps1)."
}
finally {
    Pop-Location
}
