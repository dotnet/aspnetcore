#!/usr/bin/env bash

set -euo pipefail

#
# variables
#

RESET="\033[0m"
RED="\033[0;31m"
MAGENTA="\033[0;95m"
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
[ -z "${DOTNET_HOME:-}"] && DOTNET_HOME="$HOME/.dotnet"
config_file="$DIR/version.xml"
verbose=false
update=false
repo_path="$DIR"
channel=''
tools_source=''

#
# Functions
#
__usage() {
    echo "Usage: $(basename ${BASH_SOURCE[0]}) [options] [[--] <MSBUILD_ARG>...]"
    echo ""
    echo "Arguments:"
    echo "    <MSBUILD_ARG>...         Arguments passed to MSBuild. Variable number of arguments allowed."
    echo ""
    echo "Options:"
    echo "    --verbose                Show verbose output."
    echo "    -c|--channel <CHANNEL>   The channel of KoreBuild to download. Overrides the value from the config file.."
    echo "    --config-file <FILE>     TThe path to the configuration file that stores values. Defaults to version.xml."
    echo "    -d|--dotnet-home <DIR>   The directory where .NET Core tools will be stored. Defaults to '\$DOTNET_HOME' or '\$HOME/.dotnet."
    echo "    --path <PATH>            The directory to build. Defaults to the directory containing the script."
    echo "    -s|--tools-source <URL>  The base url where build tools can be downloaded. Overrides the value from the config file."
    echo "    -u|--update              Update to the latest KoreBuild even if the lock file is present."
    echo ""
    echo "Description:"
    echo "    This function will create a file \$DIR/korebuild-lock.txt. This lock file can be committed to source, but does not have to be."
    echo "    When the lockfile is not present, KoreBuild will create one using latest available version from \$channel."

    if [[ "${1:-}" != '--no-exit' ]]; then
        exit 2
    fi
}

get_korebuild() {
    local lock_file="$repo_path/korebuild-lock.txt"
    if [ ! -f $lock_file ] || [ "$update" = true ]; then
        __get_remote_file "$tools_source/korebuild/channels/$channel/latest.txt" $lock_file
    fi
    local version="$(grep 'version:*' -m 1 $lock_file)"
    if [[ "$version" == '' ]]; then
        __error "Failed to parse version from $lock_file. Expected a line that begins with 'version:'"
        return 1
    fi
    version="$(echo ${version#version:} | sed -e 's/^[[:space:]]*//' -e 's/[[:space:]]*$//')"
    local korebuild_path="$DOTNET_HOME/buildtools/korebuild/$version"

    {
        if [ ! -d "$korebuild_path" ]; then
            mkdir -p "$korebuild_path"
            local remote_path="$tools_source/korebuild/artifacts/$version/korebuild.$version.zip"
            tmpfile="$(mktemp)"
            echo -e "${MAGENTA}Downloading KoreBuild ${version}${RESET}"
            if __get_remote_file $remote_path $tmpfile; then
                unzip -q -d "$korebuild_path" $tmpfile
            fi
            rm $tmpfile || true
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
    echo -e "${RED}$@${RESET}" 1>&2
}

__machine_has() {
    hash "$1" > /dev/null 2>&1
    return $?
}

__get_remote_file() {
    local remote_path=$1
    local local_path=$2

    if [[ "$remote_path" != 'http'* ]]; then
        cp $remote_path $local_path
        return 0
    fi

    failed=false
    if __machine_has wget; then
        wget --tries 10 --quiet -O $local_path $remote_path || failed=true
    fi

    if [ "$failed" = true ] && __machine_has curl; then
        failed=false
        curl --retry 10 -sSL -f --create-dirs -o $local_path $remote_path || failed=true
    fi

    if [ "$failed" = true ]; then
        __error "Download failed: $remote_path" 1>&2
        return 1
    fi
}

__read_dom () { local IFS=\> ; read -d \< ENTITY CONTENT ;}

#
# main
#

while [[ $# > 0 ]]; do
    case $1 in
        -\?|-h|--help)
            __usage --no-exit
            exit 0
            ;;
        -c|--channel|-Channel)
            shift
            channel=${1:-}
            [ -z "$channel" ] && __usage
            ;;
        --config-file|-ConfigFile)
            shift
            config_file="${1:-}"
            [ -z "$config_file" ] && __usage
            ;;
        -d|--dotnet-home|-DotNetHome)
            shift
            DOTNET_HOME=${1:-}
            [ -z "$DOTNET_HOME" ] && __usage
            ;;
        --path|-Path)
            shift
            repo_path="${1:-}"
            [ -z "$repo_path" ] && __usage
            ;;
        -s|--tools-source|-ToolsSource)
            shift
            tools_source="${1:-}"
            [ -z "$tools_source" ] && __usage
            ;;
        -u|--update|-Update)
            update=true
            ;;
        --verbose|-Verbose)
            verbose=true
            ;;
        --)
            shift
            break
            ;;
        *)
            break
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

if [ -f $config_file ]; then
    comment=false
    while __read_dom; do
        if [ "$comment" = true ]; then [[ $CONTENT == *'-->'* ]] && comment=false ; continue; fi
        if [[ $ENTITY == '!--'* ]]; then comment=true; continue; fi
        if [ -z "$channel" ] && [[ $ENTITY == "KoreBuildChannel" ]]; then channel=$CONTENT; fi
        if [ -z "$tools_source" ] && [[ $ENTITY == "KoreBuildToolsSource" ]]; then tools_source=$CONTENT; fi
    done < $config_file
fi

[ -z "$channel" ] && channel='dev'
[ -z "$tools_source" ] && tools_source='https://aspnetcore.blob.core.windows.net/buildtools'

get_korebuild
install_tools "$tools_source" "$DOTNET_HOME"
invoke_repository_build "$repo_path" $@
