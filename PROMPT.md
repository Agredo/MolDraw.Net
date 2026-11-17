# MolDraw.Net – 2D chemical structure rendering for .NET MAUI (SkiaSharp + NCDK)

Create a production-quality, reusable .NET library for rendering **and interactively highlighting** 2D chemical structures in .NET MAUI applications. Repository: `E:\Projects\MolDraw.Net` (git repo, commit in logical steps).

Primary consumer: a FLARE mass-spectrometry app (MVVM, CommunityToolkit.Mvvm) that colours each atom by how strongly it matches a selected MS/MS peak, and lets the user click atoms to highlight the matching peaks. The library must stay generic, but these needs are first-class requirements (see §4).

## 0. Technology baseline (fixed)

- .NET 10, C# latest, nullable enabled, `TreatWarningsAsErrors` for library projects.
- .NET MAUI 10 (`net10.0-android;net10.0-ios;net10.0-maccatalyst;net10.0-windows10.0.19041.0`; Windows-only builds must work on a machine with only the `maui-windows` workload → make non-Windows TFMs conditional on the OS/installed workloads).
- **SkiaSharp 3.119.x** + `SkiaSharp.Views.Maui.Controls` 3.119.x (must match the consumer apps; do **not** use SkiaSharp 4.x).
- CommunityToolkit.Mvvm 8.4.x for the sample.
- `.slnx` solution, `Directory.Build.props`, central package management (`Directory.Packages.props`).
- xUnit for tests.

## 1. Architecture – three layers, NCDK isolated

```
MolDraw.Net.slnx
src/
  MolDraw.Core/     netstandard2.0/net8.0+, NO MAUI, NO SkiaSharp – NCDK → depiction model
  MolDraw.Skia/     net8.0+, SkiaSharp only, NO NCDK, NO MAUI – renders the depiction model
  MolDraw.Maui/     MAUI control (MoleculeView), depends on Core + Skia
samples/
  MolDraw.Maui.Sample/
tests/
  MolDraw.Core.Tests/
  MolDraw.Skia.Tests/
docs/
  ARCHITECTURE.md
```

Key design decision: **the renderer does not consume NCDK types**. Core converts an NCDK `IAtomContainer` into an immutable, toolkit-neutral **depiction model**:

```csharp
public sealed record MoleculeDepiction(
    IReadOnlyList<DepictionAtom> Atoms,      // index == atom index of the source molecule
    IReadOnlyList<DepictionBond> Bonds);

public sealed record DepictionAtom(
    int Index, string Symbol, double X, double Y,   // 2D coordinates, y up
    int ImplicitHydrogens, int Charge, int? MassNumber,
    bool ShowLabel, HydrogenPosition HydrogenPosition /* left/right/above/below */);

public sealed record DepictionBond(
    int Begin, int End, BondOrder Order /* Single/Double/Triple/Aromatic */,
    BondStereoDisplay Stereo /* None/Wedge/Hash/Wavy */,
    DoubleBondPlacement Placement /* Center/Left/Right, from ring side */);
```

This keeps NCDK replaceable and allows other chemistry toolkits (e.g. RDKit coordinates) to feed the same Skia renderer. `MolDraw.Skia` must be usable from Avalonia or server-side code (render to PNG/SVG via `SKSurface`/`SKSvgCanvas`).

## 2. Core (NCDK)

**First inspect and verify** the actual NCDK NuGet package(s) and API (version, target frameworks, transitive dependencies – must not pull in System.Drawing/WPF/WinForms; check license, NCDK is LGPL – document consequences for NuGet distribution). Reuse NCDK for:

