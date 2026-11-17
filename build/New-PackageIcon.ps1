<#
.SYNOPSIS
    Regenerates assets/icon.png, the icon embedded in all MolDraw.Net NuGet packages.

.DESCRIPTION
    The icon is drawn with SkiaSharp (already a repository dependency) so that it can be reproduced at any time
    without external tools. It is a 256x256 PNG: a benzene ring on a dark slate card, with an aromatic circle in
    amber. 256 px so that the 128 px nuget.org rendering stays crisp on high-DPI displays.

.PARAMETER OutputPath
    Target file. Defaults to <repo>/assets/icon.png.

.EXAMPLE
    pwsh ./build/New-PackageIcon.ps1
#>
[CmdletBinding()]
param(
    [string] $OutputPath = (Join-Path (Split-Path $PSScriptRoot -Parent) 'assets/icon.png')
)

$ErrorActionPreference = 'Stop'

$skiaVersion = '3.119.4'
$work = Join-Path ([System.IO.Path]::GetTempPath()) ("moldraw-icon-" + [guid]::NewGuid().ToString('N'))

New-Item -ItemType Directory -Path $work -Force | Out-Null
try {
    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="SkiaSharp" Version="$skiaVersion" />
  </ItemGroup>
</Project>
"@ | Set-Content (Join-Path $work 'IconGen.csproj')

    @'
using SkiaSharp;

const int Size = 256;
using var surface = SKSurface.Create(new SKImageInfo(Size, Size, SKColorType.Rgba8888, SKAlphaType.Premul));
var canvas = surface.Canvas;
canvas.Clear(SKColors.Transparent);

// Card: rounded square with a vertical slate gradient.
using var card = new SKPath();
card.AddRoundRect(new SKRect(4, 4, Size - 4, Size - 4), 54, 54);
using (var fill = new SKPaint
{
    IsAntialias = true,
    Shader = SKShader.CreateLinearGradient(
        new SKPoint(0, 4), new SKPoint(0, Size - 4),
        [SKColor.Parse("#1F2937"), SKColor.Parse("#0B1220")],
        [0f, 1f],
        SKShaderTileMode.Clamp),
})
{
    canvas.DrawPath(card, fill);
}

// Subtle top-left glow so the card does not look flat.
using (var glow = new SKPaint
{
    IsAntialias = true,
    Shader = SKShader.CreateRadialGradient(
        new SKPoint(64, 40), 190,
        [new SKColor(255, 255, 255, 26), new SKColor(255, 255, 255, 0)],
        [0f, 1f],
        SKShaderTileMode.Clamp),
})
{
    canvas.DrawPath(card, glow);
}

// Benzene ring, pointy-top hexagon.
const float cx = Size / 2f, cy = Size / 2f;
const float outer = 78f;
using var hex = new SKPath();
for (var i = 0; i < 6; i++)
{
    var angle = MathF.PI / 180 * (60 * i - 90);
    var p = new SKPoint(cx + outer * MathF.Cos(angle), cy + outer * MathF.Sin(angle));
    if (i == 0) hex.MoveTo(p); else hex.LineTo(p);
}
hex.Close();

using var ringStroke = new SKPaint
{
    IsAntialias = true,
    Style = SKPaintStyle.Stroke,
    StrokeWidth = 15,
    StrokeCap = SKStrokeCap.Round,
    StrokeJoin = SKStrokeJoin.Round,
    Color = SKColor.Parse("#F8FAFC"),
};

canvas.DrawPath(hex, ringStroke);

// Aromatic circle in amber.
using (var aromatic = new SKPaint
{
    IsAntialias = true,
    Style = SKPaintStyle.Stroke,
    StrokeWidth = 11,
    Color = SKColor.Parse("#F59E0B"),
})
{
    canvas.DrawCircle(cx, cy, 33, aromatic);
}

// Two highlighted atoms in cyan: the per-atom highlighting the library is built around.
using (var atom = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#22D3EE") })
{
    canvas.DrawCircle(Vertex(cx, cy, outer, -150), 15, atom);
    canvas.DrawCircle(Vertex(cx, cy, outer, -90), 15, atom);
}

var target = Path.GetFullPath(args[0]);
Directory.CreateDirectory(Path.GetDirectoryName(target)!);
using (var image = surface.Snapshot())
using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
using (var stream = File.Create(target))
{
    data.SaveTo(stream);
}

Console.WriteLine($"{target} ({new FileInfo(target).Length} bytes)");

static SKPoint Vertex(float cx, float cy, float radius, float degrees)
{
    var angle = MathF.PI / 180 * degrees;
    return new SKPoint(cx + radius * MathF.Cos(angle), cy + radius * MathF.Sin(angle));
}
'@ | Set-Content (Join-Path $work 'Program.cs')

    Push-Location $work
    try {
        dotnet run -c Release -- $OutputPath
    }
    finally {
        Pop-Location
    }
}
finally {
    Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
}
