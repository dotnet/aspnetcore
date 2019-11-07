#!/usr/bin/env sh

if [ -n "$1" ]; then
    remote_repo="$1"
else
    remote_repo="$COREFX_REPO"
fi

if [ -z "$remote_repo" ]; then
    echo The 'COREFX_REPO' environment variable or command line paramter is not set, aborting.
    exit 1
fi

echo "COREFX_REPO: $remote_repo"

rsync -av --delete ./ "$remote_repo"/src/Common/src/System/Net/Http/Http2
rsync -av --delete ./../test/Shared.Tests/Http2/ "$remote_repo"/src/Common/tests/Tests/System/Net/Http2
