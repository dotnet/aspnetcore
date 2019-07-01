#!/usr/bin/env bash

function Write-PipelineTelemetryError {
  local telemetry_category=''
  local function_args=()
  local message=''
  while [[ $# -gt 0 ]]; do
    opt="$(echo "${1/#--/-}" | awk '{print tolower($0)}')"
    case "$opt" in
      -category|-c)
        telemetry_category=$2
        shift
        ;;
      -*)
        function_args+=("$1 $2")
        shift
        ;;
      *)
        message=$*
        ;;
    esac
    shift
  done

  if [[ "$ci" != true ]]; then
    echo "$message" >&2
    return
  fi

  message="(NETCORE_ENGINEERING_TELEMETRY=$telemetry_category) $message"
  function_args+=("$message")

  Write-PipelineTaskError $function_args
}

function Write-PipelineTaskError {
  if [[ "$ci" != true ]]; then
    echo "$@" >&2
    return
  fi

  message_type="error"
  sourcepath=''
  linenumber=''
  columnnumber=''
  error_code=''

  while [[ $# -gt 0 ]]; do
    opt="$(echo "${1/#--/-}" | awk '{print tolower($0)}')"
    case "$opt" in
      -type|-t)
        message_type=$2
        shift
        ;;
      -sourcepath|-s)
        sourcepath=$2
        shift
        ;;
      -linenumber|-ln)
        linenumber=$2
        shift
        ;;
      -columnnumber|-cn)
        columnnumber=$2
        shift
        ;;
      -errcode|-e)
        error_code=$2
        shift
        ;;
      *)
        break
        ;;
    esac

    shift
  done

  message="##vso[task.logissue"

  message="$message type=$message_type"

  if [ -n "$sourcepath" ]; then
    message="$message;sourcepath=$sourcepath"
  fi

  if [ -n "$linenumber" ]; then
    message="$message;linenumber=$linenumber"
  fi

  if [ -n "$columnnumber" ]; then
    message="$message;columnnumber=$columnnumber"
  fi

  if [ -n "$error_code" ]; then
    message="$message;code=$error_code"
  fi

  message="$message]$*"
  echo "$message"
}

