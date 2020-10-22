# sulu - Session URL Launcher Utility. 

This is a proxy/dispatcher for URL protocol handlers. A URL protocol is the part of the url before the : for example http or https are URL protocols.  Specifically, sulu is intended to handle protocols like ssh or rdp but it can be used for any protocol. sulu lets you register URL protocols and allows you to configure other programs to launch to handle those URLS. The configuration allows you to define variables that pull their values from parts of the URL and those can be passed on the command line or via config file to tools like ssh, rdesktop, RdpClient, FreeRDP etc.

# Using sulu
First, register sulu to handle the URL protocols you are interested in (you should be administrator for this part):

```
sulu.exe register -p rdp ssh
```

Next, configure sulu to launch a remote session app for the protocol:

```
sulu.exe ui
```

This will launch the configuration UI in your browser, but the sulu.json configuration file contains all the details.

That's it. Now when something (like your browser) launches a URL with one of the registered protocols it will automatically trigger the configured application. You can test it this way:

```
explorer.exe rdp://123.45.6.7/test/url
```

Uninstall with:
```
sulu.exe unregister -p rdp ssh
```


# Configure Firefox to allow SSH protocol handlers:

Navigate to about:config

Paste these values, click + to add them as BOOL and make sure the value is true:
```
network.protocol-handler.external.ssh
network.protocol-handler.expose.ssh
```
Paste these values, click + to add them as BOOL and make sure the value is false:
```
network.protocol-handler.warn-external.ssh
```

# Manual registration on Ubuntu

Run these commands from bash prompt:
```
cat << EOF > ~/.local/share/applications/sulu.desktop
[Desktop Entry]
Name=Sulu
Comment=Session URL Launcher Utility
Exec=/usr/bin/dotnet /<path to sulu>/Sulu.dll launch -u %u
Terminal=false
Type=Application
MimeType=x-scheme-handler/ssh;x-scheme-handler/rdp;x-scheme-handler/telnet
EOF

xdg-mime default sulu.desktop x-scheme-handler/rdp
xdg-mime default sulu.desktop x-scheme-handler/ssh
xdg-mime default sulu.desktop x-scheme-handler/telnet
```
