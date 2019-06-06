#!/usr/bin/env bash

set -euo pipefail

export PATH="$PATH:$HOME/nginxinstall/sbin/"

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
repo_root="$DIR/../.."
"$repo_root/build.sh" --ci --all --pack "$@"
