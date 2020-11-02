#!/usr/bin/env bash

 #
# This script is meant for testing source build by imitating some of the input parameters and conditions.
#

set -euo pipefail

scriptroot="$( cd -P "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
reporoot="$(dirname "$(dirname "$scriptroot")")"

#
# This commented out section is used for servicing branches
#
# For local development, make a backup copy of this file first
# if [ ! -f "$reporoot/global.bak.json" ]; then
#    mv "$reporoot/global.json" "$reporoot/global.bak.json"
# fi

# Detect the current version of .NET Core installed
# export SDK_VERSION=$(dotnet --version)
# echo "The ambient version of .NET Core SDK version = $SDK_VERSION"

# Update the global.json file to match the current .NET environment
# cat "$reporoot/global.bak.json" | \
#    jq '.sdk.version=env.SDK_VERSION' | \
#    jq '.tools.dotnet=env.SDK_VERSION' | \
#    jq 'del(.tools.runtimes)' \
#    > "$reporoot/global.json"

# Restore the original global.json file
#trap "{
#    mv "$reporoot/global.bak.json" "$reporoot/global.json"
#}" EXIT

runtime_source_feed=''
runtime_source_feed_key=''
other_args=()

#
# Functions
#
__usage() {
    echo "Usage: $(basename "${BASH_SOURCE[0]}") [options] [[--] <Arguments>...]

Arguments:
    <Arguments>...                    Arguments passed to the command. Variable number of arguments allowed.

    --runtime-source-feed             Additional feed that can be used when downloading .NET runtimes and SDKs
    --runtime-source-feed-key         Key for feed that can be used when downloading .NET runtimes and SDKs

Description:
   This script is meant for testing source build by imitating some of the input parameters and conditions.
"

    if [[ "${1:-}" != '--no-exit' ]]; then
        exit 2
    fi
}

__error() {
    echo -e "${RED}error: $*${RESET}" 1>&2
}

#
# main
#

while [[ $# -gt 0 ]]; do
    opt="$(echo "${1/#--/-}" | awk '{print tolower($0)}')"
    case "$opt" in
        -\?|-h|-help)
            __usage --no-exit
            exit 0
            ;;
        -dotnet-runtime-source-feed|-dotnetruntimesourcefeed|-runtime_source_feed|-runtimesourcefeed)
            shift
            [ -z "${1:-}" ] && __error "Missing value for parameter --runtime-source-feed" && __usage
            runtime_source_feed="${1:-}"
            ;;
        -dotnet-runtime-source-feed-key|-dotnetruntimesourcefeedkey|-runtime_source_feed_key|-runtimesourcefeedkey)
            shift
            [ -z "${1:-}" ] && __error "Missing value for parameter --runtime-source-feed-key" && __usage
            runtime_source_feed_key="${1:-}"
            ;;
        *)
            other_args[${#other_args[*]}]="$1"
            ;;
    esac
    shift
done

# Set up additional runtime args
runtime_feed_args=()
if [ ! -z "$runtime_source_feed$runtime_source_feed_key" ]; then
    runtime_feed_args[${#runtime_feed_args[*]}]="-runtimesourcefeed"
    runtime_feed_args[${#runtime_feed_args[*]}]="$runtime_source_feed"
    runtime_feed_args[${#runtime_feed_args[*]}]="-runtimesourcefeedKey"
    runtime_feed_args[${#runtime_feed_args[*]}]="$runtime_source_feed_key"

    runtimeFeedArg="/p:DotNetRuntimeSourceFeed=$runtime_source_feed"
    runtimeFeedKeyArg="/p:DotNetRuntimeSourceFeedKey=$runtime_source_feed_key"
    runtime_feed_args[${#runtime_feed_args[*]}]=$runtimeFeedArg
    runtime_feed_args[${#runtime_feed_args[*]}]=$runtimeFeedKeyArg
fi

# Build repo tasks
"$reporoot/eng/common/build.sh" --restore --build --ci --configuration Release /p:ProjectToBuild=$reporoot/eng/tools/RepoTasks/RepoTasks.csproj ${runtime_feed_args[@]+"${runtime_feed_args[@]}"}

export DotNetBuildFromSource='true'

# Build projects
"$reporoot/eng/common/build.sh" --restore --build --ci --pack ${other_args[@]+"${other_args[@]}"} ${runtime_feed_args[@]+"${runtime_feed_args[@]}"}
