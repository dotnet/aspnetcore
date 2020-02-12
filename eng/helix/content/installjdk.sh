#!/usr/bin/env bash

# Cause the script to fail if any subcommand fails
set -e

pushd .

if [ "$JAVA_HOME" != "" ]; then
    echo "JAVA_HOME is set"
    exit
fi

java_version=$1
arch=$2
osname=`uname -s`
if [ "$osname" = "Darwin" ]; then
   echo "macOS not supported, relying on the machine providing java itself"
   exit 1
else
   platformarch="linux-$arch"
fi
echo "PlatformArch: $platformarch"
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
output_dir="$DIR/java"
url="https://netcorenativeassets.blob.core.windows.net/resource-packages/external/linux/java/jdk-${java_version}_${platformarch}_bin.tar.gz"
echo "Downloading from: $url"
tmp="$(mktemp -d -t install-jdk.XXXXXX)"

cleanup() {
    exitcode=$?
    if [ $exitcode -ne 0 ]; then
      echo "Failed to install java with exit code: $exitcode"
    fi
    rm -rf "$tmp"
    exit $exitcode
}

trap "cleanup" EXIT
cd "$tmp"
curl -Lsfo $(basename $url) "$url"
echo "Installing java from $(basename $url) $url"
mkdir $output_dir
echo "Unpacking to $output_dir"
tar --strip-components 1 -xzf "jdk-${java_version}_${platformarch}_bin.tar.gz" --no-same-owner --directory "$output_dir"

popd