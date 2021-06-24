# SCALUS - Session Client Application Launch Uri System

This is a proxy/dispatcher for URI protocol handlers. A URL protocol is the part of the url before the : for example http or https are URL protocols.  Specifically, SCALUS is intended to handle protocols like ssh or rdp but it can be used for any protocol. SCALUS lets you register a URL protocol with the underlying operating system and allows you to configure other programs to launch to handle that URL. 
The configuration allows you to define variables that pull their values from parts of the URL and those can be passed on the command line or via a configuration file to tools like ssh, rdesktop, RdpClient, FreeRDP etc.

# Downloading SCALUS
Download the release version of scalus for your platform.

* Windows :  `scalus-setup-_version_-win-x64.msi` 
* Linux : `scalus-_version_-linux-x64.tar.gz`
* Mac OSX : `scalus-_version_-osx-x64.pkg`

# Installing SCALUS

* Windows     
     
    Run the msi installer, which installs to _C:/program files/scalus_ and creates a Scalus link from the start menu.    
    Run the ScalusUI to configure scalus.     
	    
* Linux    

    Run the following command to install to the selected installdir (this also creates a link to the scalus program from /usr/bin):    
    `sudo /bin/sh "-c mkdir -p _installdir_; tar xf _tarfile_ -C _installdir_; _installdir_/setup.sh"`    
     
    To start the UI, run  `/usr/bin/scalus ui`    
			  
* Mac OSX
	    
    Install the scalus.app application from the downloaded package file  e.g.     
    `installer -pkg scalus-_version__osx-x64.pkg -target /`    

    To start the UI, run `/Applications/scalus.app/Contents/MacOS/scalus ui`    

    The readme and examples can be found in _/Applications/scalus.app/Contents/Resources/examples_    
