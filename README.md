# SCALUS

SCALUS is an acronym for **Session Client Application Launch Uri System**. It was developed as a hackathon project at OneIdentity to aid launching remote sessions from OneIdentity's privileged account management software, [**Safeguard**](https://www.oneidentity.com/one-identity-safeguard). It is a general purpose tool that can be used for many different purposes and doesn't require **Safeguard** to use.

## What is it?

SCALUS is a dispatcher for URI protocol handlers. A protocol handler runs when the operating system attempts to launch a URI. The OS looks up an application that is registered to handle a particular URI protocol. For example a URL such as "https://" would be handled by the default browser. SCALUS can register with the OS (Windows, Mac, Linux) for any URI protocol and can be configured to launch any application. 

With SCALUS, applications that don't provide a native protocol handler can still be made to execute when the OS (or the browser) attempts to launch a URI. For example, you can configure SCALUS so that clicking a link like [ssh://user@some.host:2222]()  would launch `Putty.exe` even though `Putty.exe` doesn't register for **ssh://** URLs. SCALUS parses the URL and makes individual parts available so that `Putty.exe` can be launched with all the right command line options which in this case would be something like: `putty.exe -ssh -l user -P 2222 some.host`

The SCALUS configuration defines variables associated with parts of the URL and those variables can be passed on the command line or via a configuration file to tools like _ssh_, _rdesktop_, _RdpClient_, _FreeRDP_ etc.

## How does it work?

The SCALUS UI allows you to associate an application with a URI protocol. SCALUS registers itself as the URI protocol handler and then launches the configured application in response. SCALUS is a cross-platform .NET application that both handles URI dispatch and provides a local web application for configuration which keeps the user interface consistent across Winows, Linux and Mac.

Running SCALUS with no arguments launches the UI in the default browser. In this mode, SCALUS runs as a local webserver and exits when you close the browser tab. 

For common remote session launching scenarios, the SCALUS configuration is sufficient. The only reason to run the UI is to customize the configuration to run a preferred application. SCALUS is typically invoked behind-the-scenes by the OS rather than by users.

# Using SCALUS

## Download
SCALUS can be downloaded from the [releases area](https://github.com/OneIdentity/SCALUS/releases).

## Install
* Windows

    Run the msi installer, which installs to _C:/program files/scalus_ and creates a SCALUS link from the start menu.

    To start the UI, run `scalus.exe` from the start menu.

* Linux

    Run the following command to install to the selected installdir (this also creates a link to the SCALUS program from /usr/bin):
    `sudo /bin/sh -c "mkdir -p _installdir_; tar xf _tarfile_ -C _installdir_; _installdir_/setup.sh"`
     
    To start the UI, run  `/usr/bin/scalus`
 
* Mac OSX

    Install the scalus.app application from the downloaded package file  e.g.
    `installer -pkg scalus-_version__osx-x64.pkg -target /`

    To start the UI, run `/Applications/scalus.app`

    The readme and examples can be found in _/Applications/scalus.app/Contents/Resources/examples_

## Configure

To configure SCALUS run the application with no arguments. The configuration UI will launch in your browser. Check out the [SCALUS Wiki](https://github.com/OneIdentity/SCALUS/wiki) for more configuration details.

# Contributing to SCALUS

Is there something you would like to add to SCALUS? See the [developer guide](src/README.md). Is something broken or bothering you? [Log an issue](https://github.com/OneIdentity/SCALUS/issues).
