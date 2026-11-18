# MolDraw.Skia

[![NuGet](https://img.shields.io/nuget/v/MolDraw.Skia.svg)](https://www.nuget.org/packages/MolDraw.Skia/)
[![Downloads](https://img.shields.io/nuget/dt/MolDraw.Skia.svg)](https://www.nuget.org/packages/MolDraw.Skia/)
[![License: Apache-2.0](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](https://github.com/Agredo/MolDraw.Net/blob/main/LICENSE)

**A presentation-quality SkiaSharp renderer for 2D chemical structures.** Crisp element labels, wedges, aromatic
styles, light/dark presets, per-atom heat-map highlighting with colour bars, hit testing for interactive apps, and
PNG/SVG export. No chemistry toolkit, no UI framework — works in .NET MAUI, Avalonia, WPF, ASP.NET Core and console
tools, on x64 and **ARM64**.

## Install

```shell
dotnet add package MolDraw.Skia
```

Targets `net11.0`; depends on `SkiaSharp` **4.156.x** and [MolDraw.Depiction](https://www.nuget.org/packages/MolDraw.Depiction/).

## Quick start

```csharp
using MolDraw.Core.Ncdk;      // optional: SMILES -> depiction (package MolDraw.Core)
using MolDraw.Skia;
using SkiaSharp;

var depiction = new DepictionBuilder().Build(new MoleculeLoader().ParseSmiles("CC(=O)OC1=CC=CC=C1C(=O)O"));

var exporter = new MoleculeImageExporter();
File.WriteAllBytes("aspirin.png", exporter.RenderPng(depiction, 400, 300, background: SKColors.White));
File.WriteAllText("aspirin.svg", exporter.RenderSvg(depiction, 400, 300));
```

Already have coordinates from RDKit, Indigo or a CSV? Build a `MoleculeDepiction` yourself and skip `MolDraw.Core`
completely — the renderer only needs the depiction model.

## Highlighting

```csharp
var highlights = new MoleculeHighlights
{
    AtomWeights = weights,                       // 0..1 per atom index; NaN = no highlight
    Colormap = Colormap.Turbo,                   // Viridis (default), Plasma, Inferno, Magma, Turbo, Colormap.Custom(...)
    AtomColors = new Dictionary<int, SKColor> { [3] = SKColors.Red },     // wins over the weights
    BondColors = new Dictionary<int, SKColor> { [2] = SKColors.Orange },
    SelectedAtom = 5,
    HoveredAtom = 7,
    RadiusScale = 1.2f,
};

var png = new MoleculeImageExporter().RenderPng(depiction, 600, 400, highlights: highlights);
```

Highlights are drawn as soft discs/bands **under** bonds and labels, so the structure stays readable. Use
`ShowColorBar` in `MoleculeRenderOptions` to add a scale next to the molecule.

## Drawing into your own canvas

```csharp
using var renderer = new MoleculeRenderer();
RenderedMolecule rendered = renderer.Render(canvas, bounds, depiction, MoleculeRenderOptions.Light, highlights);

// interactive apps: map pointer coordinates to atoms
int? atom = rendered.HitTestAtom(pointerPosition, tolerance: 4);
```

Works with any `SKCanvas` — Avalonia custom draw operations, `SKSurface` bitmaps, `SKCanvasView` in MAUI, etc.

## Customisation

`MoleculeRenderOptions` is an immutable record; start from `.Light` or `.Dark` and use `with`:

```csharp
var options = MoleculeRenderOptions.Dark with
{
    BondWidth = 2,
    FontSize = 16,
    FontFamily = "Segoe UI",
    MaxBondLength = 30,
    AromaticStyle = AromaticStyle.Circle,
    ShowAtomIndices = true,
    ShowColorBar = true,
    AtomColors = AtomColorScheme.CpkDark,
    BackgroundColor = SKColors.Transparent,
};
```

`MaxBondLength` caps the scale so small molecules are not blown up; fonts and lines shrink proportionally for large
structures or small bounds.

## Dependency injection

```csharp
services.AddMolDrawSkia();     // IMoleculeRenderer + MoleculeImageExporter as singletons (stateless, thread-safe)
```

In a MAUI app, `UseMolDraw()` (from [MolDraw.Maui](https://www.nuget.org/packages/MolDraw.Maui/)) already does this
plus `UseSkiaSharp()`.

## Related packages

| Package | Purpose |
|---|---|
| [MolDraw.Depiction](https://www.nuget.org/packages/MolDraw.Depiction/) | model + `IChemistryEngine` seam |
| [MolDraw.Core](https://www.nuget.org/packages/MolDraw.Core/) | NCDK engine for SMILES/MOL/SDF input |
| [MolDraw.Skia](https://www.nuget.org/packages/MolDraw.Skia/) | this package — renderer |
| [MolDraw.Maui](https://www.nuget.org/packages/MolDraw.Maui/) | .NET MAUI `MoleculeView` control |

## Documentation

* [README](https://github.com/Agredo/MolDraw.Net/blob/main/README.md) – colours, themes, ARM64 notes, limitations
* [Architecture](https://github.com/Agredo/MolDraw.Net/blob/main/docs/ARCHITECTURE.md)
* [Changelog](https://github.com/Agredo/MolDraw.Net/blob/main/CHANGELOG.md)
* [Issues](https://github.com/Agredo/MolDraw.Net/issues)

## License

Apache-2.0 © Agredo Application. Uses SkiaSharp (MIT); the bundled colour maps are CC0 (Matplotlib) and Apache-2.0
(Google Turbo). See [THIRD-PARTY-NOTICES.md](https://github.com/Agredo/MolDraw.Net/blob/main/THIRD-PARTY-NOTICES.md).
