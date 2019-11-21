#!/usr/bin/env bash

# Cause the script to fail if any subcommand fails
set -e

appRuntimePath=$1
output_dir=$2

echo "Installing shared framework from $appRuntimePath"
cp $appRuntimePath sharedFx.zip
mkdir -p $output_dir
echo "Unpacking to $output_dir"
unzip sharedFx.zip -d $output_dir
