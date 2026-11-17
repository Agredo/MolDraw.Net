# Publishing MolDraw.Net to NuGet

Everything needed to get the four packages onto nuget.org under the publisher **Agredo Application**, and to repeat it
for every release. Maintainer runbook — run it from a machine with the .NET 10 (or later) SDK and the MAUI workloads.

| Package | Role | Depends on |
|---|---|---|
| `MolDraw.Depiction` | model + `IChemistryEngine` seam | – |
| `MolDraw.Core` | NCDK engine | `MolDraw.Depiction`, NCDK 1.6.1 (LGPL-2.1) |
| `MolDraw.Skia` | SkiaSharp renderer | `MolDraw.Depiction`, SkiaSharp 3.119.x |
| `MolDraw.Maui` | MAUI `MoleculeView` control | `MolDraw.Skia`, MAUI 10, SkiaSharp |

Repository: <https://github.com/Agredo/MolDraw.Net> — this URL is baked into every package as `ProjectUrl` and
`RepositoryUrl` (`Directory.Build.props`).

## 0. Checklist for a release

```text
[ ]  1. CHANGELOG.md: move "Unreleased" entries into the new version section
[ ]  2. Directory.Build.props: bump <Version>
[ ]  3. pwsh ./build/Pack.ps1                      (tests + pack all four MAUI targets)
[ ]  4. pwsh ./build/Test-NuGetPackages.ps1        (metadata, icon, README, dependency graph)
[ ]  5. commit + tag v<version>, push to GitHub     (triggers the Release workflow)
[ ]  6. publish: pwsh ./build/Push-NuGet.ps1       (or let the Release workflow do it)
[ ]  7. verify the package pages on nuget.org and a clean `dotnet add package` in an empty project
[ ]  8. create the GitHub release with the packages attached
```

## 1. One-time setup on nuget.org

1. **Account & publisher name.** Sign in at <https://www.nuget.org> and set the profile name to
   *Agredo Application*. This profile name is what a package page shows next to the package (`Authors` from the
   `.nuspec` is `Agredo Application` as well), and it is the account that owns the package IDs.
2. **Two-factor authentication.** Required for pushing; enable it before creating the API key.
3. **Reserve the `MolDraw.` prefix (optional but recommended).** Account → *Prefix reservation* → request `MolDraw`
   with the GitHub repository `https://github.com/Agredo/MolDraw.Net` as evidence. A reserved prefix gives the packages
   a verified check mark and blocks squatting. Reservation must be approved by nuget.org; it is not needed for the first
   push.
4. **Create the API key.** Account → *API keys* → *Create*:

   | Field | Value |
   |---|---|
   | Key name | `MolDraw.Net release` |
   | Expiration | 90–365 days (shortest you can tolerate) |
   | Scopes | **Push new packages and package versions** + **Push symbol packages** |
   | Glob pattern | `MolDraw.*` |
   | Packages | leave empty (new packages are matched by the glob) |

   Copy the key once — nuget.org never shows it again. Do **not** commit it. Store it as
   `NUGET_API_KEY` in your environment or as a GitHub Actions secret:

   ```powershell
   # current session only
   $env:NUGET_API_KEY = 'oy2...'
   ```

   ```bash
   # GitHub repository secret for the Release workflow
   gh secret set NUGET_API_KEY --body 'oy2...'
   ```

5. **Package ID availability.** Verified on 2026-11-18: `MolDraw.Core`, `MolDraw.Depiction`, `MolDraw.Skia` and
   `MolDraw.Maui` are all still free. IDs are claimed by the first successful push, so publish all four in one go.

## 2. Versioning

* The single source of truth is `<Version>` in `Directory.Build.props`; it flows into all four packages.
* Pre-releases use a SemVer suffix: `0.4.0-beta.1`, `0.4.0-rc.1`.
* Before 1.0.0, breaking changes go into a **minor** bump (`0.3.x` → `0.4.0`); fixes into a patch.
* A published version can never be overwritten or re-uploaded — only unlisted. If you need to fix something, bump.
* Tag releases as `v<version>` (`v0.3.0`); the Release workflow refuses a tag that does not match the version.

