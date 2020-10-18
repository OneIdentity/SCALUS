# sulu - Session URL Launcher Utility. 

This is a proxy/dispatcher for URL protocol handlers. Specifically, sulu is intended to handle protocols like ssh:// or rdp:// but it can be used for any protocol. You might follow a URL like [rdp://my-remote-host.example.com](rdp://my-remote-host.example.com) but nothing has been configured to handle that URL protocol. (The URL protocol is the rdp: part.)  sulu lets you register URL protocols and allows you to configure other programs to launch to handle those URLS.  The configuration allows you to define  variables that pull their values from parts of the URL and those can be passed on the command line or via config files to tools like ssh, rdesktop, RdpClient, FreeRDP etc.

# How it works
First, register sulu to handle the URL protocols you are interested in (you should be administrator for this part):

```
sulu.exe register -p rdp,ssh
```

Next, configure sulu to launch a remote session app with the appropriate command line:

```
sulu.exe ui
```

This will launch the configuration UI in your browser, but the sulu.json configuration file contains all the details.

That's it. Now when something (like your browser) launches any of the registered URL protocols it will automatically trigger the configured application. You can test it this way:

```
explorer.exe rdp://123.45.6.7/test/url
```

Uninstall with:
```
sulu.exe unregister -p rdp,ssh
```
