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
    *)
      echo "Invalid argument: $1"
      usage
      exit 1
      ;;
  esac
  shift
done

. "$scriptroot/tools.sh"
dotnetRoot="$repo_root/.dotnet"
InstallDotNet $dotnetRoot $version "$architecture" $runtime true || {
  local exit_code=$?
  echo "dotnet-install.sh failed (exit code '$exit_code')." >&2
  ExitWithExitCode $exit_code
}

ExitWithExitCode 0