- SMILES parsing (`SmilesParser`), MOL/SDF loading where reasonable
- 2D coordinates (`StructureDiagramGenerator`) when missing; keep existing coordinates
- aromaticity perception, implicit hydrogens, ring perception (double-bond placement inside rings)
- stereo: derive wedge/hash bonds from tetrahedral stereo (NCDK's non-planar bond assignment if available)

If a feature is missing in NCDK, explain and implement the smallest reasonable alternative.

API (no static global state, DI-friendly, thread-safe or documented otherwise):

```csharp
public interface IMoleculeLoader
{
    IAtomContainer ParseSmiles(string smiles);              // throws MoleculeParseException with position/details
    bool TryParseSmiles(string smiles, out IAtomContainer? molecule, out string? error);
    IAtomContainer Load(Stream stream, MoleculeFormat format);
}
public interface IDepictionBuilder
{
    MoleculeDepiction Build(IAtomContainer molecule, DepictionBuildOptions? options = null);
}
```

**Atom order guarantee:** depiction atom index `i` must equal the atom index of the parsed molecule, which equals the order of heavy atoms in the input SMILES (same as RDKit `MolFromSmiles`). Do not reorder or add explicit hydrogens unless explicitly requested. Cover this with tests – consumers map per-atom data (e.g. model outputs) by index.

## 3. SkiaSharp renderer

```csharp
public interface IMoleculeRenderer
{
    RenderedMolecule Render(SKCanvas canvas, SKRect bounds, MoleculeDepiction depiction,
                            MoleculeRenderOptions options, MoleculeHighlights? highlights = null);
}

/// screen-space result for hit testing / overlays
public sealed class RenderedMolecule
{
    public SKMatrix Transform { get; }                 // model -> canvas
    public IReadOnlyList<SKPoint> AtomPositions { get; }
    public float AtomRadius { get; }
    public int? HitTestAtom(SKPoint canvasPoint, float tolerance = 0);
    public int? HitTestBond(SKPoint canvasPoint, float tolerance = 0);
}
```

Must support: single/double/triple/aromatic bonds (aromatic as Kekulé or circle – option), ring-aware double-bond placement, atom labels with implicit H placement, implicit carbons, heteroatom colouring (CPK, configurable), formal charges, isotopes, wedge/hashed wedge bonds, automatic scaling/centering with aspect ratio, padding, max bond length (small molecules must not be blown up), configurable bond width / font size / colours, transparent background, light & dark theme presets. Text via `SKFont`/`SKTypeface` (cross-platform default typeface, configurable).

**Highlighting (required):**

```csharp
public sealed record MoleculeHighlights
{
    public IReadOnlyDictionary<int, SKColor> AtomColors { get; init; }   // explicit colours
    public IReadOnlyList<float>? AtomWeights { get; init; }              // 0..1 per atom index
    public Colormap Colormap { get; init; }                              // Viridis, Plasma, Inferno, Magma, Turbo, custom
    public IReadOnlyDictionary<int, SKColor> BondColors { get; init; }
    public int? SelectedAtom { get; init; }
    public int? HoveredAtom { get; init; }
    public float RadiusScale { get; init; }
}
```

Highlights are drawn as filled discs (optionally weight-dependent radius/opacity) **under** bonds and labels; label backgrounds must not hide highlights. Selected/hovered atoms get an outline ring. Optional colour bar legend. Recolouring must not recompute coordinates.

Options are immutable records (`with` expressions), with `MoleculeRenderOptions.Light` / `.Dark` presets.

## 4. MAUI integration (MVVM, no code-behind for consumers)

`MoleculeView : ContentView` hosting an `SKCanvasView`, all as `BindableProperty`:

| Property | Mode | Purpose |
|---|---|---|
| `Smiles` | OneWay | parse + layout (cached) |
| `Molecule` (`IAtomContainer`) | OneWay | alternative input |
| `Depiction` (`MoleculeDepiction`) | OneWay | alternative input (pre-computed / other toolkits) |
| `RenderOptions` | OneWay | null → theme preset (follows `Application.RequestedTheme` changes) |
| `AtomWeights` (`IReadOnlyList<float>`) | OneWay | per-atom highlight 0..1 |
| `AtomColors`, `Colormap`, `ShowColorBar` | OneWay | highlight customisation |
| `SelectedAtomIndex` (`int?`) | TwoWay | click selects/deselects atom |
| `HoveredAtomIndex` (`int?`) | OneWayToSource | pointer hover (desktop) |
| `AtomTappedCommand`, `AtomHoveredCommand` | – | `ICommand` with atom index parameter |
| `ErrorMessage` (`string?`) | OneWayToSource | invalid SMILES → shown in control, never throws into UI |
| `IsInteractive` | OneWay | enable/disable pointer handling |

Requirements: redraw on any property change and size change; pointer handling via MAUI gestures (`TapGestureRecognizer`, `PointerGestureRecognizer`) so it works on all platforms; hit testing delegated to `RenderedMolecule`; layout computed off the UI thread only if it is measurably slow (cache by SMILES). No chemistry/rendering logic in the control beyond wiring. Provide `UseMolDraw()` `MauiAppBuilder` extension (registers SkiaSharp + services).

## 5. Sample app (MVVM, CommunityToolkit.Mvvm)

Molecule list (caffeine, aspirin, glucose, cholesterol, benzene, a charged/isotope-labelled and a stereo example), free SMILES entry with validation message, option toggles (theme, bond width, font size, aromatic style, atom indices), **highlight demo** (random/gradient atom weights, colormap picker, selected/hovered atom shown in the ViewModel), resizable layout. DI via `MauiProgram`. No logic in code-behind.

## 6. NuGet packaging

Three packages – `MolDraw.Core`, `MolDraw.Skia`, `MolDraw.Maui` (depends on the other two) – so the renderer can be used without MAUI (Avalonia/server). Explain this in README/ARCHITECTURE. Metadata: PackageId, Version, Authors, Description, Tags, RepositoryUrl placeholder, license expression, README, snupkg symbols, XML docs, SourceLink, deterministic builds. Public surface minimal (`internal` by default).

## 7. Quality

Nullable, XML docs on public API, DI where useful, no service locator, no static mutable state, no platform-specific rendering code, clear exceptions for invalid input. Tests: SMILES parsing errors, atom-order guarantee, coordinate generation, aromatic/stereo/charge/isotope mapping into the depiction, renderer bounds/aspect ratio/centering, hit testing, highlight drawing (render to `SKBitmap`, assert pixel colours), PNG smoke snapshots for the sample molecules.

## 8. Workflow

1. Analyse NCDK (actual package, API, dependencies, license) and SkiaSharp 3.119 text APIs; write a short findings + architecture proposal and **wait for approval** before implementing.
2. Implement Core → Skia (+ tests) → MAUI → sample → packaging → README + `docs/ARCHITECTURE.md`.
3. `dotnet build` and `dotnet test` must pass; `dotnet pack` must produce the three packages; run the sample on Windows.

README: installation, XAML usage, MVVM example, SMILES vs. existing molecule vs. depiction, highlighting, customisation, supported platforms, known limitations, building the NuGet packages.

No placeholders or pseudocode where a real implementation is reasonably possible. The final solution must compile.
