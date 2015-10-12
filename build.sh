#!/usr/bin/env bash

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
cachePath=$cachedir/nuget.$nugetVersion.exe

url=https://dist.nuget.org/win-x86-commandline/$nugetVersion/nuget.exe

if test ! -f $cachePath; then
    wget -O $cachePath $url 2>/dev/null || curl -o $cachePath --location $url /dev/null
fi

if test ! -e .nuget; then
    mkdir .nuget
    cp $cachePath .nuget/nuget.exe
fi

if test ! -d packages/Sake; then
    mono .nuget/nuget.exe install Sake -ExcludeVersion -Source https://www.nuget.org/api/v2/ -Out packages
fi

mono packages/Sake/tools/Sake.exe -I build -f makefile.shade "$@"
