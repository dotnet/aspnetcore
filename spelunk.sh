#!/usr/bin/env bash
echo "LD_LIBRARY_PATH=$LD_LIBRARY_PATH"

dpkg-query --show "*ssl*"

for pkg in `dpkg-query --showformat="\\${binary:Package}\n" --show "*ssl*"`; do
    echo "*** $pkg ***"
    dpkg-query --listfiles $pkg
done