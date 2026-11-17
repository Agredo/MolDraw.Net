# Security policy

## Supported versions

MolDraw.Net is pre-1.0; security fixes go into the newest release line.

| Version | Supported |
|---|---|
| 0.3.x | ✅ |
| ≤ 0.2.x | ❌ |

## Reporting a vulnerability

Please **do not** open a public issue. Use GitHub's private reporting:

<https://github.com/Agredo/MolDraw.Net/security/advisories/new>

Include the affected package and version, a description of the impact, and a reproducer (SMILES/molfile, code snippet,
platform and architecture). You can expect an acknowledgement within a few days and a coordinated disclosure once a fix
is available. Please give us a reasonable window before publishing details.

## What is in scope

* Rendering, parsing and hit-testing code in `MolDraw.Depiction`, `MolDraw.Core`, `MolDraw.Skia` and `MolDraw.Maui`.
* Denial-of-service style problems triggered by untrusted input — huge molecules, deeply nested SMILES, pathological
  layout times, unbounded cache growth in `MoleculeView`.
* Anything that lets input escape its layer (e.g. a file path or format string taken from a molecule file).

Dependency vulnerabilities (NCDK, SkiaSharp, .NET MAUI) are best reported upstream; tell us anyway if MolDraw.Net needs a
version bump or a workaround.

## What is out of scope

* Memory-safety bugs inside native Skia binaries or the NCDK InChI natives — report those to
  [SkiaSharp](https://github.com/mono/SkiaSharp/security) or [NCDK](https://github.com/kazuyaujihara/NCDK).
* Applications that render untrusted SMILES without any size/time limits: MolDraw.Net parses what you hand it, the app
  decides what it accepts.
* Depiction quality or rendering differences (those are normal bugs, use the issue tracker).

## Hardening notes

* Only `MolDraw.Core` pulls in NCDK (LGPL-2.1). Use `MolDraw.Depiction` + `MolDraw.Skia` + `MolDraw.Maui` with your own
  `IChemistryEngine` to keep NCDK out of the process entirely.
* No package reads or writes files, opens sockets or downloads anything at runtime. `MoleculeView` caches depictions in
  memory only (LRU, `cacheCapacity` in `UseMolDraw()`).
* Libraries are built deterministically with nullable reference types and warnings as errors; the `.snupkg` symbols ship
  alongside every package.
