@echo off
setlocal enabledelayedexpansion

rem ---------------------------------------------------------------------
rem Code by Claude AI - Viktor Varga (One Identity)
rem ---------------------------------------------------------------------
rem sshSelector.bat
rem
rem A tiny SCALUS "pre-exec" launcher plugin. It is configured directly as
rem an ApplicationConfig's Exec (not as a PostProcessingExec), so SCALUS
rem simply runs this script instead of ssh.exe/winscp.exe directly. This
rem script prompts the user to pick between two target applications and
rem launches the chosen one with the correct arguments.
rem
rem Usage:
rem   Copy this script into SCALUS's app data folder:
rem     %AppData%\Local\SCALUS\sshSelector.bat
rem   (this is the same folder SCALUS itself refers to via the "%AppData%"
rem   token, e.g. scripts/examples, WinRdpTemplate.rdp, etc. live here too)
rem
rem   sshSelector.bat <CountdownSeconds> <User> <Host> <Port> <TargetUser> <TargetHost>
rem
rem Add the following Application entry to your Scalus configuration:
rem 
rem
rem   {
rem     "Protocols": [
rem       { "Protocol": "ssh", "AppId": "ssh-selector" }
rem     ],
rem     "Applications": [
rem       {
rem         "Id": "ssh-selector",
rem         "Name": "SSH or WinSCP (prompt)",
rem         "Description": "Prompts the user to choose between Windows OpenSSH and WinSCP",
rem         "Platforms": [ "Windows" ],
rem         "Protocol": "ssh",
rem         "Parser": { "ParserId": "ssh", "Options": [] },
rem         "Exec": "%AppData%\\sshSelector.bat",
rem         "Args": [ "10", "%User%", "%Host%", "%Port%", "%TargetUser%", "%TargetHost%" ]
rem       }
rem     ]
rem   }
rem
rem NOTE: the "%AppData%" used in "Exec" above is SCALUS's own token
rem (resolved by SCALUS itself, not by cmd.exe). It already expands to the
rem full folder path C:\Users\<user>\AppData\Local\SCALUS, so do NOT
rem append "\Local\SCALUS" after it - just "%AppData%\sshSelector.bat" is
rem the correct Exec value, matching where the file was copied above.
rem ---------------------------------------------------------------------
rem NOTE: We deliberately do NOT read %1/%2/%3 directly. cmd.exe's batch
rem parameter parser treats "=", "," and ";" as additional delimiters (on
rem top of spaces) when splitting the command line into %1, %2, %3, ...
rem If the User value contains "=" (e.g. Safeguard-style
rem "vaultaddress=10.10.35.140"), it gets silently split across an extra
rem parameter and everything after the "=" is lost, shifting Host into the
rem wrong slot. "FOR /F" only splits on space/tab by default, so we use it
rem on %* (the raw, unsplit argument string) to recover the 6 real values.
rem ---------------------------------------------------------------------

for /f "tokens=1,2,3,4,5,6" %%A in ("%*") do (
    set "COUNTDOWN=%%A"
    set "SSHUSER=%%B"
    set "SSHHOST=%%C"
    set "SSHPORT=%%D"
    set "TARGETUSER=%%E"
    set "TARGETHOST=%%F"
)

set "OPENSSH_EXE=C:\Windows\System32\OpenSSH\ssh.exe"
set "WINSCP_EXE=C:\Program Files (x86)\WinSCP\WinSCP.exe"

if "%COUNTDOWN%"=="" set "COUNTDOWN=10"

if not "%SSHUSER%"=="" goto :CheckHost
echo ERROR: User argument is required.
exit /b 1

:CheckHost
if not "%SSHHOST%"=="" goto :CheckPort
echo ERROR: Host argument is required.
exit /b 1

:CheckPort
if not "%SSHPORT%"=="" goto :CheckTargetUser
echo ERROR: Port argument is required.
exit /b 1

:CheckTargetUser
if not "%TARGETUSER%"=="" goto :CheckTargetHost
echo ERROR: TargetUser argument is required.
exit /b 1

:CheckTargetHost
if not "%TARGETHOST%"=="" goto :Prompt
echo ERROR: TargetHost argument is required.
exit /b 1

:Prompt
echo.
echo.
echo Select the application to connect to %SSHUSER%@%SSHHOST%:
echo.
echo   1. Windows OpenSSH  ^(default^)
echo   2. WinSCP
echo.

choice /c 12 /n /d 1 /t %COUNTDOWN% /m "Enter your choice (1 or 2), auto-selecting the default after %COUNTDOWN% seconds"
set "CHOICE=%errorlevel%"

if "%CHOICE%"=="2" goto :StartWinScp
goto :StartOpenSsh

:StartOpenSsh
if exist "%OPENSSH_EXE%" goto :RunOpenSsh
echo ERROR: Windows OpenSSH client not found at: %OPENSSH_EXE%
exit /b 1

:RunOpenSsh
echo Starting Windows OpenSSH...
start "%TARGETUSER%@%TARGETHOST%" "%OPENSSH_EXE%" -l "%SSHUSER%" -p "%SSHPORT%" "%SSHHOST%"
goto :Eof

:StartWinScp
if exist "%WINSCP_EXE%" goto :RunWinScp
echo ERROR: WinSCP not found at: %WINSCP_EXE%
exit /b 1

:RunWinScp
echo Starting WinSCP...
start "" "%WINSCP_EXE%" "scp://%SSHUSER%@%SSHHOST%:%SSHPORT%" /sessionname="%TARGETUSER%@%TARGETHOST%"
goto :Eof

:Eof
endlocal
exit /b 0
