<#
.SYNOPSIS
    Verifies the packed MolDraw.Net NuGet packages before they are published.

.DESCRIPTION
    Checks, for every package in artifacts/packages:
      * metadata (id, version, author, project/repository URL, icon, readme, licence expression),
      * that README.md, LICENSE, THIRD-PARTY-NOTICES.md and icon.png are embedded at the package root,
      * the dependency graph (MolDraw.Maui must not depend on MolDraw.Core or NCDK, MolDraw.Skia must not depend on NCDK),
      * that the MAUI package carries all four target frameworks including the WinUI .pri file,
      * that README links are absolute (nuget.org cannot resolve relative links).
    Throws and exits with a non-zero code when a check fails.

.PARAMETER PackageDirectory
    Directory to inspect. Defaults to artifacts/packages.

.PARAMETER Version
    Expected version. Defaults to the version in Directory.Build.props.
#>
[CmdletBinding()]
param(
    [string] $PackageDirectory,
    [string] $Version
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.IO.Compression.FileSystem

$repoRoot = Split-Path $PSScriptRoot -Parent
if (-not $PackageDirectory) { $PackageDirectory = Join-Path $repoRoot 'artifacts/packages' }
if (-not $Version) {
    $props = [xml](Get-Content (Join-Path $repoRoot 'Directory.Build.props'))
    $Version = @($props.SelectNodes('//PropertyGroup/Version') | ForEach-Object { $_.InnerText.Trim() })[0]
}

$expectedIds = @('MolDraw.Depiction', 'MolDraw.Core', 'MolDraw.Skia', 'MolDraw.Maui')
$projectUrl = 'https://github.com/Agredo/MolDraw.Net'
$mauiTfms = @('net10.0-android36.0', 'net10.0-ios26.0', 'net10.0-maccatalyst26.0', 'net10.0-windows10.0.19041')

$failures = [System.Collections.Generic.List[string]]::new()

function Check {
    param([bool] $Condition, [string] $Message)
    if ($Condition) {
        Write-Host "  ok   $Message" -ForegroundColor DarkGreen
    }
    else {
        Write-Host "  FAIL $Message" -ForegroundColor Red
        $script:failures.Add($Message)
    }
}

function Get-PackageEntries {
    param([string] $Path)
    $zip = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $entries = @{}
        foreach ($entry in $zip.Entries) {
            $reader = [System.IO.StreamReader]::new($entry.Open())
            try { $entries[$entry.FullName] = $reader.ReadToEnd() } finally { $reader.Dispose() }
        }
        return $entries
    }
    finally { $zip.Dispose() }
}

Write-Host "Verifying packages in $PackageDirectory (version $Version)`n" -ForegroundColor Cyan

