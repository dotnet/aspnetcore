#!/usr/bin/env bash

if [ -z $1 ]; then
    echo "Deleting $1/toolassets"
    rm -rf $1/toolassets
fi

mkdir -p $1/toolassets
echo "Copying ./../src/Microsoft.Extensions.SecretManager.Tools/toolassets/*.targets"
cp ../../src/Microsoft.Extensions.SecretManager.Tools/toolassets/*.targets $1/toolassets

exit 0