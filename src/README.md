# SCALUS Developer Guide

SCALUS consists of 3 major components:

* The CLI: Dispatches URL's to applications. Hosts the GUI.
* The GUI: Modifies the SCALUS URL -> application configuration.
* Build & Packaging: Cross platform build and packaging.

## Working with the SCALUS CLI

### Requirements

* [.NET 6 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)

### Build

Use the dotnet CLI to build SCALUS:

dotnet build

The following build parameters can be set to customize the build output:

* /p:Edition=&lt;community | safeguard&gt;

    Determines which edition to build. The Safeguard edition is supported by OneIdentity. Only binaries built and signed by OneIdentity are supported.

* /p:NativeWindowing=&lt;true | false&gt;

    Determines whether or not to use native Windowing features. This is currently only supported for Windows platforms. With NativeWindowing enabled SCALUS shows a splash screen on startup and does not display a console Window when invoked to handle a URL.

### Configuration

`appsettings.json` can be used to customize the runtime SCALUS behavior. Set MinLevel to `Debug` to get more detailed log file output. Set `Console` to true to log to the console window. Configuration files are stored in the user's profile folder as determined by the OS. Use the `scalus info` command to see full paths to log and configuration files.

```
{
  "Logging": {
    "FileName": "scalus.log",
    "MinLevel": "Information",
    "Console": true
  },
  "Configuration": {
    "FileName":  "scalus.json"
  }
}
```

### Usage

SCALUS command-line usage:

```
Session Client Application Launch Uri System (SCALUS)
Copyright (c) 2022 One Identity LLC

  info          Show information about the current SCALUS configuration
  launch        Launch an app configured for the specified URL
  register      Register SCALUS to handle URLs
  ui            (Default Verb) Run the configuration UI
  unregister    Unregister SCALUS for URL handling
  verify        Run a syntax check on a SCALUS configuration file
  help          Display more information on a specific command.
  version       Display version information.
```

## Working with the SCALUS GUI

See the [UI developer guide](Ui/README.md).