#!/usr/bin/env bash

[ -z "$KOREBUILD_DOTNET_CHANNEL" ] && KOREBUILD_DOTNET_CHANNEL=beta
[ -z "$KOREBUILD_DOTNET_VERSION" ] && KOREBUILD_DOTNET_VERSION=1.0.0.001496

targets=""
filename=$0
while [[ $# > 0 ]]; do
    case $1 in
        -m)
            shift
            makeFilePath=$1
            ;;
        -n)
            shift
            nugetPath=$1
            ;;
        *)
            targets+=" $1"
            ;;
    esac
    shift
done
if [ ! -e "$nugetPath" ] || [ ! -e "$makeFilePath" ]; then
    printf "Usage: $filename -m [makefile] -n [nuget] [ [targets] ]\n\n"
    echo "       -m [makefile]  The makefile.shade to execute"
    echo "       -n [nuget]     nuget.exe"
    echo "       [targets]      A space separated list of targets to run"
    exit 1
fi

# ==== find directory containing this file =====
SOURCE="${BASH_SOURCE[0]}"
while [ -h "$SOURCE" ]; do # resolve $SOURCE until the file is no longer a symlink
  DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"
  SOURCE="$(readlink "$SOURCE")"
  [[ $SOURCE != /* ]] && SOURCE="$DIR/$SOURCE" # if $SOURCE was a relative symlink, we need to resolve it relative to the path where the symlink file was located
done
thisDir="$( cd -P "$( dirname "$SOURCE" )" && pwd )"
# ===================


sakeFolder=$thisDir/Sake
if [ ! -d $sakeFolder ]; then
    mono $nugetPath install Sake -ExcludeVersion -o $thisDir -nocache
fi
if [ ! -d $thisDir/xunit.runner.console ]; then
    mono $nugetPath install xunit.runner.console -ExcludeVersion -o $thisDir -nocache
fi
if [ ! -d $thisDir/xunit.core ]; then
    mono $nugetPath install xunit.core -ExcludeVersion -o $thisDir -nocache
fi

if [ ! -z "$KOREBUILD_SKIP_RUNTIME_INSTALL" ]; then
    echo "Skipping runtime installation because KOREBUILD_SKIP_RUNTIME_INSTALL is set"
else
    # Need to set this variable because by default the install script
    # requires sudo
    export DOTNET_INSTALL_DIR=~/.dotnet
    export PATH=$DOTNET_INSTALL_DIR/bin:$PATH
    export KOREBUILD_FOLDER="$(dirname $thisDir)"
    chmod +x $thisDir/dotnet-install.sh
    $thisDir/dotnet-install.sh --channel $KOREBUILD_DOTNET_CHANNEL --version $KOREBUILD_DOTNET_VERSION
    # ==== Temporary ====
    if ! type dnvm > /dev/null 2>&1; then
        source $thisDir/dnvm.sh
    fi
        if ! type dnx > /dev/null 2>&1 || [ -z "$SKIP_DNX_INSTALL" ]; then
            dnvm install latest -runtime coreclr -alias default
            dnvm install default -runtime mono -alias default
        else
        dnvm use default -runtime mono
    fi
    # ============
fi

# Probe for Mono Reference assemblies
if [ -z "$DOTNET_REFERENCE_ASSEMBLIES_PATH" ]; then
    if [ $(uname) == Darwin ] && [ -d "/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/xbuild-frameworks" ]; then
        export DOTNET_REFERENCE_ASSEMBLIES_PATH="/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/xbuild-frameworks"
    elif [ -d "/usr/local/lib/mono/xbuild-frameworks" ]; then
        export DOTNET_REFERENCE_ASSEMBLIES_PATH="/usr/local/lib/mono/xbuild-frameworks"
    elif [ -d "/usr/lib/mono/xbuild-frameworks" ]; then
        export DOTNET_REFERENCE_ASSEMBLIES_PATH="/usr/lib/mono/xbuild-frameworks"
    fi
fi
echo "Using Reference Assemblies from: $DOTNET_REFERENCE_ASSEMBLIES_PATH"
mono $sakeFolder/tools/Sake.exe -I $thisDir -f $makeFilePath $targets
