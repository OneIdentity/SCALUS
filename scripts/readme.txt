Session Client Application Launch Uri System 

Version: SCALUSVERSION
------------------------------------------------------------------

What is SCALUS ? 

Scalus is a proxy/dispatcher for URI protocol handlers. 
A URL protocol is the part of the url before the ':', e.g. http or https. 
Specifically, SCALUS is intended to handle protocols like ssh or rdp but it can be used for any protocol used in a stardard URL. 
SCALUS lets you register URL protocols and allows you to configure other programs to launch to handle those URLS (e.g. Microsoft Remote Desktop or FreeRDP etc). 

The configuration of supported protocols and applications is stored in the file: scalus.json, but the configuration must be registered with the OS for the configuration to take effect. 

For more information about the contents of the scalus.json configuration file, run "scalus info -d"

The configuration file allows you to use tokens that pull their values from parts of the URL, e.g. %HOST%, %Port%. For a full list 
of available tokens, run "scalus info -t"

 - To view or edit the current configuration:
    - Start the scalusUI from the desktop link, or from the command line by running &"C:/program files/scalus/scalus.exe" ui 
    - From the commandline, view the registered protocols by running:
     	scalus info

 - To test the current scalus configuration, run:
        scalus launch -u <URL> [-p]

 - To syntax check the scalus configuration file, run:
        scalus verify

 - To register the current scalus.json configuration with the OS, run:
 	scalus register [-p protocol]
	
 - To deregister the current scalus.json configuration with the OS, run:
 	scalus unregister [-p protocol]
	
 - For help run:
       'scalus help'
       'scalus <option> --help'


Troubleshooting: 
- Linux:

   * If you see the following error when running scalus as a non-root user:
 	"Failed to create CoreCLR, HRESULT: 0x80004005"

     set the following environment variable: export COMPlus_EnableDiagnostics=0
