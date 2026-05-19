#!/usr/bin/env bash

set -euo pipefail

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

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
done < $DIR/../../global.json > global.json.swap
mv global.json.swap $DIR/../../global.json
