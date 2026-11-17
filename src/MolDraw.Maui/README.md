# MolDraw.Maui

[![NuGet](https://img.shields.io/nuget/v/MolDraw.Maui.svg)](https://www.nuget.org/packages/MolDraw.Maui/)
[![Downloads](https://img.shields.io/nuget/dt/MolDraw.Maui.svg)](https://www.nuget.org/packages/MolDraw.Maui/)
[![License: Apache-2.0](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](https://github.com/Agredo/MolDraw.Net/blob/main/LICENSE)

**`MoleculeView` — an interactive 2D chemical structure control for .NET MAUI.** Show structures from SMILES or from
precomputed coordinates, colour atoms by heat-map weights (perfect for mass-spec peaks, docking scores or model
attention), let the user tap and hover atoms through MVVM bindings, and follow the app's light/dark theme.
Runs on Windows (x64/**ARM64**), Android, iOS and Mac Catalyst — the .NET MAUI part contains **no chemistry toolkit**.

## Install

```shell
dotnet add package MolDraw.Maui      # the control (pulls MolDraw.Skia + MolDraw.Depiction)
dotnet add package MolDraw.Core      # optional: NCDK engine so `Smiles` works out of the box
```

Requires .NET 10, .NET MAUI 10 and SkiaSharp 3.119.x (not 4.x).

## Quick start

```csharp
// MauiProgram.cs
using MolDraw.Core;

builder.UseMauiApp<App>()
       .UseMolDraw();                    // UseSkiaSharp() + renderer + depiction cache
builder.Services.AddMolDrawCore();       // chemistry engine (SMILES -> depiction); optional
```

```xml
<ContentPage xmlns:md="clr-namespace:MolDraw.Maui;assembly=MolDraw.Maui" ...>
    <md:MoleculeView Smiles="{Binding Smiles}"
                     AtomWeights="{Binding AtomWeights}"
                     Colormap="{Binding Colormap}"
                     ShowColorBar="True"
                     SelectedAtomIndex="{Binding SelectedAtomIndex}"
                     AtomTappedCommand="{Binding AtomTappedCommand}"
                     ErrorMessage="{Binding SmilesError}" />
</ContentPage>
```

## Highlighting atoms (MVVM)

```csharp
public partial class PeakViewModel : ObservableObject
{
    [ObservableProperty] private string smiles = "CC(=O)OC1=CC=CC=C1C(=O)O";
    [ObservableProperty] private IReadOnlyList<float>? atomWeights;   // 0..1 per atom index
    [ObservableProperty] private int? selectedAtomIndex;              // TwoWay: tap selects/deselects
    [ObservableProperty] private string? smilesError;                 // pushed by the control

    public void ShowPeak(float[] modelOutput) => AtomWeights = modelOutput;  // assign a new list to refresh

    [RelayCommand] private void AtomTapped(int atomIndex) { /* show matching peaks */ }
}
```

Atom indices in `AtomWeights` are the heavy-atom indices of the SMILES — the same order RDKit uses — so model outputs
can be bound directly. Recolouring never recomputes coordinates.

## Properties

| Property | Default mode | Purpose |
|---|---|---|
| `Smiles` | OneWay | Depicted by the registered `IChemistryEngine`, cached by SMILES string |
| `Depiction` (`MoleculeDepiction`) | OneWay | Precomputed depiction, e.g. from RDKit or Indigo (takes precedence over `Smiles`) |
| `DepictionOptions` | OneWay | `ForceLayout`, `PerceiveAromaticity`, `AssignMissingWedges`, `CarbonLabels` |
| `RenderOptions` | OneWay | `null` → `Light`/`Dark` preset following `Application.RequestedTheme` |
| `AtomWeights` (`IReadOnlyList<float>`) | OneWay | 0..1 per atom, mapped through `Colormap` |
| `AtomColors`, `BondColors` | OneWay | Explicit per-index highlight colours (win over weights) |
| `Colormap`, `ShowColorBar`, `HighlightRadiusScale` | OneWay | Highlight customisation |
| `SelectedAtomIndex` (`int?`) | **TwoWay** | Tap selects; tapping again or empty space deselects |
| `HoveredAtomIndex` (`int?`) | **OneWayToSource** | Pointer hover (desktop) |
| `AtomTappedCommand`, `AtomHoveredCommand` | – | `ICommand` with the atom index |
| `ErrorMessage` (`string?`) | **OneWayToSource** | Invalid input, shown inside the control and never thrown |
| `IsInteractive`, `HitTolerance` | OneWay | Pointer handling |

## Using another chemistry engine

`MolDraw.Maui` only knows `IChemistryEngine` (from [MolDraw.Depiction](https://www.nuget.org/packages/MolDraw.Depiction/)).
Register your own implementation — RDKit, Indigo, a web service — before or after `UseMolDraw()` and `Smiles` follows it:

```csharp
builder.Services.AddSingleton<IChemistryEngine, IndigoEngine>();
```

Without any engine, `Smiles` produces an explanatory `ErrorMessage` instead of a picture, while a bound `Depiction`
always renders. That also means: **no LGPL dependency** in this package — NCDK only enters your app via `MolDraw.Core`.

## Platforms

| Platform | Package target |
|---|---|
| Windows 10/11 (WinUI), x64 + ARM64 | `net10.0-windows10.0.19041.0` |
| Android (arm64-v8a, arm, x64, x86) | `net10.0-android36.0` |
| iOS 15+ | `net10.0-ios26.0` |
| Mac Catalyst 15+ | `net10.0-maccatalyst26.0` |

The control is managed code; native Skia binaries come from SkiaSharp for the consuming app's runtime identifier.

## Related packages

| Package | Purpose |
|---|---|
| [MolDraw.Depiction](https://www.nuget.org/packages/MolDraw.Depiction/) | model + `IChemistryEngine` seam |
| [MolDraw.Core](https://www.nuget.org/packages/MolDraw.Core/) | NCDK engine (`AddMolDrawCore()`) |
| [MolDraw.Skia](https://www.nuget.org/packages/MolDraw.Skia/) | renderer, highlights, PNG/SVG export |
| [MolDraw.Maui](https://www.nuget.org/packages/MolDraw.Maui/) | this package — MAUI control |

## Documentation

* [README](https://github.com/Agredo/MolDraw.Net/blob/main/README.md) – full quick start, XAML and ARM64 notes
* [Samples](https://github.com/Agredo/MolDraw.Net/tree/main/samples/MolDraw.Maui.Sample)
* [Architecture](https://github.com/Agredo/MolDraw.Net/blob/main/docs/ARCHITECTURE.md)
* [Changelog](https://github.com/Agredo/MolDraw.Net/blob/main/CHANGELOG.md)
* [Issues](https://github.com/Agredo/MolDraw.Net/issues)

## License

Apache-2.0 © Agredo Application. Uses SkiaSharp and .NET MAUI (both MIT); no chemistry toolkit is bundled.
