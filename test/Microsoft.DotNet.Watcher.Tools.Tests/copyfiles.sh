#!/usr/bin/env bash

if [ -z $1 ]; then
    echo "Deleting $1/tools"
    rm -rf $1/tools
fi

mkdir -p $1/tools
echo "Copying ./../src/Microsoft.DotNet.Watcher.Tools/tools/*.targets"
cp ../../src/Microsoft.DotNet.Watcher.Tools/tools/*.targets $1/tools

exit 0