#!/usr/bin/env bash

# Cause the script to fail if any subcommand fails
set -e

if type -P "node" &>/dev/null; then
    echo "node is in \$PATH"
    exit
fi

node_version=$1
osname=`uname -s`
echo $osname
if [ "$osname" = "Darwin" ]; then
   platformarch='darwin-x64'
else
   platformarch='linux-x64'
fi
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
output_dir="$DIR/node"
url="http://nodejs.org/dist/v$node_version/node-v$node_version-$platformarch.tar.gz"
tmp="$(mktemp -d -t install-node.XXXXXX)"
trap "rm -rf $tmp" EXIT
cd "$tmp"
curl -Lsfo $(basename $url) "$url"
echo "Installing node from $(basename $url) $url"
mkdir $output_dir
echo "Unpacking to $output_dir"
tar --strip-components 1 -xzf "node-v$node_version-$platformarch.tar.gz" --no-same-owner --directory "$output_dir"
