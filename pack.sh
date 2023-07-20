#!/usr/bin/env bash

set -eo pipefail

configuration=Release
#arch=x64,arm,arm64

cat pack-list.txt | while read line || [[ -n $line ]];
do
    echo $line
    fullPath=$(pwd)/$(find . -iname "$line")
    ./eng/build.sh --pack --configuration $configuration --projects $fullPath
done
