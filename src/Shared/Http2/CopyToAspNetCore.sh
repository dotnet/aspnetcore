#!/usr/bin/env sh

if [ -n "$1" ]; then
    remote_repo="$1"
else
    remote_repo="$ASPNETCORE_REPO"
fi

if [ -z "$remote_repo" ]; then
    echo The 'ASPNETCORE_REPO' environment variable or command line paramter is not set, aborting.
    exit 1
fi

echo "ASPNETCORE_REPO: $remote_repo"

rsync -av --delete ./ "$remote_repo"/src/Shared/Http2
rsync -av --delete ./../../../../../tests/Tests/System/Net/Http2/ "$remote_repo"/src/Shared/test/Shared.Tests/Http2