## 3. Build and pack

```powershell
pwsh ./build/Pack.ps1                 # tests, cleans artifacts/packages, packs all MAUI targets
pwsh ./build/Pack.ps1 -SkipTests      # when you just ran the tests
```

`Pack.ps1` runs `dotnet pack MolDraw.Net.slnx -c Release -p:MolDrawAllTargets=true -p:NoWarn=NU1507`. `MolDrawAllTargets`
is required: `MolDraw.Maui` only builds the platform TFMs of the current host otherwise, and the package would be
missing `lib/net10.0-android36.0`, `lib/net10.0-ios26.0` or `lib/net10.0-maccatalyst26.0`.

Requirements for the full package:

| Workload | Needed for |
|---|---|
| `maui-windows` | `net10.0-windows10.0.19041.0` |
| `maui-android` | `net10.0-android` |
| `maui-ios`, `maui-maccatalyst` | `net10.0-ios`, `net10.0-maccatalyst` (compiling the library is enough; no Mac needed unless you publish an app) |

Result: `artifacts/packages` contains four `.nupkg` and four `.snupkg` files.

## 4. Verify before publishing

```powershell
pwsh ./build/Test-NuGetPackages.ps1
```

The script opens each `.nupkg` and checks:

* metadata — id, version, `Authors` = *Agredo Application*, `ProjectUrl`/`RepositoryUrl` = the GitHub repo,
  Apache-2.0 licence expression, icon and README declared, description length, tags;
* embedded root files — `README.md`, `LICENSE`, `THIRD-PARTY-NOTICES.md`, `icon.png`;
* dependency graph — `MolDraw.Maui` must **not** depend on `MolDraw.Core`/NCDK, `MolDraw.Skia` must **not** depend on
  NCDK, `MolDraw.Depiction` must have no dependencies at all;
* `lib/<tfm>/` for all four MAUI target frameworks including `MolDraw.Maui.pri`;
* README links are absolute (nuget.org cannot resolve relative Markdown links) and contain an install snippet.

Optional end-to-end smoke test against a scratch project — this is what a consumer sees:

```powershell
mkdir $env:TEMP/seam-check; cd $env:TEMP/seam-check
dotnet new console; dotnet add package MolDraw.Skia --source C:\Projects\MolDraw.Net\artifacts\packages
dotnet add package MolDraw.Core --source C:\Projects\MolDraw.Net\artifacts\packages
dotnet run
```

## 5. Publish

```powershell
$env:NUGET_API_KEY = 'oy2...'
pwsh ./build/Push-NuGet.ps1            # asks for confirmation, pushes nupkg + snupkg in dependency order
```

The script pushes `MolDraw.Depiction` → `MolDraw.Core` → `MolDraw.Skia` → `MolDraw.Maui` and then the four symbol
packages. `--skip-duplicate` makes a re-run harmless (an already-published version is skipped instead of failing).

Symbols: `.snupkg` files are accepted by the same endpoint and routed to the nuget.org symbol server, so debuggers can
step into MolDraw source and the `.pdb` matches the shipped assemblies.

If the GitHub Release workflow is enabled (see §7), pushing a `v*` tag does all of this automatically.

### SourceLink (optional, once the repo has a git history)

