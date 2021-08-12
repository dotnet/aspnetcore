#!/usr/bin/env bash

set -e

# GitHub doesn't pull in submodules by default when mounting
# the repo directory on the container so we do this post-create
git submodule update --init --recursive
# Install SDK and tool dependencies before container starts
# Also run the full restore on the repo so that go-to definition
# and other language features will be available in C# files
./restore.sh
# The container creation script is executed in a new Bash instance
# so we exit at the end to avoid the creation process lingering.
exit
