#!/usr/bin/env bash

if [ -z $1 ]; then
    echo "Deleting $1/TestProjects"
    rm -rf $1/TestProjects

    echo "Deleting $1/toolassets"
    rm -rf $1/toolassets
fi

mkdir -p $1/toolassets
cp ../../src/Microsoft.DotNet.Watcher.Tools/toolassets/*.targets $1/toolassets

exit 0