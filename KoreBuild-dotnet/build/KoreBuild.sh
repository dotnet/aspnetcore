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

if test ! -d $koreBuildFolder/build/xunit.runner.console; then
    mono $nugetPath install xunit.runner.console -ExcludeVersion -o $koreBuildFolder/build -nocache -pre
fi

# Need to set this variable because by default the install script
# requires sudo 
export DOTNET_INSTALL_DIR=~/.dotnet
export PATH=~/.dotnet/bin:$PATH
export DOTNET_HOME=DOTNET_INSTALL_DIR
export KOREBUILD_FOLDER=$koreBuildFolder
source $koreBuildFolder/build/dotnet-install.sh

# ==== Temporary ====		
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

makefilePath=makefile.shade
if test ! -f $makefilePath; then
    makefilePath=$koreBuildFolder/build/makefile.shade
fi
echo "Using makefile: ${makefile}"

# Probe for Mono Reference assemblies
if test -z "$DOTNET_REFERENCE_ASSEMBLIES_PATH"; then
    if test $(uname) == Darwin && test -d "/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/xbuild-frameworks"; then
        export DOTNET_REFERENCE_ASSEMBLIES_PATH="/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/xbuild-frameworks"
    elif test -d "/usr/local/lib/mono/xbuild-frameworks"; then
        export DOTNET_REFERENCE_ASSEMBLIES_PATH="/usr/local/lib/mono/xbuild-frameworks"
    elif test -d "/usr/lib/mono/xbuild-frameworks"; then
        export DOTNET_REFERENCE_ASSEMBLIES_PATH="/usr/lib/mono/xbuild-frameworks"
    fi
fi

echo "Using Reference Assemblies from: $DOTNET_REFERENCE_ASSEMBLIES_PATH"

mono $sakeFolder/tools/Sake.exe -I $koreBuildFolder/build -f $makefilePath "$@"

