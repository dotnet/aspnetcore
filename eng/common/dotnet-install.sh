#!/usr/bin/env bash

source="${BASH_SOURCE[0]}"
# resolve $source until the file is no longer a symlink
while [[ -h "$source" ]]; do
  scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"
  source="$(readlink "$source")"
  # if $source was a relative symlink, we need to resolve it relative to the path where the
  # symlink file was located
  [[ $source != /* ]] && source="$scriptroot/$source"
done
scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"

version='Latest'
architecture=''
runtime='dotnet'
runtimeSourceFeed=''
runtimeSourceFeedKey=''
while [[ $# > 0 ]]; do
  opt="$(echo "$1" | awk '{print tolower($0)}')"
  case "$opt" in
    -version|-v)
      shift
      version="$1"
      ;;
    -architecture|-a)
      shift
      architecture="$1"
      ;;
    -runtime|-r)
      shift
      runtime="$1"
      ;;
    -runtimesourcefeed)
      shift
      runtimeSourceFeed="$1"
      ;;
    -runtimesourcefeedkey)
      shift
      runtimeSourceFeedKey="$1"
      ;;
    *)
      echo "Invalid argument: $1"
      exit 1
      ;;
  esac
  shift
done

# Use uname to determine what the CPU is.
cpuname=$(uname -p)
# Some Linux platforms report unknown for platform, but the arch for machine.
if [[ "$cpuname" == "unknown" ]]; then
  cpuname=$(uname -m)
fi

case $cpuname in
  aarch64)
    buildarch=arm64
    ;;
  amd64|x86_64)
    buildarch=x64
    ;;
  armv7l)
    buildarch=arm
    ;;
  i686)
    buildarch=x86
    ;;
  *)
    echo "Unknown CPU $cpuname detected, treating it as x64"
    buildarch=x64
    ;;
esac

. "$scriptroot/tools.sh"
dotnetRoot="$repo_root/.dotnet"
if [[ $architecture != "" ]] && [[ $architecture != $buildarch ]]; then
  dotnetRoot="$dotnetRoot/$architecture"
fi

InstallDotNet $dotnetRoot $version "$architecture" $runtime true $runtimeSourceFeed $runtimeSourceFeedKey || {
  local exit_code=$?
  echo "dotnet-install.sh failed (exit code '$exit_code')." >&2
  ExitWithExitCode $exit_code
}

ExitWithExitCode 0
