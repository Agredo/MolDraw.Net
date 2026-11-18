# Changelog

All notable changes to MolDraw.Net are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html). Until 1.0.0, minor versions may contain breaking changes;
they are always listed under **Changed** with a migration note.

## [Unreleased]

Nothing yet.

## [0.4.0] - 2026-11-18

Moved to **.NET 11**, **.NET MAUI 11** and **SkiaSharp 4**. Chemistry, depiction output and the public API of the four
packages are unchanged, but the target frameworks are not, so this is a major-version-style bump for consumers: an app
that references these packages has to target `net11.0` as well.

### Changed

- **Breaking:** every package targets `net11.0` instead of `net10.0`. `MolDraw.Maui` ships
  `net11.0-android37.0`, `net11.0-ios27.2`, `net11.0-maccatalyst27.2` and
  `net11.0-windows10.0.19041.0`. Requires the .NET 11 SDK (11.0.100 or later) and the .NET 11 MAUI workloads.
- `Microsoft.Maui.Controls` 10.0.130 → 11.0.10; `SkiaSharp`, `SkiaSharp.Views.Maui.Controls` and
  `SkiaSharp.NativeAssets.Linux` 3.119.4 → 4.156.1.
- `MoleculeRenderer` now builds its wedge/wavy paths with `SKPathBuilder` — SkiaSharp 4 obsoleted the mutable `SKPath`
  drawing methods. Rendering output is unchanged (the snapshot tests still match).
- The explicit `Microsoft.Extensions.DependencyInjection.Abstractions` package reference was dropped: the assembly is
  part of the .NET 11 shared framework, so `AddMolDrawCore()` and the renderer registration need no extra dependency.
- The MAUI sample uses MVVM Toolkit partial properties and concrete collection types, which keeps the WinUI build free
  of `MVVMTK0045`/`CsWinRT1032` trimming warnings.

### Migration from 0.3.x

```xml
<!-- before -->
<TargetFramework>net10.0</TargetFramework>
<!-- after -->
<TargetFramework>net11.0</TargetFramework>
```

MAUI apps: use `net11.0-android`/`-ios`/`-maccatalyst`/`-windows10.0.19041.0` and `Microsoft.Maui.Controls` 11.x.
Nothing else changes — `UseMolDraw()`, `AddMolDrawCore()`, `IChemistryEngine` and the bindable properties are the same.

## [0.3.0] - 2026-11-17

Chemistry engine and renderer are now separated by an interface, so NCDK is no longer a dependency of the MAUI control.

### Added

- `IChemistryEngine` (`Name`, `Depict(smiles, options)`) in `MolDraw.Depiction` — the seam between chemistry toolkits
  and rendering. Any engine (NCDK, RDKit, Indigo, custom) can now drive `MoleculeView.Smiles`.
- `NcdkDepictionEngine` in `MolDraw.Core.Ncdk` — the NCDK implementation of that seam.
- `DepictionBuildOptions`, `CarbonLabelMode`, `MoleculeParseException` and `DepictionException` in `MolDraw.Depiction`.
- `AddMolDrawCore()` also registers `IChemistryEngine` (via `TryAddSingleton`, so a previously registered engine wins).
- Tests for the seam, including a fake engine that proves the NCDK-free path.

### Changed

- **Breaking:** `MolDraw.Maui` no longer references `MolDraw.Core`, so it no longer pulls in NCDK (LGPL-2.1). Register an
  engine explicitly (`builder.Services.AddMolDrawCore()`) if you want `MoleculeView.Smiles` to work.
- **Breaking:** `UseMolDraw()` no longer takes `MoleculeLoaderOptions` and only adds the renderer, the depiction cache and
  `UseSkiaSharp()`.
- **Breaking:** `MoleculeView.Molecule` was removed — bind `MoleculeView.Depiction` instead.
- **Breaking:** engine-neutral types moved to `MolDraw.Depiction` (`IChemistryEngine`, `DepictionBuildOptions`,
  `CarbonLabelMode`, `MoleculeParseException`, `DepictionException`); `MoleculeLoaderOptions` and `MoleculeFormat` moved
  to `MolDraw.Core.Ncdk`. Add the matching `using`.
- Package descriptions, icons and repository links for the nuget.org publication.

### Migration from 0.2.x

```csharp
builder.UseMauiApp<App>().UseMolDraw();     // renderer + cache only
builder.Services.AddMolDrawCore();          // + using MolDraw.Core; restores the old "SMILES just works" behaviour
```

## [0.2.0] - 2026-11-17

### Changed

- **Breaking:** the NCDK-bound API moved to the `MolDraw.Core.Ncdk` namespace (`MoleculeLoader`, `IMoleculeLoader`,
  `DepictionBuilder`, `IDepictionBuilder`). Add `using MolDraw.Core.Ncdk;`.

## [0.1.1] - 2026-11-17

### Changed

- Dependency updates: CommunityToolkit.Mvvm 8.4.2, Microsoft.Extensions.DependencyInjection 11.0.0,
  Microsoft.NET.Test.Sdk 18.12.0, xunit.runner.visualstudio 4.0.1, coverlet.collector 10.1.1, .NET MAUI 10.0.130.
  SkiaSharp stays at 3.119.4.

## [0.1.0] - 2026-11-17

### Added

- First release: `MolDraw.Depiction`, `MolDraw.Core` (NCDK), `MolDraw.Skia` (SkiaSharp 3.119 renderer) and
  `MolDraw.Maui` (`MoleculeView` control), Apache-2.0.
- 2D depiction of SMILES/MOL/SDF with aromaticity, ring-aware double bonds, wedge/hash stereo and implicit hydrogens;
  atom order compatible with RDKit.
- Per-atom heat-map highlighting (Viridis, Plasma, Inferno, Magma, Turbo, custom), colour bars, atom selection and
  hover with MVVM bindings and commands, light/dark themes, hit testing, PNG/SVG export.
- x64 and ARM64 support: Windows (WinUI), Android, iOS, Mac Catalyst, plus `net10.0` for server/Avalonia use.

[Unreleased]: https://github.com/Agredo/MolDraw.Net/compare/v0.4.0...HEAD
[0.4.0]: https://github.com/Agredo/MolDraw.Net/compare/v0.3.0...v0.4.0
[0.3.0]: https://github.com/Agredo/MolDraw.Net/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/Agredo/MolDraw.Net/compare/v0.1.1...v0.2.0
[0.1.1]: https://github.com/Agredo/MolDraw.Net/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/Agredo/MolDraw.Net/releases/tag/v0.1.0
