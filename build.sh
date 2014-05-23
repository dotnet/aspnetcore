#!/bin/sh

if test `uname` = Darwin; then
    cachedir=~/Library/Caches/KBuild
else
    if x$XDG_DATA_HOME = x; then
    cachedir=$HOME/.local/share
    else
    cachedir=$XDG_DATA_HOME;
    fi
fi
mkdir -p $cachedir

url=https://www.nuget.org/nuget.exe

if test ! -f $cachedir/nuget.exe; then
    wget -o $cachedir/nuget.exe $url 2>/dev/null || curl -o $cachedir/nuget.exe --location $url /dev/null
fi

if test ! -e .nuget; then
    mkdir .nuget
    cp $cachedir/nuget.exe .nuget
fi

if test ! -d packages/KoreBuild; then
    mono .nuget/nuget.exe install KoreBuild -ExcludeVersion -o packages -nocache -pre
    mono .nuget/nuget.exe install Sake -version 0.2 -o packages -ExcludeVersion
fi

KRE_VERSION=$(mono .nuget/nuget.exe install KRE-mono45-x86 -pre -o ~/.kre/packages | head -1 | sed "s/.*KRE-mono45-x86 \([^']*\).*/\1/")
KRE_BIN=~/.kre/packages/KRE-mono45-x86.$KRE_VERSION/bin

chmod +x $KRE_BIN/k
chmod +x $KRE_BIN/klr
chmod +x $KRE_BIN/kpm
chmod +x $KRE_BIN/k-build

export PATH=$KRE_BIN:$PATH

mono packages/Sake/tools/Sake.exe -I packages/KoreBuild/build -f makefile.shade "$@"