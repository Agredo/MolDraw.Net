# MolDraw.Core

[![NuGet](https://img.shields.io/nuget/v/MolDraw.Core.svg)](https://www.nuget.org/packages/MolDraw.Core/)
[![Downloads](https://img.shields.io/nuget/dt/MolDraw.Core.svg)](https://www.nuget.org/packages/MolDraw.Core/)
[![License: Apache-2.0](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](https://github.com/Agredo/MolDraw.Net/blob/main/LICENSE)

**The chemistry engine for MolDraw.Net.** Parses SMILES, MOL and SDF input with
[NCDK](https://github.com/kazuyaujihara/NCDK) and turns it into a clean, toolkit-neutral
[`MoleculeDepiction`](https://www.nuget.org/packages/MolDraw.Depiction/): 2D layout, aromaticity perception,
ring-aware double bonds, wedge/hash stereo, implicit hydrogens and RDKit-compatible heavy-atom order — so
per-atom model output lines up with the picture.

## Install

```shell
dotnet add package MolDraw.Core
```

Targets `net10.0`. Pulls in `MolDraw.Depiction` and NCDK.

## Quick start: .NET MAUI

```csharp
// MauiProgram.cs
using MolDraw.Core;

builder.UseMauiApp<App>()
       .UseMolDraw();                 // renderer + depiction cache (package MolDraw.Maui)
builder.Services.AddMolDrawCore();    // the NCDK chemistry engine
```

```xml
<md:MoleculeView Smiles="{Binding Smiles}" AtomWeights="{Binding AtomWeights}" />
```

`Smiles` now yields a picture. Without `AddMolDrawCore()` (or another `IChemistryEngine`) `MoleculeView.Smiles` only
reports an `ErrorMessage`.

## Quick start: without MAUI (console, server, tests)

```csharp
using MolDraw.Core.Ncdk;      // MoleculeLoader / DepictionBuilder – the NCDK-backed API
using MolDraw.Depiction;      // MoleculeDepiction – the toolkit-neutral model

var loader = new MoleculeLoader();
var builder = new DepictionBuilder();

var molecule = loader.ParseSmiles("c1ccccc1O");          // phenol (throws MoleculeParseException on bad input)
MoleculeDepiction depiction = builder.Build(molecule);

// also available: TryParseSmiles(...), Load(stream, MoleculeFormat.MolFile | Sdf | Smiles), LoadAll(...)
```

Rendering is not part of this package — pair it with [MolDraw.Skia](https://www.nuget.org/packages/MolDraw.Skia/):

```csharp
File.WriteAllBytes("phenol.png", new MoleculeImageExporter().RenderPng(depiction, 400, 300));
```

## What registering does

`AddMolDrawCore()` adds three singletons with `TryAddSingleton`, so anything you registered **before** it wins:

| Service | Implementation |
|---|---|
| `IChemistryEngine` | `NcdkDepictionEngine` (`Name` = `"NCDK"`) — this is what `MoleculeView.Smiles` calls |
| `IMoleculeLoader` | `MoleculeLoader` (`ParseSmiles`, `TryParseSmiles`, `Load`, `LoadAll`) |
| `IDepictionBuilder` | `DepictionBuilder` (`Build(molecule, options)`) |

```csharp
// Register your own engine first and NCDK stays out of the picture entirely:
builder.Services.AddSingleton<IChemistryEngine, MyEngine>();
builder.Services.AddMolDrawCore();
```

## API

| Member | Notes |
|---|---|
| `MoleculeLoader.ParseSmiles(string)` | Atom order = order of heavy atoms in the SMILES (= RDKit `MolFromSmiles`). `[H]` is folded into implicit counts like RDKit's `RemoveHs` |
| `MoleculeLoader.TryParseSmiles(string?, out IAtomContainer?, out string?)` | Non-throwing variant for UI input |
| `MoleculeLoader.Load(Stream, MoleculeFormat)` / `LoadAll(...)` | First molecule / all records (SDF, SMILES lines, single molfile) |
| `DepictionBuilder.Build(IAtomContainer, DepictionBuildOptions?)` | Never mutates the input; atom `i` of the depiction == `molecule.Atoms[i]` |
| `MoleculeLoaderOptions` | `KeepExplicitHydrogens`, `MoleculeFormat`, … (`MolDraw.Core.Ncdk`) |
| `DepictionBuildOptions` | `ForceLayout`, `PerceiveAromaticity`, `AssignMissingWedges`, `CarbonLabels` (`MolDraw.Depiction`) |

Depictions and services are immutable/stateless and thread-safe; `MoleculeView` runs parsing + layout off the UI thread.

## License note

`MolDraw.Core` depends on **NCDK 1.6.1 (LGPL-2.1-only)** as a separate NuGet package; NCDK is not modified and not
statically linked. Apps that distribute `MolDraw.Core` must satisfy the LGPL (ship the NCDK licence text, point to its
source, allow replacing `NCDK.dll`). The other MolDraw.Net packages — `MolDraw.Depiction`, `MolDraw.Skia` and
`MolDraw.Maui` — do **not** depend on NCDK, so you can keep an app LGPL-free by supplying your own `IChemistryEngine`.
Details: [THIRD-PARTY-NOTICES.md](https://github.com/Agredo/MolDraw.Net/blob/main/THIRD-PARTY-NOTICES.md).

Known NCDK quirks (not trim-safe, InChI natives missing on ARM64) are listed in the
[main README](https://github.com/Agredo/MolDraw.Net#known-limitations); MolDraw never calls InChI.

## Related packages

| Package | Purpose |
|---|---|
| [MolDraw.Depiction](https://www.nuget.org/packages/MolDraw.Depiction/) | model + `IChemistryEngine` seam |
| [MolDraw.Core](https://www.nuget.org/packages/MolDraw.Core/) | this package — NCDK engine |
| [MolDraw.Skia](https://www.nuget.org/packages/MolDraw.Skia/) | SkiaSharp renderer, PNG/SVG export |
| [MolDraw.Maui](https://www.nuget.org/packages/MolDraw.Maui/) | .NET MAUI `MoleculeView` control |

## Documentation

* [README](https://github.com/Agredo/MolDraw.Net/blob/main/README.md)
* [Architecture](https://github.com/Agredo/MolDraw.Net/blob/main/docs/ARCHITECTURE.md)
* [Changelog](https://github.com/Agredo/MolDraw.Net/blob/main/CHANGELOG.md)
* [Issues](https://github.com/Agredo/MolDraw.Net/issues)

## License

Apache-2.0 © Agredo Application. NCDK: LGPL-2.1-only.
