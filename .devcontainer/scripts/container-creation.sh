#!/usr/bin/env bash

set -e

# GitHub doesn't pull in submodules by default when mounting
# the repo directory on the container so we do this post-create
git submodule update --init --recursive
# Run restore to install the SDK before the app starts
./restore.sh