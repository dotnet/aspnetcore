#!/usr/bin/env bash

# Cause the script to fail if any subcommand fails
set -e

appRuntimePath=$1
output_dir=$2

echo "Installing shared framework from $appRuntimePath"
mkdir $output_dir
echo "Unpacking to $output_dir"
tar --strip-components 1 -xzf "$appRuntimePath" --no-same-owner --directory "$output_dir"
