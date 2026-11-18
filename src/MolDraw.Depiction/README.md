# MolDraw.Depiction

[![NuGet](https://img.shields.io/nuget/v/MolDraw.Depiction.svg)](https://www.nuget.org/packages/MolDraw.Depiction/)
[![Downloads](https://img.shields.io/nuget/dt/MolDraw.Depiction.svg)](https://www.nuget.org/packages/MolDraw.Depiction/)
[![License: Apache-2.0](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](https://github.com/Agredo/MolDraw.Net/blob/main/LICENSE)

**The boundary between chemistry and rendering.** An immutable, toolkit-neutral 2D depiction model
(`MoleculeDepiction`: atoms, bonds, rings, coordinates) plus the `IChemistryEngine` seam that lets any chemistry
toolkit — NCDK, RDKit, Indigo, OpenChemLib or your own code — drive the
[MolDraw.Skia](https://www.nuget.org/packages/MolDraw.Skia/) renderer.
**No dependencies, no chemistry toolkit, no UI framework.**

## Install

```shell
dotnet add package MolDraw.Depiction
```

Targets `net11.0`. Dependency-free, so it is safe to reference from any layer of your app.

## What you get

| Type | Purpose |
|---|---|
| `MoleculeDepiction` | Immutable depiction: `Atoms`, `Bonds`, `Rings`, `Empty`, `GetBounds()`, `GetMedianBondLength()` |
| `DepictionAtom` | Index, element symbol, 2D coordinates, implicit hydrogens, charge, mass number, label/aromatic/radical flags |
| `DepictionBond` | Begin/end atom, `BondOrder`, `BondStereoDisplay` (wedge/hash/wavy), `DoubleBondPlacement`, `KekuleOrder`, ring flag |
| `DepictionRing` | Atom indices of a ring and whether it is aromatic |
| `IChemistryEngine` | The seam: `Name` + `Depict(smiles, options)` → `MoleculeDepiction` |
| `DepictionBuildOptions` | Requested layout/label behaviour: `ForceLayout`, `PerceiveAromaticity`, `AssignMissingWedges`, `CarbonLabels` |
| `MoleculeParseException`, `DepictionException` | Toolkit-neutral failures — `MolDraw.Maui` turns them into `MoleculeView.ErrorMessage` |
| `BondOrder`, `BondStereoDisplay`, `DoubleBondPlacement`, `HydrogenPosition`, `CarbonLabelMode` | Shared enums |

## Quick start: build a depiction by hand

```csharp
using MolDraw.Depiction;

var depiction = new MoleculeDepiction(
    [
        new DepictionAtom(0, "C", 0.0, 0.0, 3, 0, null, false, HydrogenPosition.Right),
        new DepictionAtom(1, "O", 1.5, 0.0, 1, 0, null, true,  HydrogenPosition.Right),
    ],
    [
        new DepictionBond(0, 1, BondOrder.Single, BondStereoDisplay.None, DoubleBondPlacement.Center),
    ]);
```

Coordinates follow the MOL/RDKit convention (**y points up**, arbitrary bond length); the renderer scales by the median
bond length. Two hard rules are validated in the constructor: `atoms[i].Index == i`, `bonds[j].Index == j`, and every
bond must reference two distinct existing atoms. Everything else is optional and degrades gracefully (no rings → plain
bonds, no `ShowLabel` → skeletal formula, no `Stereo` → plain bonds).

## Quick start: implement your own engine

```csharp
using MolDraw.Depiction;

public sealed class IndigoEngine : IChemistryEngine
{
    public string Name => "Indigo";

    public MoleculeDepiction Depict(string smiles, DepictionBuildOptions? options = null)
    {
        // 1. parse `smiles`  2. lay it out  3. map the result onto DepictionAtom/DepictionBond
        // throw MoleculeParseException for bad input, DepictionException when layout fails
    }
}

// .NET MAUI / generic host:
builder.Services.AddSingleton<IChemistryEngine, IndigoEngine>();
```

That is all a UI or renderer needs — `MolDraw.Maui`'s `MoleculeView.Smiles` and its depiction cache work with any engine,
and `MolDraw.Skia` renders the resulting `MoleculeDepiction` without knowing where it came from.

## Related packages

| Package | Purpose |
|---|---|
| [MolDraw.Depiction](https://www.nuget.org/packages/MolDraw.Depiction/) | this package — model + engine seam |
| [MolDraw.Core](https://www.nuget.org/packages/MolDraw.Core/) | NCDK engine (`AddMolDrawCore()`) |
| [MolDraw.Skia](https://www.nuget.org/packages/MolDraw.Skia/) | SkiaSharp renderer, highlights, PNG/SVG export |
| [MolDraw.Maui](https://www.nuget.org/packages/MolDraw.Maui/) | .NET MAUI `MoleculeView` control |

## Documentation

* [README](https://github.com/Agredo/MolDraw.Net/blob/main/README.md) – full feature tour and examples
* [Architecture](https://github.com/Agredo/MolDraw.Net/blob/main/docs/ARCHITECTURE.md) – why the seam exists
* [Changelog](https://github.com/Agredo/MolDraw.Net/blob/main/CHANGELOG.md)
* [Issues](https://github.com/Agredo/MolDraw.Net/issues)

## License

Apache-2.0 © Agredo Application. No third-party code is redistributed in this package.
