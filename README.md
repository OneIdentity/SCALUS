# SCALUS

SCALUS is an acronym for **Session Client Application Launch Uri System**. It was developed as a hackathon project at One Identity to aid launching remote sessions from One Identity's privileged account management software, [**Safeguard**](https://www.oneidentity.com/one-identity-safeguard). It is a general purpose tool that can be used for many different purposes and doesn't require **Safeguard** to use.

> **SCALUS 2.0 is a major release** with a reworked technology stack and a new
> native desktop configuration app (the old local-web-server/browser UI is gone).
> Upgrading from 1.x? Read **[UPGRADING.md](UPGRADING.md)** first.

## What is it?

SCALUS is a dispatcher for URI protocol handlers. A protocol handler runs when the operating system attempts to launch a URI. The OS looks up an application that is registered to handle a particular URI protocol. For example a URL such as "https://" would be handled by the default browser. SCALUS can register with the OS (Windows, Mac, Linux) for any URI protocol and can be configured to launch any application.

With SCALUS, applications that don't provide a native protocol handler can still be made to execute when the OS (or the browser) attempts to launch a URI. For example, you can configure SCALUS so that clicking a link like `ssh://user@some.host:2222` would launch `Putty.exe` even though `Putty.exe` doesn't register for **ssh://** URLs. SCALUS parses the URL and makes individual parts available so that `Putty.exe` can be launched with all the right command line options which in this case would be something like: `putty.exe -ssh -l user -P 2222 some.host`

The SCALUS configuration defines variables associated with parts of the URL and those variables can be passed on the command line or via a configuration file to tools like _ssh_, _rdesktop_, _RdpClient_, _FreeRDP_ etc.

## How does it work?

SCALUS ships as two cooperating binaries that share one core library:

- **`scalus`** — a small, self-contained native launcher (and CLI). The OS invokes
  it on the hot path every time a registered URI is clicked; it parses the URL,
  resolves the configured application and template, and starts the session client.
  It also exposes the `register` / `unregister` / `info` / `verify` commands.
- **`scalus-ui`** — a native desktop application for editing the configuration
  (which application handles which protocol, with which template). It is an
  Angular front end hosted in a [Photino](https://www.tryphotino.io/) window — a
  real cross-platform desktop window, **not** a browser or a local web server.

You only need the UI to customize which application a protocol launches. For
common remote-session scenarios the shipped defaults are sufficient, and SCALUS
runs behind the scenes — invoked by the OS, not by users.

SCALUS is a cross-platform .NET 10 application and runs on **Windows, macOS, and
Linux**.

# Using SCALUS

## Download

SCALUS can be downloaded from the [releases area](https://github.com/OneIdentity/SCALUS/releases).
Each release provides installers per OS and architecture (x64 and arm64).

## Install

* **Windows** — run the MSI installer (`scalus-setup-<version>-win-x64.msi`). It
  installs to `%ProgramFiles%\SCALUS` and adds a SCALUS shortcut to the Start
  menu. Launch **SCALUS** from the Start menu to open the configuration app.

  Upgrading from a 1.x install is automatic — the 2.0 MSI removes the old version
  and installs the new one in place.

* **macOS** — install from the `.pkg` (`scalus-<version>-osx-x64.pkg` or the
  `osx-arm64` build):

    ```
    installer -pkg scalus-<version>-osx-x64.pkg -target CurrentUserHomeDirectory
    ```

  Then launch **SCALUS** (the installed `scalus.app` bundle, which runs the
  `scalus-ui` executable) from Launchpad, or `open ~/Applications/scalus.app`.
  A portable `.tar.gz` is also provided.

* **Linux** — install the native package for your distro, or use the portable
  tarball:

    ```
    sudo dpkg -i scalus_<version>-1_amd64.deb      # Debian / Ubuntu
    sudo rpm -i  scalus-<version>-1.x86_64.rpm     # RHEL / Fedora / SUSE
    ```

  The packages install to `/opt/scalus` and symlink `scalus` and `scalus-ui`
  into `/usr/bin`, plus an application-menu launcher. For the tarball, extract it
  and run the bundled `setup.sh`. (Photino needs GTK/WebKitGTK, which the native
  packages declare as dependencies.)

## Configure

Launch the SCALUS desktop app (**`scalus-ui`**, or the Start-menu / Launchpad /
app-menu entry). From there you can:

- associate an application with a URI protocol and edit its launch template,
- **register** / **unregister** SCALUS as the OS handler for a protocol,
- import and export configuration (or share a single application definition), and
- review recent launches and diagnostics.

Configuration is stored in a per-user `SCALUS.json`. Run `scalus info` to print
the exact configuration and log file paths on your machine. See the
[SCALUS Wiki](https://github.com/OneIdentity/SCALUS/wiki) for configuration
details.

# Contributing to SCALUS

Is there something you would like to add to SCALUS? See the
[developer guide](src/README.md) for build, run, and packaging instructions, and
[AGENTS.md](AGENTS.md) for a map of the repository. Notable changes are recorded
in the [changelog](CHANGELOG.md). Is something broken or bothering you?
[Log an issue](https://github.com/OneIdentity/SCALUS/issues).

