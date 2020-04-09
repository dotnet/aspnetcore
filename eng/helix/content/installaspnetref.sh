#!/usr/bin/env bash

# Cause the script to fail if any subcommand fails
set -e

refPath=$1
output_dir=$2
tmpDir=./tmpRuntime

echo "Installing ref package from $refPath"
cp $refPath sharedFx.zip

mkdir -p $output_dir
echo "Unzip to $output_dir"
unzip sharedFx.zip -d $output_dir
