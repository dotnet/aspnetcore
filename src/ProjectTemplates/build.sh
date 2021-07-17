#!/usr/bin/env bash

set -euo pipefail

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
repo_root="$DIR/../.."
"$repo_root/eng/build.sh" --projects "$DIR/**/*.*proj" "/p:EnforceE2ETestPrerequisites=true" "$@"
"$repo_root/eng/build.sh" --projects "$DIR/../submodules/spa-templates/src/*.*proj" --no-build-repo-tasks "/p:EnforceE2ETestPrerequisites=true" "$@"