`PublishRepositoryUrl` and `EmbedUntrackedSources` are already enabled, so the packages carry the repository URL.
Adding [Microsoft.SourceLink.GitHub](https://www.nuget.org/packages/Microsoft.SourceLink.GitHub) (currently 11.0.100)
to the four library projects gives nuget.org's "Source repository" deep links and F11-into-source in the debugger:

```xml
<!-- src/MolDraw.Core/MolDraw.Core.csproj (and the other three) -->
<PackageReference Include="Microsoft.SourceLink.GitHub" Version="11.0.100" PrivateAssets="all" />
```

It needs a real git clone with a GitHub remote; source link paths are resolved from the commit that produced the build.
That is why it is not wired up yet: this working copy has no usable `.git`. Add the reference and the
`Directory.Packages.props` entry after the repository is pushed, then pack again.

## 6. After publishing

1. nuget.org indexing takes a few minutes. Check the four pages:
   <https://www.nuget.org/packages/MolDraw.Depiction>, `…/MolDraw.Core`, `…/MolDraw.Skia`, `…/MolDraw.Maui`.
2. Look at each page as a first-time user: icon, README rendering, dependency list, licence, "Source repository" link.
   Fix anything that reads badly in the README and bump the version (the README can be updated for the *next* release
   only).
3. Verify from a clean project (no local feed in `nuget.config`):

   ```powershell
   dotnet new console -o $env:TEMP/nuget-smoke
   cd $env:TEMP/nuget-smoke
   dotnet add package MolDraw.Skia
   dotnet add package MolDraw.Core
   ```

4. On the package page, use *Edit package* only for metadata that is not in the `.nuspec` — the README and description
   come from the package itself.

## 7. GitHub Actions (optional automation)

| Workflow | Trigger | Does |
|---|---|---|
| `.github/workflows/ci.yml` | push/PR to `main` | build + test on Windows (full solution) and Linux (Depiction/Core/Skia); packs the cross-platform package as a check |
| `.github/workflows/release.yml` | tag `v*` or manual | verifies tag == version, packs, attaches the packages to a GitHub release, pushes to nuget.org when `NUGET_API_KEY` is set |

Requirements: the `NUGET_API_KEY` secret (see §1). The release job installs the `maui`, `maui-ios` and
`maui-maccatalyst` workloads on `windows-latest`, which takes a few minutes. If the runner cannot provide them, pack
locally with `./build/Pack.ps1` and push with `./build/Push-NuGet.ps1` — the outcome is identical.

## 8. Unlist, deprecate, yank

* **Wrong README/metadata only** → fix and release a new patch; the old page stays as is.
* **Unlist** (package page → *Manage package* → *Listing*): hides a version from search and `dotnet add package`, but
  existing `PackageReference` restores keep working. Use for broken releases and always publish a fixed version.
* **Deprecate**: shows a banner in the UI pointing to the replacement version/package (`MolDraw.Core` →
  `MolDraw.Depiction` use cases).
* A version number is never free again; do not delete and re-upload.

## 9. Troubleshooting

| Symptom | Cause / fix |
|---|---|
| `403 Forbidden` on push | API key expired, wrong glob, or missing *Push new packages* scope. Create a new key. |
| `409 Conflict` | That version already exists. Bump `<Version>`; `--skip-duplicate` turns this into a no-op. |
| `NU5039` / package page shows no README | `README.md` was not packed. Check the `<None … Pack="true">` items in `Directory.Build.props` and `-p:PackageReadmeFile`. |
| `NU5046` / no icon on nuget.org | `assets/icon.png` missing or not packed as `icon.png` at the package root. Regenerate with `pwsh ./build/New-PackageIcon.ps1`. |
| `MolDraw.Maui` misses Android/iOS/MacCatalyst | Pack without `-p:MolDrawAllTargets=true`, or workloads not installed. |
| Consumers see NCDK in a MAUI-only app | The app references `MolDraw.Core`; `MolDraw.Maui` itself never pulls NCDK (verify with `dotnet list package --include-transitive`). |
| `NU1507` build error on a developer machine | Several package sources configured; pass `-p:NoWarn=NU1507` (the scripts already do). |
| Symbols not found while debugging | The `.snupkg` was not pushed, or the consumer has no symbol source (`https://api.nuget.org/v3/index.json`). |

## 10. Legal checklist for the packages

* `LICENSE` (Apache-2.0) and `THIRD-PARTY-NOTICES.md` are embedded in every package root.
* `MolDraw.Core` declares NCDK 1.6.1 as a **NuGet dependency** (LGPL-2.1, dynamically linked, unmodified). The
  LGPL obligations for redistributing apps are described in `THIRD-PARTY-NOTICES.md`; do not statically link or bundle
  `NCDK.dll` into the packages.
* Colour-map data (Viridis/Plasma/Inferno/Magma: CC0; Turbo: Apache-2.0) is noted in the same file.
* No secrets, no private feeds, no sample-app keys in the packed files — `Test-NuGetPackages.ps1` fails the release if
  the metadata or the dependency graph is wrong.
