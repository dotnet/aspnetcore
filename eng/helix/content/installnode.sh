#!/usr/bin/env bash

node_version=$1
platformarch=$2
output_dir=$HELIX_CORRELATION_PAYLOAD/node
url="http://nodejs.org/dist/v$node_version/node-v$node_version-$platformarch.tar.gz"
tmp="$(mktemp -d -t install-node.XXXXXX)"
trap "rm -rf $tmp" EXIT
cd "$tmp"
curl -Lsfo $(basename $url) "$url"
echo "Installing node from $(basename $url) $url"
mkdir $output_dir
echo "Unpacking to $output_dir"
tar --strip-components 1 -xzf "node-v$node_version-$platformarch.tar.gz" --no-same-owner --directory "$output_dir"

export PATH="$PATH:$output_dir"
