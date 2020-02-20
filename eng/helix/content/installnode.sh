#!/usr/bin/env bash

# Cause the script to fail if any subcommand fails
set -e

if type -P "node" &>/dev/null; then
    echo "node is in \$PATH"
    exit
fi

node_version=$1
arch=$2
osname=`uname -s`
if [ "$osname" = "Darwin" ]; then
   platformarch="darwin-$arch"
else
   platformarch="linux-$arch"
fi
echo "PlatformArch: $platformarch"
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
output_dir="$DIR/node"
url="http://nodejs.org/dist/v$node_version/node-v$node_version-$platformarch.tar.gz"
echo "Downloading from: $url"
tmp="$(mktemp -d -t install-node.XXXXXX)"

cleanup() {
    exitcode=$?
    if [ $exitcode -ne 0 ]; then
      echo "Failed to install node with exit code: $exitcode"
    fi
    rm -rf "$tmp"
    exit $exitcode
}

trap "cleanup" EXIT
cd "$tmp"
curl -Lsfo $(basename $url) "$url"
echo "Installing node from $(basename $url) $url"
mkdir $output_dir
echo "Unpacking to $output_dir"
tar --strip-components 1 -xzf "node-v$node_version-$platformarch.tar.gz" --no-same-owner --directory "$output_dir"
