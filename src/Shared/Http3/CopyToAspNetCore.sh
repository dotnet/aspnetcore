#!/usr/bin/env bash

if [[ -n "$1" ]]; then
    remote_repo="$1"
else
    remote_repo="$ASPNETCORE_REPO"
fi

if [[ -z "$remote_repo" ]]; then
    echo The 'ASPNETCORE_REPO' environment variable or command line parameter is not set, aborting.
    exit 1
fi

cd "$(dirname "$0")" || exit 1

echo "ASPNETCORE_REPO: $remote_repo"

rsync -av --delete ./ "$remote_repo"/src/Shared/Http3
rsync -av --delete ./../../../../../tests/Tests/System/Net/Http3/ "$remote_repo"/src/Shared/test/Shared.Tests/Http3
