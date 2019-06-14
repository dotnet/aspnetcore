#!/usr/bin/env bash

set -euo pipefail

#
# Functions
#
__usage() {
    echo "Usage: $(basename "${BASH_SOURCE[0]}") <Arguments>

Arguments:
    <Arguments>...         Arguments passed to the 'git' command. Any number of arguments allowed.

Description:
    This script cleans the repository interactively, leaving downloaded infrastructure untouched.
    Clean operation is interactive to avoid losing new but unstaged files. Press 'c' then [Enter]
    to perform the proposed deletions.
"
}

git_args=()

while [[ $# -gt 0 ]]; do
    case $1 in
        -\?|-h|--help)
            __usage
            exit 0
            ;;
        *)
            git_args[${#git_args[*]}]="$1"
            ;;
    esac
    shift
done

# This incantation avoids unbound variable issues if git_args is empty
# https://stackoverflow.com/questions/7577052/bash-empty-array-expansion-with-set-u
git clean -dix -e .dotnet/ -e .tools/ ${git_args[@]+"${git_args[@]}"}
