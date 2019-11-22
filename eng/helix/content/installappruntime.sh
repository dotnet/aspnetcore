#!/usr/bin/env bash

# Cause the script to fail if any subcommand fails
set -e

appRuntimePath=$1
output_dir=$2
tmpDir=./tmpRuntime

echo "Installing shared framework from $appRuntimePath"
cp $appRuntimePath sharedFx.zip

mkdir -p $tmpDir
echo "Unpacking to $tmpDir"
unzip sharedFx.zip -d $tmpDir
mkdir -p $output_dir
echo "Copying to $output_dir"
cp $tmpDir/runtimes/win-x86/lib/netcoreapp5.0/* $output_dir
cp $tmpDir/runtimes/win-x86/native/* $output_dir
ls -la $output_dir
