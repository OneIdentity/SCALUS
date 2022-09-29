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
.PARAMETER Edition
The build edition to use.
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
    [ValidateSet("community", "safeguard")]
    [string]$Edition = "community",
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
if (dotnet tool list --tool-path tools | Select-String cake.tool) {
    Write-Host "Cake.Tool already installed"
}
else {
    Invoke-Expression "dotnet tool install Cake.Tool --version 2.2.0 --tool-path $TOOLS_DIR"
}

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$scpt="${PSScriptRoot}/${Script}"

# Build Cake arguments
$cakeArguments = @("$scpt");
if ($Target) { $cakeArguments += "--target=$Target" }
if ($Configuration) { $cakeArguments += "--configuration=$Configuration" }
if ($Edition) { $cakeArguments += "--edition=$Edition" }
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
