#!/usr/bin/env bash

# Cause the script to fail if any subcommand fails
set -e

appRuntimePath=$1
output_dir=$2
framework=$3
rid=$4
tmpDir=./tmpRuntime

echo "Installing shared framework from $appRuntimePath"
cp $appRuntimePath sharedFx.zip

mkdir -p $tmpDir
unzip sharedFx.zip -d $tmpDir
mkdir -p $output_dir
echo "Copying to $output_dir"
cp $tmpDir/runtimes/$rid/lib/$framework/* $output_dir
cp $tmpDir/runtimes/$rid/native/* $output_dir
