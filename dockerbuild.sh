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
build_args=()
docker_args=()

#
# Functions
#
__usage() {
    echo "Usage: $(basename "${BASH_SOURCE[0]}") <image> [options] [[--] <Arguments>...]"
    echo ""
    echo "Arguments:"
    echo "    image                      The docker image to use."
    echo "    <Arguments>...             Arguments passed to the command. Variable number of arguments allowed."
    echo ""
    echo "Options:"
    echo "    -v, --volume <VOLUME>      An additional volume mount to add to the build container"
    echo ""
    echo "Description:"
    echo "    This will run build.sh inside the dockerfile as defined in build/docker/\$image.Dockerfile."

    if [[ "${1:-}" != '--no-exit' ]]; then
        exit 2
    fi
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

#
# main
#

image="${1:-}"
shift || True

while [[ $# -gt 0 ]]; do
    case $1 in
        -\?|-h|--help)
            __usage --no-exit
            exit 0
            ;;
        -v|--volume)
            shift
            volume_spec="${1:-}"
            [ -z "$volume_spec" ] && __error "Missing value for parameter --volume" && __usage
            docker_args[${#docker_args[*]}]="--volume"
            docker_args[${#docker_args[*]}]="$volume_spec"
            ;;
        *)
            build_args[${#build_args[*]}]="$1"
            ;;
    esac
    shift
done

if [ -z "$image" ]; then
    __usage --no-exit
    __error 'Missing required argument: image'
    exit 1
fi

if ! __machine_has docker; then
    __error 'Missing required command: docker'
    exit 1
fi

dockerfile="$DIR/build/docker/$image.Dockerfile"
tagname="universe-build-$image"

docker build "$(dirname "$dockerfile")" \
    --build-arg "USER=$(whoami)" \
    --build-arg "USER_ID=$(id -u)" \
    --build-arg "GROUP_ID=$(id -g)" \
    --tag $tagname \
    -f "$dockerfile"

docker run \
    --rm \
    -t \
    -e CI \
    -e TEAMCITY_VERSION \
    -e DOTNET_CLI_TELEMETRY_OPTOUT \
    -e Configuration \
    -v "$DIR:/code/build" \
    ${docker_args[@]+"${docker_args[@]}"} \
    $tagname \
    ./build.sh \
    ${build_args[@]+"${build_args[@]}"}
