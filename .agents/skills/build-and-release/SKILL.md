---
name: build-and-release
description: Use when working on SCALUS build/publish/package scripts, versioning, the Azure pipeline, code signing/notarization, or cutting a GitHub release.
---

# SCALUS Build & Release

Read this when touching `scripts/`, `build.ps1`/`build.sh`, `Directory.Build.props`,
`azure-pipelines.yml`, or anything about versions, signing, or releases.

## Versioning — one source of truth

`Directory.Build.props` `<VersionPrefix>` (currently `2.0.0`) is the **only**
checked-in version. Nothing else hardcodes a version. Every script that needs a
version falls back to reading `VersionPrefix` when `-Version`/`--version` is
omitted, so local builds match CI.

`scripts/version.ps1` (Windows) and `scripts/version.sh` (Linux/macOS) are the
CI version-derivation logic and are identical in behavior:

- **Non-tag build** (trunk / PR / manual): `Version = X.Y.Z.<buildCounter>`,
  `IsPrerelease = true`. Installer versions are always purely numeric (WiX/fpm
  reject semver pre-release suffixes); "prerelease" is a flag, not a `-pre`
  string.
- **Tag build** (`refs/tags/vX.Y.Z`): `Version = X.Y.Z`, `IsPrerelease = false`.
  **Drift guard:** the tag must equal `VersionPrefix` exactly or the build fails —
  bump `VersionPrefix` in the same commit you tag.

They emit pipeline variables `Version`, `IsPrerelease`, `IsTagBuild`,
`ReleaseTag`, and set the build number.

Azure's bounded `buildCounter` is used instead of `Build.BuildId`, so the complete
CI version can also be used for CLR `AssemblyVersion` and Windows `FileVersion`;
their numeric components are limited to 65535.

## Cutting a release

1. Land the target `<VersionPrefix>` on `master` (and a CHANGELOG entry).
2. `git tag vX.Y.Z && git push origin vX.Y.Z` (tag must match VersionPrefix).
3. The tag build signs/notarizes all artifacts and drafts the GitHub release.

## Local builds (installers)

`build.ps1` (Windows) / `build.sh` (Linux/macOS) are thin wrappers: **test →
publish → package** for one runtime.

```powershell
./build.ps1 -Runtime win-x64            # test, publish, WiX MSI -> Output\Release\win-x64
./build.ps1 -Runtime win-x64 -SkipTests -SignFiles
```
```bash
./build.sh --runtime osx-x64            # -> .pkg + .tar.gz
./build.sh --runtime linux-x64          # -> .deb + .rpm + .tar.gz
```

Under the hood: `scripts/publish.{ps1,sh}` publishes the NativeAOT `scalus` CLI +
the Photino `scalus-ui` payload (which triggers the Angular build), then the
per-OS packager runs:

| OS | Packager | Produces |
|----|----------|----------|
| Windows | `scripts/Win/package.ps1` + `Product.wxs` (WiX 5) | `.msi` |
| macOS | `scripts/Osx/package.sh` (+ `applet`) | `.pkg`, `.tar.gz` |
| Linux | `scripts/Linux/package.sh` (fpm) | `.deb`, `.rpm`, `.tar.gz` |

The Windows MSI keeps a stable `UpgradeCode` with `MajorUpgrade
AllowSameVersionUpgrades="yes"`, so 2.x upgrades a 1.x install in place. Linux
packages declare the Photino runtime deps (`libwebkit2gtk-4.1`, `gtk3`,
`libnotify`).

## The pipeline (`azure-pipelines.yml`)

Trunk-based, tag-driven. `isTagBuild` = ref starts with `refs/tags/`.

Jobs:
- **BuildAndTest** — derive version, install .NET 10, `dotnet test`. Every push/PR.
- **Windows / Linux / Mac** — each a `matrix` over two RIDs (`{os}-x64`,
  `{os}-arm64`): derive version, install SDK + Node (for the Angular build) +
  per-OS packaging toolchain, publish `scalus` + `scalus-ui`, package.
  These run **on PRs too, unsigned** (`condition: succeeded()`).
- **Release** — `GitHubRelease@1` via `gitHubConnection: 'PangaeaBuild-GitHub'`.
  Tag builds only.

**Signing / notarization is decoupled from packaging** and gated on
`eq(variables.isTagBuild, true)`:
- Windows: Azure Key Vault cert → eSignerCKA → sign the MSI (`-SignFiles` only when
  `isTagBuild`).
- macOS: Apple product + code signing certs → `productsign` → notarize/staple.
- Linux: unsigned packages.

So a PR produces installable but unsigned artifacts; only a `vX.Y.Z` tag signs and
releases.

### Service connection

`PangaeaBuild-GitHub` is the shared One Identity Azure DevOps GitHub connection
(same one the Safeguard SDK repos use). The GitHubRelease task publishes to
`OneIdentity/SCALUS`. **Open item:** confirm this connection has write scope to
the SCALUS repo, or the tag-build release step 403s.

## AOT watch-items

- The `scalus` CLI publishes with NativeAOT. Keep trim/AOT warnings clean; use the
  source-gen JSON context (`Util/ScalusJson.cs`), not reflection serialization.
- `CA1416` (platform-compatibility) is suppressed **unconditionally** in the
  csprojs on purpose — AOT/self-contained publish builds with `Platform=x64`, and a
  `Platform=AnyCPU`-conditioned `NoWarn` would silently stop applying there. Don't
  re-scope that suppression back to a configuration/platform condition.

## Related

- `.github/workflows/dotnet-core.yml` is a lightweight GitHub Actions build+test on
  PRs (coexists with the Azure pipeline).
