##########################################################################
# This is the Cake bootstrapper script for PowerShell.
# This file was downloaded from https://github.com/cake-build/resources
# Feel free to change this file to fit your needs.
##########################################################################

<#
.SYNOPSIS
This is a Powershell script to bootstrap a Cake build.

.DESCRIPTION
This Powershell script will download NuGet if missing, restore NuGet tools (including Cake)
and execute your Cake build script with the parameters you provide.

.PARAMETER Script
The build script to execute.
.PARAMETER Target
The build script target to run.
.PARAMETER Configuration
The build configuration to use.
.PARAMETER Verbosity
Specifies the amount of information to be displayed.
.PARAMETER ShowDescription
Shows description about tasks.
.PARAMETER DryRun
Performs a dry run.
.PARAMETER SkipToolPackageRestore
Skips restoring of packages.
.PARAMETER ScriptArgs
Remaining arguments are added here.

.LINK
https://cakebuild.net
#>



[CmdletBinding()]
Param(
    [string]$Script = "build.cake",
    [string]$Target,
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$Version,
    [ValidateSet("Quiet", "Minimal", "Normal", "Verbose", "Diagnostic")]
    [string]$Verbosity,
    [switch]$ShowDescription,
    [Alias("WhatIf", "Noop")]
    [switch]$DryRun,
    [switch]$SkipToolPackageRestore,
    [Parameter(Position=0,Mandatory=$false,ValueFromRemainingArguments=$true)]
    [string[]]$ScriptArgs,
    [switch]$Pre
)

# This is an automatic variable in PowerShell Core, but not in Windows PowerShell 5.x
if (-not (Test-Path variable:global:IsCoreCLR)) {
    $IsCoreCLR = $false
}

# Attempt to set highest encryption available for SecurityProtocol.
# PowerShell will not set this by default (until maybe .NET 4.6.x). This
# will typically produce a message for PowerShell v2 (just an info
# message though)
$previousSecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol
try {
  # Set TLS 1.2 (3072), then TLS 1.1 (768), then TLS 1.0 (192), finally SSL 3.0 (48)
  # Use integers because the enumeration values for TLS 1.2 and TLS 1.1 won't
  # exist in .NET 4.0, even though they are addressable if .NET 4.5+ is
  # installed (.NET 4.5 is an in-place upgrade).
  # PowerShell Core already has support for TLS 1.2 so we can skip this if running in that.
  if (-not $IsCoreCLR) {
      [System.Net.ServicePointManager]::SecurityProtocol = 3072 -bor 768 -bor 192 -bor 48
  }
}
catch {
  Write-Output 'Unable to set PowerShell to use TLS 1.2 and TLS 1.1 due to old .NET Framework installed. If you see underlying connection closed or trust errors, you may need to upgrade to .NET Framework 4.5+ and PowerShell v3'
}

function GetProxyEnabledWebClient
{
    $wc = New-Object System.Net.WebClient
    $proxy = [System.Net.WebRequest]::GetSystemWebProxy()
    $proxy.Credentials = [System.Net.CredentialCache]::DefaultCredentials
    $wc.Proxy = $proxy
    return $wc
}

Write-Host "Preparing to run build script..."

$TOOLS_DIR = Join-Path $PSScriptRoot "tools"

# Try find dotnet.exe nuget.exe in path if not exists
function Test-Command {
    Param(
        [Parameter(Mandatory=$true)]
        [string] $Command
    )
    Get-Command $Command -EA SilentlyContinue
}
if (-not (Test-Command dotnet)) {
    throw "This script requires dotnet.exe -- https://dotnet.microsoft.com/en-us/download"
}
if (-not (Test-Command nuget)) {
    throw "This script requires nuget.exe -- https://www.nuget.org/downloads"
}

# Save nuget.exe path to environment to be available to child processes
$ENV:NUGET_EXE = (Test-Command nuget | Select-Object -ExpandProperty Definition)

# Make sure tools folder exists
if ((Test-Path $PSScriptRoot) -and !(Test-Path $TOOLS_DIR)) {
    Write-Verbose -Message "Creating tools directory..."
    New-Item -Path $TOOLS_DIR -Type directory | Out-Null
}

Write-Host "Installing Cake.Tool..."
if (dotnet tool list --tool-path tools | Select-String cake.tools) {
    Invoke-Expression "dotnet tool install Cake.Tool --version 2.2.0 --tool-path $TOOLS_DIR"
}
else {
    Write-Host "Cake.Tool already installed"
}

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

# Set it back so that DevOps doesn't get messed up
[System.Net.ServicePointManager]::SecurityProtocol = $previousSecurityProtocol

$scpt="${PSScriptRoot}/${Script}"

# Build Cake arguments
$cakeArguments = @("$scpt");
if ($Target) { $cakeArguments += "--target=$Target" }
if ($Configuration) { $cakeArguments += "--configuration=$Configuration" }
if ($Version) { $cakeArguments += "--version=$Version" }
if ($Verbosity) { $cakeArguments += "--verbosity=$Verbosity" }
if ($ShowDescription) { $cakeArguments += "--showdescription" }
if ($DryRun) { $cakeArguments += "--dryrun" }
if ($Experimental) { $cakeArguments += "--experimental" }
if ($Pre) { $cakeArguments += "--pre=true" }
$cakeArguments += $ScriptArgs

# Start Cake
Write-Host "Running build script for SCALUS..."
Write-Host "cake $cakeArguments"
& (Join-Path $TOOLS_DIR dotnet-cake.exe) $cakeArguments
exit $LASTEXITCODE
