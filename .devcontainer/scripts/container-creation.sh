#!/usr/bin/env bash

set -e

# GitHub doesn't pull in submodules by default when mounting
# the repo directory on the container so we do this post-create
git submodule update --init --recursive

# Install SDK and tool dependencies before container starts
# Also run the full restore on the repo so that go-to definition
# and other language features will be available in C# files
./restore.sh

# Add .NET Dev Certs to environment to facilitate debugging.
# Do **NOT** do this in a public base image as all images inheriting
# from the base image would inherit these dev certs as well. 
dotnet dev-certs https

# The container creation script is executed in a new Bash instance
# so we exit at the end to avoid the creation process lingering.
exit
