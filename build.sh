#!/usr/bin/env bash

set -euo pipefail

#
# variables
#

RESET="\033[0m"
RED="\033[0;31m"
YELLOW="\033[0;33m"
MAGENTA="\033[0;95m"
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
verbose=false
update=false
reinstall=false
repo_path="$DIR"
lockfile_path="$DIR/korebuild-lock.txt"
config_file="$DIR/korebuild.json"
channel='master'
tools_source='https://aspnetcore.blob.core.windows.net/buildtools'
ci=false
run_restore=true
run_build=true
run_pack=false
run_tests=false
build_all=false
build_managed=false
build_nodejs=false
build_projects=''
msbuild_args=()

#
# Functions
#
__usage() {
    echo "Usage: $(basename "${BASH_SOURCE[0]}") [options] [[--] <Arguments>...]

Arguments:
    <Arguments>...     Arguments passed to the command. Variable number of arguments allowed.

Options:
    --[no-]restore     Run restore.
    --[no-]build       Compile projects
    --[no-]pack        Produce packages.
    --[no-]test        Run tests.

    --projects         A list of projects to build. (Must be an absolute path.)
                       Globbing patterns are supported, such as \"$(pwd)/**/*.csproj\".

    --all              Build all project types.
    --managed          Build managed projects (C#, F#, VB).
    --nodejs           Build NodeJS projects (TypeScript, JS).

    --ci               Apply CI specific settings and environment variables.
    --verbose          Show verbose output.

Description:
    This build script installs required tools and runs an MSBuild command on this repository
    This script can be used to invoke various targets, such as targets to produce packages
    build projects, run tests, and generate code.
"

    if [[ "${1:-}" != '--no-exit' ]]; then
        exit 2
    fi
}

get_korebuild() {
    local version
    if [ ! -f "$lockfile_path" ]; then
        __get_remote_file "$tools_source/korebuild/channels/$channel/latest.txt" "$lockfile_path"
    fi
    version="$(grep 'version:*' -m 1 "$lockfile_path")"
    if [[ "$version" == '' ]]; then
        __error "Failed to parse version from $lockfile_path. Expected a line that begins with 'version:'"
        return 1
    fi
    version="$(echo "${version#version:}" | sed -e 's/^[[:space:]]*//' -e 's/[[:space:]]*$//')"
    local korebuild_path="$DOTNET_HOME/buildtools/korebuild/$version"

    {
        if [ ! -d "$korebuild_path" ]; then
            mkdir -p "$korebuild_path"
            local remote_path="$tools_source/korebuild/artifacts/$version/korebuild.$version.zip"
            tmpfile="$(mktemp)"
            echo -e "${MAGENTA}Downloading KoreBuild ${version}${RESET}"
            if __get_remote_file "$remote_path" "$tmpfile"; then
                unzip -q -d "$korebuild_path" "$tmpfile"
            fi
            rm "$tmpfile" || true
        fi

        source "$korebuild_path/KoreBuild.sh"
    } || {
        if [ -d "$korebuild_path" ]; then
            echo "Cleaning up after failed installation"
            rm -rf "$korebuild_path" || true
        fi
        return 1
    }
}

__error() {
    echo -e "${RED}error: $*${RESET}" 1>&2
}

__warn() {
    echo -e "${YELLOW}warning: $*${RESET}"
}

__machine_has() {
    hash "$1" > /dev/null 2>&1
    return $?
}

__get_remote_file() {
    local remote_path=$1
    local local_path=$2

    if [[ "$remote_path" != 'http'* ]]; then
        cp "$remote_path" "$local_path"
        return 0
    fi

    local failed=false
    if __machine_has wget; then
        wget --tries 10 --quiet -O "$local_path" "$remote_path" || failed=true
    else
        failed=true
    fi

    if [ "$failed" = true ] && __machine_has curl; then
        failed=false
        curl --retry 10 -sSL -f --create-dirs -o "$local_path" "$remote_path" || failed=true
    fi

    if [ "$failed" = true ]; then
        __error "Download failed: $remote_path" 1>&2
        return 1
    fi
}

#
# main
#

while [[ $# -gt 0 ]]; do
    case $1 in
        -\?|-h|--help)
            __usage --no-exit
            exit 0
            ;;
        --repo-root|-RepoRoot)
            shift
            __warn '--repo-root is obsolete and will be removed when we finish https://github.com/aspnet/AspNetCore/issues/4246'
            repo_path="${1:-}"
            [ -z "$repo_path" ] && __error "Missing value for parameter --repo-root" && __usage
            ;;
        --restore|-[Rr]estore)
            run_restore=true
            ;;
        --no-restore)
            run_restore=false
            ;;
        --build|-[Bb]build)
            run_build=true
            ;;
        --no-build)
            run_build=false
            ;;
        --pack|-[Pp]ack)
            run_pack=true
            ;;
        --no-pack)
            run_pack=false
            ;;
        --test|-[Tt]est)
            run_tests=true
            ;;
        --no-test)
            run_tests=false
            ;;
        --projects|-[Pp]rojects)
            shift
            build_projects="${1:-}"
            [ -z "$build_projects" ] && __error "Missing value for parameter --projects" && __usage
            ;;
        --all|-[Aa]ll)
            build_all=true
            ;;
        --managed|-[Mm]anaged)
            build_managed=true
            ;;
        --nodejs|-[Nn]ode[Jj][Ss])
            build_nodejs=true
            ;;
        --native|-[Nn]ative)
            __warn 'The C++ projects in this repo only build on Windows. The --native flag will be ignored.'
            ;;
        --ci|-[Cc][Ii])
            ci=true
            if [[ -z "${DOTNET_HOME:-}" ]]; then
                DOTNET_HOME="$DIR/.dotnet"
            fi
            ;;
        --verbose|-[Vv]erbose)
            verbose=true
            ;;
        *)
            msbuild_args[${#msbuild_args[*]}]="$1"
            ;;
    esac
    shift
done

if ! __machine_has unzip; then
    __error 'Missing required command: unzip'
    exit 1
fi

if ! __machine_has curl && ! __machine_has wget; then
    __error 'Missing required command. Either wget or curl is required.'
    exit 1
fi

if [ -f "$config_file" ]; then
    if __machine_has jq ; then
        if jq '.' "$config_file" >/dev/null ; then
            config_channel="$(jq -r 'select(.channel!=null) | .channel' "$config_file")"
            config_tools_source="$(jq -r 'select(.toolsSource!=null) | .toolsSource' "$config_file")"
        else
            __error "$config_file is invalid JSON. Its settings will be ignored."
            exit 1
        fi
    elif __machine_has python ; then
        if python -c "import json,codecs;obj=json.load(codecs.open('$config_file', 'r', 'utf-8-sig'))" >/dev/null ; then
            config_channel="$(python -c "import json,codecs;obj=json.load(codecs.open('$config_file', 'r', 'utf-8-sig'));print(obj['channel'] if 'channel' in obj else '')")"
            config_tools_source="$(python -c "import json,codecs;obj=json.load(codecs.open('$config_file', 'r', 'utf-8-sig'));print(obj['toolsSource'] if 'toolsSource' in obj else '')")"
        else
            __error "$config_file is invalid JSON. Its settings will be ignored."
            exit 1
        fi
    else
        __error 'Missing required command: jq or python. Could not parse the JSON file. Its settings will be ignored.'
        exit 1
    fi

    [ ! -z "${config_channel:-}" ] && channel="$config_channel"
    [ ! -z "${config_tools_source:-}" ] && tools_source="$config_tools_source"
fi

[ -z "${DOTNET_HOME:-}" ] && DOTNET_HOME="$HOME/.dotnet"
export DOTNET_HOME="$DOTNET_HOME"

get_korebuild

if [ "$build_all" = true ]; then
    msbuild_args[${#msbuild_args[*]}]="-p:BuildAllProjects=true"
elif [ ! -z "$build_projects" ]; then
    msbuild_args[${#msbuild_args[*]}]="-p:Projects=$build_projects"
else
    # When adding new sub-group build flags, add them to this check
    if [ "$build_managed" = false ] && [ "$build_nodejs" = false ]; then
        # This goal of this is to pick a sensible default for `build.sh` with zero arguments.
        # We believe the most common thing our contributors will work on is C#, so if no other build group was picked, build the C# projects.
        __warn "No default group of projects was specified, so building the 'managed' subset of projects. Run ``build.sh -help`` for more details."
        build_managed=true
    fi

    msbuild_args[${#msbuild_args[*]}]="-p:BuildManaged=$build_managed"
    msbuild_args[${#msbuild_args[*]}]="-p:BuildNodeJS=$build_nodejs"
fi

msbuild_args[${#msbuild_args[*]}]="-p:_RunRestore=$run_restore"
msbuild_args[${#msbuild_args[*]}]="-p:_RunBuild=$run_build"
msbuild_args[${#msbuild_args[*]}]="-p:_RunPack=$run_pack"
msbuild_args[${#msbuild_args[*]}]="-p:_RunTests=$run_tests"

set_korebuildsettings "$tools_source" "$DOTNET_HOME" "$repo_path" "$config_file" "$ci"

# This incantation avoids unbound variable issues if msbuild_args is empty
# https://stackoverflow.com/questions/7577052/bash-empty-array-expansion-with-set-u
invoke_korebuild_command 'default-build' ${msbuild_args[@]+"${msbuild_args[@]}"}
