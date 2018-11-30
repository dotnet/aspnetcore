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

verbosity='minimal'
warnaserror=true
nodereuse=true
prepare_machine=false
extraargs=''

while (($# > 0)); do
  lowerI="$(echo $1 | awk '{print tolower($0)}')"
  case $lowerI in
    --verbosity)
      verbosity=$2
      shift 2
      ;;
    --warnaserror)
      warnaserror=$2
      shift 2
      ;;
    --nodereuse)
      nodereuse=$2
      shift 2
      ;;
    --ci)
      ci=true
      shift 1
      ;;
    --preparemachine)
      prepare_machine=true
      shift 1
      ;;
      *)
      extraargs="$extraargs $1"
      shift 1
      ;;
  esac
done

. "$scriptroot/tools.sh"

InitializeTools
MSBuild $extraargs
ExitWithExitCode $?
