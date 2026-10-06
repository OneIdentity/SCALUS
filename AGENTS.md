# AGENTS.md — SCALUS

SCALUS (**Session Client Application Launch Uri System**) registers the operating
system's URI-protocol handlers (e.g. `rdp://`, `ssh://`, `telnet://`) and launches
native session clients in response. It ships as two cooperating binaries that
share one core library:

- **`scalus`** — a self-contained NativeAOT launcher + CLI (URL dispatch,
  `register`/`unregister`/`info`/`verify`). This is the OS hot path.
- **`scalus-ui`** — a native desktop configuration app: an Angular front end
  hosted in a [Photino](https://www.tryphotino.io/) window (no browser, no local
  web server).

Cross-platform on **Windows, macOS, and Linux**, built on **.NET 10**.

This file is the agent orchestrator. Read it first, then load the on-demand skill
that matches your task.

## Project structure

```
SCALUS/
├── src/                          # .NET sources
│   ├── OneIdentity.Scalus.csproj # Scalus.Core.dll — shared core (dispatch, config,
│   │                             #   templates, platform services, registration)
│   ├── UrlParser/                # URI parsing (rdp/ssh/telnet/default parsers)
│   ├── Launch/ Register/ Unregister/ Info/ Verify/   # CLI verb implementations
│   ├── Platform/                 # OS-specific services (Windows/MacOS/Linux)
│   │   └── {Windows,MacOS,Linux}/
│   ├── Util/                     # ConfigurationManager, ScalusJson, PlatformFilter, ...
│   ├── Dto/                      # Serialized config/data-transfer types
│   ├── Ioc.cs                    # Dependency-injection composition root
│   ├── CommandLineRunner.cs      # Shared verb parsing/bootstrap (used by both binaries)
│   ├── Cli/Scalus.Cli.csproj     # `scalus` — NativeAOT launcher/CLI apphost
│   └── Scalus.Ui/Scalus.Ui.csproj# `scalus-ui` — Photino desktop host
│       └── BridgeDispatcher.cs   # Handles UI bridge calls (the UI<->C# contract)
├── ui/                           # Angular 22.2 front end (see ui/README.md)
│   └── src/app/core/bridge/      # ScalusBridge interface + Photino/Mock impls
├── test/                         # OneIdentity.Scalus.Test.csproj (xUnit)
├── scripts/                      # Build/publish/package + version derivation
│   ├── publish.{ps1,sh}          # Publish the NativeAOT CLI + Photino UI payload
│   ├── version.{ps1,sh}          # CI version derivation (tag/trunk, drift guard)
│   ├── Win/   (package.ps1, Product.wxs)     # WiX 5 MSI
│   ├── Osx/   (package.sh, applet)           # .pkg + .tar.gz
│   └── Linux/ (package.sh)                    # .deb / .rpm / .tar.gz
├── build.ps1 / build.sh          # Local one-shot: test -> publish -> package
├── Directory.Build.props         # SINGLE checked-in version source (VersionPrefix)
├── azure-pipelines.yml           # CI/CD (trunk + tag-driven releases)
├── UPGRADING.md                  # 1.x -> 2.0 migration guide
└── .agents/skills/               # On-demand skill references (see routing table)
```

## Setup and build commands

Prerequisites: **.NET 10 SDK** and **Node.js 22.x** (the UI project builds the
Angular app during its .NET build).

```powershell
# Build everything (Core, CLI, UI). The UI build runs `npm ci` + `npm run build`.
dotnet build scalus.sln -c Debug

# Run the desktop configuration app against the live C# host
dotnet run --project src/Scalus.Ui/Scalus.Ui.csproj

# Build just the CLI launcher (fast; skips the Angular build)
dotnet build src/Cli/Scalus.Cli.csproj -c Debug
```

Fast UI-only iteration (browser dev mode, backed by mock data) lives in
`ui/` — `npm ci && npm start`. See `ui/README.md`.

### Local packaged build (installers)

`build.ps1` / `build.sh` run test → publish → package for one runtime:

```powershell
./build.ps1 -Runtime win-x64                 # -> Output\Release\win-x64\*.msi
```
```bash
./build.sh --runtime osx-x64                  # -> Output/Release/osx-x64/*.pkg + *.tar.gz
./build.sh --runtime linux-x64                # -> *.deb / *.rpm / *.tar.gz
```

