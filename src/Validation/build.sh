#!/usr/bin/env bash

set -euo pipefail

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
"$DIR/../../eng/build.sh" --projects "$DIR/**/*.csproj" "$@"