foreach ($id in $expectedIds) {
    $nupkg = Join-Path $PackageDirectory "$id.$Version.nupkg"
    $snupkg = Join-Path $PackageDirectory "$id.$Version.snupkg"

    Write-Host "$id $Version" -ForegroundColor Cyan
    if (-not (Test-Path $nupkg)) { Check $false "$id.$Version.nupkg exists"; continue }

    Check (Test-Path $snupkg) "$id.$Version.snupkg exists"

    $entries = Get-PackageEntries $nupkg
    $nuspecEntry = $entries.Keys | Where-Object { $_ -like '*.nuspec' } | Select-Object -First 1
    if (-not $nuspecEntry) { Check $false "$id contains a .nuspec"; continue }

    [xml]$nuspec = $entries[$nuspecEntry]
    $meta = $nuspec.package.metadata

    Check ($meta.id -eq $id) "nuspec id is $id"
    Check ($meta.version -eq $Version) "nuspec version is $Version"
    Check ($meta.authors -eq 'Agredo Application') "author is 'Agredo Application' (is: '$($meta.authors)')"
    Check ($meta.projectUrl -eq $projectUrl) "projectUrl is $projectUrl"
    Check ($meta.repository.url -eq $projectUrl) "repository url is $projectUrl"
    Check ($meta.license.type -eq 'expression' -and $meta.license.'#text' -eq 'Apache-2.0') 'licence is Apache-2.0'
    Check ($meta.icon -eq 'icon.png') 'icon is declared'
    Check ($meta.readme -eq 'README.md') 'readme is declared'
    Check ($meta.description.Length -ge 100) "description is substantial ($($meta.description.Length) chars)"
    Check ([bool] $meta.tags) "tags are set ($($meta.tags))"

    foreach ($file in 'README.md', 'LICENSE', 'THIRD-PARTY-NOTICES.md', 'icon.png') {
        Check ($entries.ContainsKey($file)) "$file is embedded at the package root"
    }
    if ($entries.ContainsKey('icon.png')) {
        $iconIsPng = $entries['icon.png'].Length -gt 0
        Check $iconIsPng 'icon.png is not empty'
    }

    $dependencies = @(@($nuspec.SelectNodes("//*[local-name()='dependency']") | ForEach-Object { $_.GetAttribute('id') }) | Sort-Object -Unique)
    Write-Host "       dependencies: $($dependencies -join ', ')"

    switch ($id) {
        'MolDraw.Depiction' {
            Check ($dependencies.Count -eq 0) 'has no dependencies'
        }
        'MolDraw.Skia' {
            Check ($dependencies -contains 'MolDraw.Depiction') 'depends on MolDraw.Depiction'
            Check ($dependencies -contains 'SkiaSharp') 'depends on SkiaSharp'
            Check ($dependencies -notcontains 'NCDK') 'does NOT depend on NCDK'
            Check ($dependencies -notcontains 'MolDraw.Core') 'does NOT depend on MolDraw.Core'
        }
        'MolDraw.Core' {
            Check ($dependencies -contains 'MolDraw.Depiction') 'depends on MolDraw.Depiction'
            Check ($dependencies -contains 'NCDK') 'depends on NCDK'
        }
        'MolDraw.Maui' {
            Check ($dependencies -contains 'MolDraw.Skia') 'depends on MolDraw.Skia'
            Check ($dependencies -contains 'Microsoft.Maui.Controls') 'depends on Microsoft.Maui.Controls'
            Check ($dependencies -notcontains 'MolDraw.Core') 'does NOT depend on MolDraw.Core'
            Check ($dependencies -notcontains 'NCDK') 'does NOT depend on NCDK'
            foreach ($tfm in $mauiTfms) {
                $hasDll = $entries.Keys -contains "lib/$tfm/MolDraw.Maui.dll"
                Check $hasDll "lib/$tfm contains MolDraw.Maui.dll"
            }
            # The WinUI resource index is only produced for Windows; its absence breaks WinUI consumers at runtime.
            Check ($entries.Keys -contains 'lib/net10.0-windows10.0.19041/MolDraw.Maui.pri') 'WinUI resource index MolDraw.Maui.pri is packed'
            Check (-not ($entries.Keys | Where-Object { $_ -like '*NCDK*' })) 'contains no NCDK files'
        }
    }

    if ($entries.ContainsKey('README.md')) {
        $readme = $entries['README.md']
        $relativeLinks = @([regex]::Matches($readme, '\]\((?!#)(?!https?://)(?!mailto:)([^)]+)\)') |
            ForEach-Object { $_.Groups[1].Value } |
            Where-Object { $_ -notmatch '^(https?:|#|mailto:)' })
        Check ($relativeLinks.Count -eq 0) "README links are absolute$([string]::Join('', @($relativeLinks | ForEach-Object { " (found: $_)" })))"
        Check ($readme -match 'dotnet add package') 'README contains an installation snippet'
    }

    Write-Host ''
}

if ($failures.Count -gt 0) {
    Write-Host "$($failures.Count) check(s) failed." -ForegroundColor Red
    exit 1
}

Write-Host 'All package checks passed.' -ForegroundColor Green
