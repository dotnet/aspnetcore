#!/usr/bin/env bash

set -euo pipefail

if [[ $# -lt 1 || $# -gt 2 ]]; then
    echo "Usage: $0 <package-directory> [abi-image]" >&2
    exit 1
fi

scriptroot="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
reporoot="$(dirname "$(dirname "$scriptroot")")"
image="${2:-mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-18.04-net11.0-abi-test-amd64}"

shopt -s nullglob
packages=()
for package in "$1"/aspnetcoretools.linux-x64.*.nupkg; do
    if [[ "$package" != *.symbols.nupkg ]]; then
        packages+=("$package")
    fi
done

if [[ ${#packages[@]} -ne 1 ]]; then
    echo "Expected one aspnetcoretools.linux-x64 package in $1; found ${#packages[@]}." >&2
    exit 1
fi

mkdir -p "$reporoot/artifacts/tmp" "$reporoot/artifacts/log/NativeAbi"
payloadroot="$(mktemp -d "$reporoot/artifacts/tmp/native-abi.XXXXXX")"
trap 'rm -rf "$payloadroot"' EXIT

unzip -q "${packages[0]}" 'tools/any/linux-x64/*' -x '*.dbg' '*.pdb' -d "$payloadroot"
payload="$payloadroot/tools/any/linux-x64"
chmod +x "$payload/dotnet-dev-certs" "$payload/dotnet-user-jwts" "$payload/dotnet-user-secrets"

docker run --rm --network none \
    --env HOME=/tmp/native-abi-home \
    --mount "type=bind,src=$payload,dst=/payload,readonly" \
    --mount "type=bind,src=$scriptroot/test-native-tools.sh,dst=/test-native-tools.sh,readonly" \
    "$image" /bin/sh /test-native-tools.sh \
    2>&1 | tee "$reporoot/artifacts/log/NativeAbi/native-abi.log"
