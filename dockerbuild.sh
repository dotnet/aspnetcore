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
    echo "    -e, --env <NAME=VAL>       Additional environment variables to add to the build container"
    echo ""
    echo "Description:"
    echo "    This will run build.sh inside the dockerfile as defined in eng/docker/\$image.Dockerfile."

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
        -e|--env)
            shift
            env_var="${1:-}"
            [ -z "$env_var" ] && __error "Missing value for parameter --env" && __usage
            docker_args[${#docker_args[*]}]="-e"
            docker_args[${#docker_args[*]}]="$env_var"
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

commit_hash="$(git rev-parse HEAD || true)"

if [ ! -z "$commit_hash" ]; then
    build_args[${#build_args[*]}]="-p:SourceRevisionId=$commit_hash"
fi

dockerfile="$DIR/eng/docker/$image.Dockerfile"
tagname="aspnetcore-build-$image"

# Use docker pull with retries to pre-pull the image need by the dockerfile
# docker build regularly fails with TLS handshake issues for unclear reasons.
base_imagename="$(grep -E -o 'FROM (.*)' $dockerfile | cut -c 6-)"
pull_retries=3
while [ $pull_retries -gt 0 ]; do
    failed=false
    docker pull $base_imagename || failed=true
    if [ "$failed" = true ]; then
        let pull_retries=pull_retries-1
        echo -e "${YELLOW}Failed to pull $base_imagename Retries left: $pull_retries.${RESET}"
        sleep 1
    else
        pull_retries=0
    fi
done

docker build "$(dirname "$dockerfile")" \
    --build-arg "USER=$(whoami)" \
    --build-arg "USER_ID=$(id -u)" \
    --build-arg "GROUP_ID=$(id -g)" \
    --build-arg "WORKDIR=$DIR" \
    --tag $tagname \
    -f "$dockerfile"

docker run \
    --rm \
    -t \
    -e TF_BUILD \
    -e BUILD_NUMBER \
    -e BUILD_BUILDNUMBER \
    -e BUILD_REPOSITORY_URI \
    -e BUILD_SOURCEVERSION \
    -e BUILD_SOURCEBRANCH \
    -e DOTNET_CLI_TELEMETRY_OPTOUT \
    -e Configuration \
    -v "$DIR:$DIR" \
    ${docker_args[@]+"${docker_args[@]}"} \
    $tagname \
    ./build.sh \
    ${build_args[@]+"${build_args[@]}"}
