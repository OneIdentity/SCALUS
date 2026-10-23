# SCALUS Developer Guide

SCALUS consists of 3 major components:

* The CLI (`scalus`): A self-contained native launcher that dispatches URLs to
  applications and registers/unregisters SCALUS as the OS protocol handler.
* The GUI (`scalus-ui`): A Photino-hosted Angular desktop app that edits the
  SCALUS URL -> application configuration. Run the `scalus-ui` executable to open
  it (there is no `scalus` sub-command for the UI).
* Build & Packaging: Cross-platform build and packaging scripts.

## Working with the SCALUS CLI

### Requirements

* [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
* [Node.js 22.x](https://nodejs.org/) + npm — required to build the Angular UI
  (`scalus-ui`). A CLI-only build (`dotnet build src/Cli/Scalus.Cli.csproj`) does
  not need it, but building the full solution or the UI project does.

### Build

* Using Visual Studio

    Open the scalus.sln in Visual Studio and build.

* Using dotnet CLI

    Make sure you have nuget.org configured as a package source:

    ```
    dotnet nuget list source
    ```

    If not, add it:
    
    ```
    dotnet nuget add source -n Nuget.Org https://api.nuget.org/v3/index.json
    ```

    Use the dotnet CLI to build SCALUS:

    ```
    dotnet build
    ```

### Packaging

Cross-platform build and packaging is done with plain scripts (no Cake). Each
entry point tests, publishes the NativeAOT `scalus` launcher plus the Photino
`scalus-ui` GUI, and produces an installer for the target runtime:

* macOS / Linux:

    ```
    ./build.sh --runtime osx-x64      # -> .pkg + .tar.gz
    ./build.sh --runtime linux-x64    # -> .deb + .rpm + .tar.gz
    ```

    On Linux the `.tar.gz` is always produced; the `.deb`/`.rpm` are built only
    when [`fpm`](https://fpm.readthedocs.io/) is on `PATH` (otherwise skipped with
    a warning).

* Windows (requires the `wix` dotnet tool + `WixToolset.UI.wixext`):

    ```
    ./build.ps1 -Runtime win-x64      # -> .msi
    ```

The underlying steps can also be run directly: `scripts/publish.{sh,ps1}` to
publish the payload, then the per-OS packager under `scripts/{Osx,Linux,Win}`.

### Configuration

SCALUS stores its working configuration and preferences in a per-user `SCALUS.json` file in the
user's profile folder (the exact location is OS-specific). Use the `scalus info` command to see the
full paths to the configuration and log files.

Logging is on by default at the `Debug` level and logs are written to a per-user, writable log
directory (so logging works even when SCALUS is installed under a read-only location such as
Program Files). The log level and console output are controlled from the **Settings** area of the
configuration UI, which persists them in the `settings` block of `SCALUS.json`:

```
{
  "settings": {
    "logLevel": "Debug",
    "console": false
  }
}
```

During development you can optionally drop an `appsettings.json` to override these
(`Logging:MinLevel`, `Logging:Console`, `Logging:FileName`, `Configuration:FileName`).
On Windows and Linux it is read from next to the binary; on macOS it is read from
the per-user application-support directory. This file is a development-only
convenience and is not shipped with released builds.

### Usage

SCALUS command-line usage:

```
Description:
  Session Client Application Launch Uri System (SCALUS)

Usage:
  scalus [command] [options]

Options:
  -?, -h, --help  Show help and usage information
  --version       Show version information

Commands:
  info        Show information about the current scalus configuration
  launch      Launch an app configured for the specified URL
  register    Register SCALUS to handle URLs
  unregister  Unregister SCALUS for URL handling
  verify      Run a syntax check on a scalus configuration file
```

The configuration GUI is a separate binary (`scalus-ui`), not a `scalus`
sub-command.

## Uninstalling and removing configuration

SCALUS stores its settings in a per-user `SCALUS.json` (run `scalus info` for the
exact path). Uninstalling the application leaves that configuration in place by
default so a future install keeps your settings. To remove it as well:

- **Interactive Windows uninstall** — the uninstaller shows a Yes/No dialog asking
  whether to delete your configuration data. Choose *No* (the default) to keep it.
- **Silent Windows uninstall** — pass `DELETECONFIG=1` to also purge the current
  user's configuration, e.g. `msiexec /x {ProductCode} /qn DELETECONFIG=1`. Without
  it, a silent uninstall never deletes configuration.
- **Any platform, from the CLI** — `scalus unregister --delete-config` removes the
  current user's configuration data, and `scalus unregister --prompt-delete-config`
  asks first (a dialog on Windows, a `[y/N]` terminal prompt elsewhere). On
  macOS/Linux, where the package managers cannot prompt during removal, use these
  flags to clean up per-user data.

## Working with the SCALUS GUI

See the [UI developer guide](../ui/README.md).
