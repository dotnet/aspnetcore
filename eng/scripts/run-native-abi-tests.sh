#!/usr/bin/env bash

set -euo pipefail

if [[ $# -ne 1 ]]; then
    echo "Usage: $0 <publish-directory>" >&2
    exit 1
fi

scriptroot="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
reporoot="$(dirname "$(dirname "$scriptroot")")"
image="mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-18.04-net11.0-abi-test-amd64"

payload="$(cd "$1" && pwd)"
mkdir -p "$reporoot/artifacts/log/NativeAbi"

docker run --rm --network none \
    --env HOME=/tmp/native-abi-home \
    --mount "type=bind,src=$payload,dst=/payload,readonly" \
    --mount "type=bind,src=$scriptroot/test-native-tools.sh,dst=/test-native-tools.sh,readonly" \
    "$image" /bin/sh /test-native-tools.sh \
    2>&1 | tee "$reporoot/artifacts/log/NativeAbi/native-abi.log"
