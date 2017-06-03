#!/usr/bin/env bash

if [[ $# < 2 ]]; then
    echo "Usage: [src] [dest]"
    exit 1
fi

function realpath() {
    [[ $1 = /* ]] && echo "$1" || echo "$PWD/${1#./}"
}

src="$(realpath $1)"
dest="$(realpath $2)"

echo "Converting:"
echo "  -  $src"
echo "  => $dest"

tmp="$(mktemp -d)"
echo "Using temp dir $tmp"
function cleanup() {
    echo "Cleaning up"
    rm -rf $tmp
    echo "Done"
}
trap cleanup INT TERM EXIT
set -e

unzip -q $src -d $tmp
chmod -R +r $tmp
tar -c -z -f $dest -C $tmp .
