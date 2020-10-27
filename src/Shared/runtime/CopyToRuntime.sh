#!/usr/bin/env bash

if [[ -n "$1" ]]; then
    remote_repo="$1"
else
    remote_repo="$RUNTIME_REPO"
fi

if [[ -z "$remote_repo" ]]; then
    echo The 'RUNTIME_REPO' environment variable or command line parameter is not set, aborting.
    exit 1
fi

cd "$(dirname "$0")" || exit 1

echo "RUNTIME_REPO: $remote_repo"

rsync -av --delete ./ "$remote_repo"/src/libraries/Common/src/System/Net/Http/aspnetcore
rsync -av --delete ./../test/Shared.Tests/runtime/ "$remote_repo"/src/libraries/Common/tests/Tests/System/Net/aspnetcore
