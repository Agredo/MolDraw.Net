# Third-party notices

MolDraw.Net is licensed under the Apache License 2.0. The packages depend on or include material from:

| Component | Used by | License | Notes |
|---|---|---|---|
| [NCDK](https://github.com/kazuyaujihara/NCDK) 1.6.1 (C# port of the Chemistry Development Kit) | MolDraw.Core (NuGet dependency, not redistributed in our packages) | LGPL-2.1-only | Source: https://github.com/kazuyaujihara/NCDK |
| MathNet.Numerics.Signed (transitive via NCDK) | MolDraw.Core | MIT | |
| [SkiaSharp](https://github.com/mono/SkiaSharp) 4.156 | MolDraw.Skia, MolDraw.Maui | MIT | |
| .NET MAUI | MolDraw.Maui | MIT | |
| Viridis, Plasma, Inferno, Magma colour maps (sampled values) | MolDraw.Skia | CC0 (Matplotlib, van der Walt / Smith / Huang) | |
| Turbo colour map (sampled values) | MolDraw.Skia | Apache-2.0 (Google LLC) | |

## NCDK / LGPL-2.1

Applications that ship `MolDraw.Core` also ship `NCDK.dll`, which is covered by the GNU Lesser General Public License v2.1.
When you distribute such an application you must, among other things: provide the LGPL license text and a notice that NCDK
is used, make the NCDK source available (or point to it), and allow users to replace `NCDK.dll` with a modified version.
Dynamic linking via the separate NuGet package (as done here) satisfies the relinking requirement on platforms that load
assemblies dynamically (Windows, Android with JIT). Ahead-of-time compiled/statically linked targets (iOS, NativeAOT) and
trimmed builds need extra care. This is not legal advice.

`MolDraw.Depiction` and `MolDraw.Skia` do not depend on NCDK.