When `-Version`/`--version` is omitted, every build/publish/package script reads
`<VersionPrefix>` from `Directory.Build.props`. Never hardcode a version. For a
quick version check without a full build, use
`dotnet msbuild <proj> -getProperty:Version`.

## Testing

xUnit tests live in `test/`. There is no live-service dependency.

```powershell
dotnet test test/OneIdentity.Scalus.Test.csproj -c Debug
```

UI unit specs (Karma) run from `ui/` with `npm test`.

## Code conventions

- **Two binaries, one core.** Shared logic (verb parsing, config bootstrap,
  registration, dispatch) belongs in `OneIdentity.Scalus` (`Scalus.Core`) so both
  `scalus` and `scalus-ui` behave identically. Don't fork logic into an apphost.
- **NativeAOT-safe.** The CLI publishes with NativeAOT. Avoid unbounded
  reflection and unsupported dynamic codegen; use the source-generated JSON
  context in `Util/ScalusJson.cs` and keep trim/AOT warnings clean.
- **Platform code is isolated.** OS-specific behavior lives under
  `src/Platform/{Windows,MacOS,Linux}` behind interfaces resolved in `Ioc.cs`.
  Guard Windows-only P/Invoke with `RuntimeInformation.IsOSPlatform`.
- **Config is JSON, case-insensitive on read.** `SCALUS.json` round-trips through
  `ScalusJson`; legacy/PascalCase and older template forms still load and are
  migrated (see the configuration skill).
- **The UI talks over the bridge, never HTTP.** Adding a capability means adding a
  bridge method on both sides — see the ui-bridge skill.
- **No secrets, ever.** SCALUS stores launch templates, not credentials.

## CI/CD

`azure-pipelines.yml` is the pipeline. Trunk-based with tag-driven releases:
build & test on every push/PR to `master`; packaging jobs (Win/Linux/Mac, x64 &
arm64) also run on PRs but **unsigned**; a `vX.Y.Z` tag signs/notarizes and drafts
a GitHub release. Full job layout, signing, and service connections are in the
build-and-release skill. (A lightweight `.github/workflows/dotnet-core.yml` also
runs a plain build+test on PRs.)

## Versioning

One checked-in source of truth: `Directory.Build.props` (`<VersionPrefix>`).
Nothing hardcodes a version. Local/trunk builds → `X.Y.Z.<build>` prerelease;
a `vX.Y.Z` git tag → clean `X.Y.Z` release. `scripts/version.{ps1,sh}` derive the
build version and **hard-fail** if a release tag disagrees with `VersionPrefix`.
To cut a release: land the desired `VersionPrefix` on `master`, then
`git tag vX.Y.Z && git push origin vX.Y.Z`. Details in the build-and-release skill
and `UPGRADING.md`.

## Security

- Never commit secrets or credentials. SCALUS itself stores no credentials.
- Registration writes OS handler entries; treat elevation paths (the all-users
  broker that shells out to a sibling `scalus`) with care.
- Keep NativeAOT/trim warnings clean — silent reflection failures ship as runtime
  bugs.

## On-demand skills

Read the matching `SKILL.md` when your task fits the trigger.

| Skill | When to read | File |
|-------|-------------|------|
| Architecture | Understanding component boundaries, URI dispatch flow, registration, the config/template model | [`.agents/skills/architecture/SKILL.md`](.agents/skills/architecture/SKILL.md) |
| Build and Release | Working on build/publish/package scripts, versioning, the pipeline, signing/notarization, GitHub releases | [`.agents/skills/build-and-release/SKILL.md`](.agents/skills/build-and-release/SKILL.md) |
| UI Bridge | Working on the Angular UI or adding/altering a host bridge method end-to-end | [`.agents/skills/ui-bridge/SKILL.md`](.agents/skills/ui-bridge/SKILL.md) |
| Configuration | Working on `SCALUS.json`, applications/protocols/templates, seeding, platform filtering, or legacy migration/import | [`.agents/skills/configuration/SKILL.md`](.agents/skills/configuration/SKILL.md) |

## Keeping this file current

After completing tasks, propose updates for new patterns, corrections, or skill
changes. Keep the project-structure tree, the skills routing table, and the
CI/testing pointers aligned with the actual files.
