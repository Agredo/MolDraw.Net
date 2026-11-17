## What does this pull request change?

<!-- One or two sentences. Link the issue it fixes: "Fixes #123". -->

## Type

- [ ] Bug fix (no public API change)
- [ ] New feature (additive)
- [ ] Breaking change (needs a migration note in CHANGELOG.md)
- [ ] Documentation / samples only
- [ ] Packaging / CI

## Checklist

- [ ] `dotnet build MolDraw.Net.slnx` and `dotnet test MolDraw.Net.slnx` are green locally
- [ ] Public API has XML documentation; library builds without warnings (`TreatWarningsAsErrors`)
- [ ] Layering rules kept (`MolDraw.Depiction` has no dependencies, `MolDraw.Skia` no NCDK/MAUI, `MolDraw.Maui` no NCDK)
- [ ] Tests added or updated for the behaviour this change introduces
- [ ] `CHANGELOG.md` updated (**Unreleased**) — with a migration note if the public API changed
- [ ] README / `docs/ARCHITECTURE.md` / package README (`src/<Package>/README.md`) updated if behaviour or usage changed
- [ ] Rendering changes: before/after PNG or SVG attached

## Screenshots (for depiction/rendering changes)

<!-- Drag images here. Structures that show the difference are more useful than prose. -->
