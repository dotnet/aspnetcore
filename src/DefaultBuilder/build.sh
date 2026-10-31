#!/usr/bin/env bash

set -euo pipefail

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
repo_root="$DIR/../.."
"$repo_root/eng/build.sh" --projects "$DIR/**/*.*proj" "$@"
