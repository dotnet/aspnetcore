#!/usr/bin/env bash

SOURCE="${BASH_SOURCE[0]}"
while [ -h "$SOURCE" ]; do # resolve $SOURCE until the file is no longer a symlink
  DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"
  SOURCE="$(readlink "$SOURCE")"
  [[ $SOURCE != /* ]] && SOURCE="$DIR/$SOURCE" # if $SOURCE was a relative symlink, we need to resolve it relative to the path where the symlink file was located
done
repoFolder="$( cd -P "$( dirname "$SOURCE" )" && pwd )"

buildFolder=.build
koreBuildFolder=$buildFolder/KoreBuild-dotnet

nugetPath=$buildFolder/nuget.exe

if test `uname` = Darwin; then
    cachedir=~/Library/Caches/KBuild
else
    if [ -z $XDG_DATA_HOME ]; then
        cachedir=$HOME/.local/share
    else
        cachedir=$XDG_DATA_HOME;
    fi
fi
mkdir -p $cachedir
nugetVersion=latest
cacheNuget=$cachedir/nuget.$nugetVersion.exe

nugetUrl=https://dist.nuget.org/win-x86-commandline/$nugetVersion/nuget.exe

if test ! -d $buildFolder; then
    mkdir $buildFolder
fi

if test ! -f $nugetPath; then
    if test ! -f $cacheNuget; then
        wget -O $cacheNuget $nugetUrl 2>/dev/null || curl -o $cacheNuget --location $nugetUrl /dev/null
    fi

    cp $cacheNuget $nugetPath
fi

if test ! -d $koreBuildFolder; then
    mono $nugetPath install KoreBuild-dotnet -ExcludeVersion -o $buildFolder -nocache -pre
fi

source $koreBuildFolder/build/KoreBuild.sh

