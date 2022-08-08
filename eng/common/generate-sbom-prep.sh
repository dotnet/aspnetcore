#!/usr/bin/env bash

source="${BASH_SOURCE[0]}"

# resolve $SOURCE until the file is no longer a symlink
while [[ -h $source ]]; do
  scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"
  source="$(readlink "$source")"

  # if $source was a relative symlink, we need to resolve it relative to the path where the
  # symlink file was located
  [[ $source != /* ]] && source="$scriptroot/$source"
done
scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"
. $scriptroot/pipeline-logging-functions.sh

manifest_dir=$1

if [ ! -d "$manifest_dir" ] ; then
  mkdir -p "$manifest_dir"
  echo "Sbom directory created." $manifest_dir
else
  Write-PipelineTelemetryError -category 'Build'  "Unable to create sbom folder."
fi

artifact_name=$SYSTEM_STAGENAME"_"$AGENT_JOBNAME"_SBOM"
echo "Artifact name before : "$artifact_name
# replace all special characters with _, some builds use special characters like : in Agent.Jobname, that is not a permissible name while uploading artifacts.
safe_artifact_name="${artifact_name//["/:<>\\|?@*$" ]/_}"
echo "Artifact name after : "$safe_artifact_name
export ARTIFACT_NAME=$safe_artifact_name
echo "##vso[task.setvariable variable=ARTIFACT_NAME]$safe_artifact_name"

exit 0
