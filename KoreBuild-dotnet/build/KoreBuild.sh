#!/usr/bin/env bash

if [ -z "$koreBuildFolder" ]; then
    echo "koreBuildFolder is not set."
    exit 1
fi

if [ -z "$nugetPath" ]; then
    echo "nugetPath is not set."
    exit 1
fi

sakeFolder=$koreBuildFolder/build/Sake

if test ! -d $sakeFolder; then
    mono $nugetPath install Sake -ExcludeVersion -o $koreBuildFolder/build -nocache -pre
fi

# Need to set this variable because by default the install script
# requires sudo 
export DOTNET_INSTALL_DIR=~/.dotnet
export PATH=$DOTNET_INSTALL_DIR/bin:$PATH
export DOTNET_HOME=DOTNET_INSTALL_DIR
source $koreBuildFolder/build/dotnet-install.sh

makefilePath=makefile.shade
if test ! -f $makefilePath; then
    makefilePath=$koreBuildFolder/build/makefile.shade
fi

# ==== Temporary because we need 'dnu packages add' ====
if ! type dnvm > /dev/null 2>&1; then
    source $koreBuildFolder/build/dnvm.sh
fi
if ! type dnx > /dev/null 2>&1 || [ -z "$SKIP_DNX_INSTALL" ]; then
    dnvm install latest -runtime coreclr -alias default
    dnvm install default -runtime mono -alias default
else
    dnvm use default -runtime mono
fi
# ============

mono $koreBuildFolder/build/Sake/tools/Sake.exe -I $koreBuildFolder/build -f $makefilePath "$@"