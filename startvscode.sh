#!/usr/bin/env bash

# This command launches a Visual Studio code with environment variables required to use a local version of the .NET Core SDK.

# This tells .NET Core to use the same dotnet.exe that build scripts use
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
export DOTNET_ROOT="$DIR/.dotnet"

# This tells .NET Core not to go looking for .NET Core in other places
export DOTNET_MULTILEVEL_LOOKUP=0

# Put our local dotnet on PATH first so Visual Studio knows which one to use
export PATH="$DOTNET_ROOT:$PATH"

# Sets TFW for Visual Studio Code usage
export TARGET=net9.0

if [ ! -f "$DOTNET_ROOT/dotnet" ]; then
    echo ".NET Core has not yet been installed. Run `./restore.sh` to install tools."
    exit 1
fi

if [[ $1 == "" ]]; then
  code .
else
  code $1
fi

exit 1
