#!/bin/bash

## Parameters
Target=
Configuration="Release"
Version=
# "Quiet", "Minimal", "Normal", "Verbose", "Diagnostic"
Verbosity=
ShowDescription=false
Pre=false
ScriptArgs=

>&2 echo "Preparing to run build script..."
ScriptDir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
ToolsDir="$ScriptDir/tools"
Script="$ScriptDir/build.cake"

if [ -z "$(which dotnet)" ]; then
    >&2 echo "You must install dotnet to use this build script -- https://dotnet.microsoft.com/en-us/download"
    exit 1
fi
if [ -z "$(which nuget)" ]; then
    >&2 echo "You must install nuget to use this build script -- https://www.nuget.org/downloads"
    exit 1
fi

## Make sure tools folder exists
if [ ! -d "$ToolsDir" ]; then
    mkdir -p $ToolsDir
fi

>&2 echo "Installing Cake.Tool..."
if [ -z "$(dotnet tool list --tool-path $ToolsDir | grep cake.tool)" ]; then
    dotnet tool install Cake.Tool --version 2.2.0 --tool-path "$ToolsDir"
else
    >&2 echo "Cake.Tool already installed"
fi
if [ $? -ne 0 ]; then
    exit $?
fi

## Build Cake arguments
CakeArguments=$Script
if [ ! -z "$Target" ]; then CakeArguments="$CakeArguments --target=$Target"; fi
if [ ! -z "$Configuration" ]; then CakeArguments="$CakeArguments --configuration=$Configuration"; fi
if [ ! -z "$Version" ]; then CakeArguments="$CakeArguments --version=$Version"; fi
if [ ! -z "$Verbosity" ]; then CakeArguments="$CakeArguments --verbosity=$Verbosity"; fi
if $ShowDescription; then CakeArguments="$CakeArguments --showdescription"; fi
if $Pre; then CakeArguments="$CakeArguments --pre=true"; fi
if [ ! -z "$ScriptArgs" ]; then CakeArguments="$CakeArguments $ScriptArgs"; fi

# Start Cake
>&2 echo "Running build script for SCALUS..."
>&2 echo "cake $CakeArguments"
CakeCommand="$ToolsDir/dotnet-cake $CakeArguments"
eval "$CakeCommand"