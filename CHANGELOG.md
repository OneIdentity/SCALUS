# Changelog

All notable changes to SCALUS are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- **Optional configuration removal on uninstall** — `scalus unregister` now accepts
  `--delete-config` (remove the current user's `SCALUS.json` and templates) and
  `--prompt-delete-config` (ask first, via a modal dialog on Windows or a terminal
  prompt elsewhere; the default answer keeps the data). The Windows installer uses
  this during an interactive uninstall to let the user choose whether to keep their
  settings; a silent uninstall (`/qn`) only purges when `DELETECONFIG=1` is passed to
  `msiexec`.

## [2.0.0]

SCALUS 2.0 is a major release that reworks the technology stack, user interface,
and several storage locations. It is **not** a drop-in patch over a 1.x install —
see [UPGRADING.md](UPGRADING.md) for the migration path.

### Added
- **Native desktop configuration app (`scalus-ui`)** — an Angular 20 single-page
  app hosted in a [Photino](https://www.tryphotino.io/) webview. Replaces the old
  local web server + system browser entirely (no browser, no `localhost` port).
- **In-process UI bridge** — the UI calls the shared core directly over a JSON
  message bridge instead of a REST API.
- **Logs & recent launches** — every launch (success or failure) writes a
  structured per-launch record; the UI has a Logs view with drill-down, "Open logs
  folder", and "Export for issue". Failed launches raise a native dialog that
  deep-links the UI to that launch.
- **Preferred-terminal selection** for terminal-hosted (SSH/telnet) launches, with
  automatic detection and a per-user setting.
- **Registration scope + conflict handling** — register per-user or (Windows/Linux)
  all-users via an elevation broker; the UI surfaces when another application owns a
  scheme and can replace it.
- **Custom protocol schemes** can be added and removed from the UI, alongside the
  built-in `rdp`/`ssh`/`telnet` defaults.
- **Import / Export** — whole-config replace and per-application merge (with
  collision handling) from the UI.
- **Single version source + tag-driven releases** — `Directory.Build.props`
  (`<VersionPrefix>`) is the only checked-in version; `vX.Y.Z` git tags cut signed
  releases with a drift guard. See `scripts/version.{ps1,sh}`.
- **macOS and Linux packaging** — `.pkg` + `.tar.gz` (macOS) and `.deb` / `.rpm` /
  `.tar.gz` (Linux), including the Photino runtime dependencies. Windows, macOS, and
  Linux build for both x64 and arm64.
- **AGENTS.md + `.agents/skills/`** for coding-agent onboarding.

### Changed
- **Two binaries instead of one.** `scalus` is a self-contained NativeAOT launcher +
  CLI (the OS hot path); `scalus-ui` is the configuration app. Both share one core
  library (`Scalus.Core`).
- **Rebuilt on .NET 10.** The launcher publishes with NativeAOT for fast cold start
  and a dependency-free deployment.
- **Per-user configuration and logs.** `SCALUS.json` and logs now live in per-user,
  writable locations (Windows `%LOCALAPPDATA%\SCALUS`, macOS
  `~/Library/Application Support/SCALUS` + `~/Library/Logs/SCALUS`, Linux XDG
  `~/.config/scalus` + `~/.local/state/scalus/logs`) instead of next to the binary,
  so editing settings never needs elevation. Debug logging is on by default.
- **Serialization** moved from Newtonsoft.Json to System.Text.Json source
  generators; **dependency injection** from Autofac to
  Microsoft.Extensions.DependencyInjection; **argument parsing** to
  System.CommandLine — all to support NativeAOT and remove runtime reflection.
- **Single unified edition.** The old community/official edition split was removed.
- **Platform-filtered seeding.** A single master seed (`src/SCALUS.json`) tags each
  application with its valid platforms and is filtered to the running OS on first
  run, replacing the hand-synced per-platform seed files.
- **CI/CD** rebuilt as a trunk-based, tag-driven Azure pipeline; packaging runs on
  PRs (unsigned), and signing/notarization/release are gated on release tags.

### Removed
- The ASP.NET Core Kestrel web server, REST controllers, browser-based settings UI,
  and the two-DI-container bridge hack.
- The separate Swift `scalusmac` helper (macOS default-handler calls now happen in
  the core).
- The Cake build (`build.cake`); replaced by `build.ps1` / `build.sh` +
  `scripts/`.
- The Angular 14 app and its legacy dependencies (Elemental UI, jQuery, etc.).

### Migration
- Windows 2.0 MSI upgrades a 1.x install in place (same `UpgradeCode`), but
  configuration does not move automatically — bring old settings forward with
  **Import** in the new UI. Older on-disk template forms are migrated automatically
  on first load. Full details in [UPGRADING.md](UPGRADING.md).

[Unreleased]: https://github.com/OneIdentity/SCALUS/compare/v2.0.0...HEAD
[2.0.0]: https://github.com/OneIdentity/SCALUS/releases/tag/v2.0.0
