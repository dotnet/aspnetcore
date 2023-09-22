#!/usr/bin/env bash

set -eo pipefail

export DOTNET_CLI_TELEMETRY_OPTOUT="true"

configuration=Release
#arch=x64,arm,arm64

cat pack-list.txt | while read line || [[ -n $line ]];
do
    echo $line
    fullPath=$(pwd)/$(find . -iname "$line")
    ./dockerbuild.sh bionic --pack --configuration $configuration --projects $fullPath

    # temporary quick exit while testing.
    exit 0
done
