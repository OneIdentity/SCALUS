TODO Write something


Troubleshooting: 
* if you see the following error when running scalus as a non-root user:
 "Failed to create CoreCLR, HRESULT: 0x80004005"

  set the following environment variable: 
  export COMPlus_EnableDiagnostics=0
