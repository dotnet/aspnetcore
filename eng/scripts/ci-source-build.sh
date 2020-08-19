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

# Build repo tasks
"$reporoot/eng/common/build.sh" --restore --build --ci --configuration Release /p:ProjectToBuild=$reporoot/eng/tools/RepoTasks/RepoTasks.csproj

export DotNetBuildFromSource='true'

# Build projects
"$reporoot/eng/common/build.sh" --restore --build --pack "$@"