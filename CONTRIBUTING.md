# Contributing to MolDraw.Net

Thanks for helping out! MolDraw.Net renders and highlights 2D chemical structures for .NET and .NET MAUI. Bug reports,
depiction-quality feedback (with an image!) and engine adapters (RDKit, Indigo, …) are all welcome.

## Code of conduct

Participation is covered by [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md). Be kind, assume good intent.

## Reporting bugs

Open an [issue](https://github.com/Agredo/MolDraw.Net/issues/new/choose) and include:

* the package and version (`MolDraw.Skia` 0.3.0, …), .NET SDK, OS and architecture (x64/ARM64),
* the SMILES or molfile that shows the problem (if it is proprietary, describe it or use a similar public structure),
* expected vs. actual picture — a screenshot or the exported PNG/SVG helps enormously,
* the exception type and stack trace for crashes.

For **security issues** do not open an issue; follow [SECURITY.md](SECURITY.md).

## Development setup

Requirements:

* .NET SDK 10 or later (the packages target `net10.0`).
* Windows: the `maui-windows` workload (`dotnet workload install maui-windows`) — enough to build and test everything.
* For the full package with all four MAUI targets: the `maui-android`, `maui-ios` and `maui-maccatalyst` workloads.

```shell
dotnet build MolDraw.Net.slnx
dotnet test MolDraw.Net.slnx
dotnet run --project samples/MolDraw.Maui.Sample        # Windows sample app
```

If your machine has several NuGet sources configured, add `-p:NoWarn=NU1507` to `restore`/`build`/`test`/`pack` (the
library projects treat warnings as errors).

## Project layout

| Path | Purpose |
|---|---|
| `src/MolDraw.Depiction` | Toolkit-neutral model + `IChemistryEngine`. **No dependencies, no NCDK, no UI.** |
| `src/MolDraw.Core` | NCDK engine: parsing, layout, perception → `MoleculeDepiction`. |
| `src/MolDraw.Skia` | SkiaSharp renderer, highlights, exporter. Must not reference NCDK or MAUI. |
| `src/MolDraw.Maui` | `MoleculeView` control. Must not reference NCDK. |
| `tests/MolDraw.Core.Tests` | Parsing, layout, engine seam, RDKit-compatible fixtures. |
| `tests/MolDraw.Skia.Tests` | Rendering snapshots, highlights, hit testing, export. |
| `samples/MolDraw.Maui.Sample` | MAUI demo app (SMILES picker, peak highlighting). |
| `docs/ARCHITECTURE.md` | Layering rules and why the seam exists. |
| `docs/PUBLISHING.md` | How the NuGet packages are built and published. |

### Layering rules (important)

1. `MolDraw.Depiction` never references another MolDraw package, NCDK or SkiaSharp.
2. `MolDraw.Skia` never references NCDK and never references MAUI.
3. `MolDraw.Maui` never references `MolDraw.Core`.
4. Only `MolDraw.Core` / `MolDraw.Core.Ncdk` expose NCDK types in public signatures.

If a change seems to require breaking one of these, open an issue first — the separation is the product.

## Coding style

* C# latest, nullable enabled, file-scoped namespaces, `var` only when the type is obvious.
* Library projects build with `TreatWarningsAsErrors`, `GenerateDocumentationFile` and analyzers from the .NET SDK.
  Public members need XML documentation (`<summary>`, `<param>`, `<returns>`, `<exception>`).
* Immutability by default: records/`init` properties, no static mutable state. Services must be thread-safe; the tests
  rely on it.
* No NCDK type in an exception message that a UI shows — use `MoleculeParseException`/`DepictionException`.

## Tests

* xUnit. Add tests next to the existing ones and keep the suites green (currently 139 tests).
* Rendering changes: check the PNG snapshots written to `artifacts/snapshots` and describe the intended change in the
  pull request.
* Depiction/layout changes: add or update a fixture that was produced with RDKit and mention the RDKit version in the
  test.
* Run the smallest useful subset while iterating:

```shell
dotnet test tests/MolDraw.Skia.Tests
dotnet test tests/MolDraw.Core.Tests --filter FullyQualifiedName~ChemistryEngine
```

## Pull requests

* One topic per pull request; keep unrelated reformatting out.
* Update `README.md`, `docs/ARCHITECTURE.md`, the package READMEs (`src/*/README.md`) and `CHANGELOG.md`
  (**Unreleased** section) when behaviour or public API changes.
* Public API changes need a migration note in `CHANGELOG.md`; breaking changes need a minor version bump before 1.0.0.
* CI must be green (build + tests on Windows and Linux).

## Releasing

Releases are cut by the maintainers — see [docs/PUBLISHING.md](docs/PUBLISHING.md).

## License

By contributing you agree that your contribution is licensed under the [Apache License 2.0](LICENSE), and you confirm
that you have the right to submit it (no copied code from incompatible licences, e.g. GPL toolkits — LGPL/MIT/Apache
dependencies go through NuGet, not into the repository).
