#!/usr/bin/env bash

function Write-PipelineTelemetryError {
  local telemetry_category=''
  local force=false
  local function_args=()
  local message=''
  while [[ $# -gt 0 ]]; do
    opt="$(echo "${1/#--/-}" | tr "[:upper:]" "[:lower:]")"
    case "$opt" in
      -category|-c)
        telemetry_category=$2
        shift
        ;;
      -force|-f)
        force=true
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

  if [[ $force != true ]] && [[ "$ci" != true ]]; then
    echo "$message" >&2
    return
  fi

  if [[ $force == true ]]; then
    function_args+=("-force")
  fi
  message="(NETCORE_ENGINEERING_TELEMETRY=$telemetry_category) $message"
  function_args+=("$message")
  Write-PipelineTaskError ${function_args[@]}
}

function Write-PipelineTaskError {
  local message_type="error"
  local sourcepath=''
  local linenumber=''
  local columnnumber=''
  local error_code=''
  local force=false

  while [[ $# -gt 0 ]]; do
    opt="$(echo "${1/#--/-}" | tr "[:upper:]" "[:lower:]")"
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
      -force|-f)
        force=true
        ;;
      *)
        break
        ;;
    esac

    shift
  done

  if [[ $force != true ]] && [[ "$ci" != true ]]; then
    echo "$@" >&2
    return
  fi

  local message="##vso[task.logissue"

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

function Write-PipelineSetVariable {
  if [[ "$ci" != true ]]; then
    return
  fi

  local name=''
  local value=''
  local secret=false
  local as_output=false
  local is_multi_job_variable=true

  while [[ $# -gt 0 ]]; do
    opt="$(echo "${1/#--/-}" | tr "[:upper:]" "[:lower:]")"
    case "$opt" in
      -name|-n)
        name=$2
        shift
        ;;
      -value|-v)
        value=$2
        shift
        ;;
      -secret|-s)
        secret=true
        ;;
      -as_output|-a)
        as_output=true
        ;;
      -is_multi_job_variable|-i)
        is_multi_job_variable=$2
        shift
        ;;
    esac
    shift
  done

  value=${value/;/%3B}
  value=${value/\\r/%0D}
  value=${value/\\n/%0A}
  value=${value/]/%5D}

  local message="##vso[task.setvariable variable=$name;isSecret=$secret;isOutput=$is_multi_job_variable]$value"

  if [[ "$as_output" == true ]]; then
    $message
  else
    echo "$message"
  fi
}

function Write-PipelinePrependPath {
  local prepend_path=''

  while [[ $# -gt 0 ]]; do
    opt="$(echo "${1/#--/-}" | tr "[:upper:]" "[:lower:]")"
    case "$opt" in
      -path|-p)
        prepend_path=$2
        shift
        ;;
    esac
    shift
  done

  export PATH="$prepend_path:$PATH"

  if [[ "$ci" == true ]]; then
    echo "##vso[task.prependpath]$prepend_path"
  fi
}

function Write-PipelineSetResult {
  local result=''
  local message=''

  while [[ $# -gt 0 ]]; do
    opt="$(echo "${1/#--/-}" | tr "[:upper:]" "[:lower:]")"
    case "$opt" in
      -result|-r)
        result=$2
        shift
        ;;
      -message|-m)
        message=$2
        shift
        ;;
    esac
    shift
  done

  if [[ "$ci" == true ]]; then
    echo "##vso[task.complete result=$result;]$message"
  fi
}
