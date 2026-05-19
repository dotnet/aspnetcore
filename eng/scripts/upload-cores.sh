#!/usr/bin/env bash

set -euo pipefail
RESET="\033[0m"
YELLOW="\033[0;33m"

__warn() {
  echo -e "${YELLOW}warning: $*${RESET}"
  if [ -n "${TF_BUILD:-}" ]; then
    echo "##vso[task.logissue type=warning]$*"
  fi
}

if [ -n "${SYSTEM_DEFAULTWORKINGDIRECTORY:-}" ]; then
  jobName="${SYSTEM_PHASENAME:-$AGENT_OS}"
  artifactName="${jobName}_Dumps"
  wd=$SYSTEM_DEFAULTWORKINGDIRECTORY
else
  artifactName=Artifacts_Dumps
  wd=$(pwd -P)
fi

save_nullglob=$(shopt -p nullglob || true)
shopt -s nullglob
files=(
  $wd/core*
  $wd/dotnet-*.core
)
$save_nullglob

if [ -z "${files:-}" ] || (( ${#files[@]} == 0 )); then
  __warn "No core files found."
else
  for file in ${files[@]}; do
    echo "##vso[artifact.upload containerfolder=$artifactName;artifactname=$artifactName]$file"
  done
fi
