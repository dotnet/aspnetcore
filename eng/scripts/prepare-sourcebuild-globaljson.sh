#!/usr/bin/env bash

set -euo pipefail

while IFS= read -r line
do
    # Remove dotnet/x64 runtimes
    if [[ "$line" == *"dotnet/x86"* ]]
    then
        while IFS= read -r removeLine
        do
            if [[ "$removeLine" == *"]"* ]]
            then
                break
            fi
        done
    else
        # Change dotnet/x64 to dotnet
        echo "${line/dotnet\/x64/dotnet}"
    fi
done < global.json > global.json.swap
mv global.json.swap global.json
