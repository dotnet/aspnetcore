#!/usr/bin/env bash

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
    chmod +x $koreBuildFolder/build/KoreBuild.sh
fi

makeFile=makefile.shade
if [ ! -e $makeFile ]; then
    makeFile=$koreBuildFolder/build/makefile.shade
fi

./$koreBuildFolder/build/KoreBuild.sh -n $nugetPath -m $makeFile "$@"