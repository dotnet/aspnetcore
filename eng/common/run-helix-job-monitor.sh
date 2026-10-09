#!/usr/bin/env bash
set -euo pipefail

scriptroot="$(cd -P "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
. "$scriptroot/pipeline-logging-functions.sh"

toolArgs=(
  --helix-base-uri "$HELIX_MONITOR_BASE_URI"
  --use-entra-authentication "$HELIX_MONITOR_USE_ENTRA_AUTHENTICATION"
  --polling-interval-seconds "$HELIX_MONITOR_POLLING_INTERVAL_SECONDS"
  --fail-on-failed-tests "$HELIX_MONITOR_FAIL_ON_FAILED_TESTS"
  --allow-no-helix-jobs "$HELIX_MONITOR_ALLOW_NO_HELIX_JOBS"
  --use-fully-qualified-test-name "$HELIX_MONITOR_USE_FULLY_QUALIFIED_TEST_NAME"
  --max-wait-minutes "$((HELIX_MONITOR_TIMEOUT_IN_MINUTES - 5))"
  --stage-name "$SYSTEM_STAGENAME"
  --stage-attempt "$SYSTEM_STAGEATTEMPT"
  --job-attempt "$SYSTEM_JOBATTEMPT"
  --test-result-upload-parallelism "$HELIX_MONITOR_TEST_RESULT_UPLOAD_PARALLELISM"
)

organization="${HELIX_MONITOR_ORGANIZATION:-}"
repository="${HELIX_MONITOR_REPOSITORY:-}"

# Fall back to Azure DevOps-provided environment variables when the caller did not
# supply organization or repository explicitly.
if [ -z "$organization" ] || [ -z "$repository" ]; then
  buildRepoName="${BUILD_REPOSITORY_NAME:-}"
  if [ -n "$buildRepoName" ] && [[ "$buildRepoName" == */* ]]; then
    repoOwner="${buildRepoName%%/*}"
    repoName="${buildRepoName#*/}"
  elif [ -n "$buildRepoName" ] && [[ "$buildRepoName" == *-* ]]; then
    repoOwner="${buildRepoName%%-*}"
    repoName="${buildRepoName#*-}"
  fi

  if [ -n "${repoOwner:-}" ] && [ -n "${repoName:-}" ]; then
    if [ -z "$organization" ]; then organization="$repoOwner"; fi
    if [ -z "$repository" ]; then repository="$repoName"; fi
  fi
fi

if [ -n "$organization" ]; then toolArgs+=( --organization "$organization" ); fi
if [ -n "$repository" ]; then toolArgs+=( --repository "$repository" ); fi
if [ -n "${HELIX_MONITOR_TEST_RESULT_ATTACHMENT_MODE:-}" ]; then
  toolArgs+=( --test-result-attachment-mode "$HELIX_MONITOR_TEST_RESULT_ATTACHMENT_MODE" )
fi

# These values let the monitor derive the same Helix source filter as the SDK submitter.
toolArgs+=( --build-reason "$BUILD_REASON" )
toolArgs+=( --source-branch "$BUILD_SOURCEBRANCH" )

cd "$BUILD_SOURCESDIRECTORY"
exitCode=0
if [ -n "${HELIX_MONITOR_TOOL_NUPKG_ARTIFACT_NAME:-}" ]; then
  export DOTNET_ROOT="$BUILD_SOURCESDIRECTORY/.dotnet"
  ./eng/common/dotnet.sh exec "$HELIXJOBMONITORDLL" "${toolArgs[@]}" || exitCode=$?
else
  ./eng/common/dotnet.sh tool run "$HELIX_MONITOR_TOOL_COMMAND" -- "${toolArgs[@]}" || exitCode=$?
fi

if [ "$exitCode" -ne 0 ]; then
  Write-PipelineTelemetryError -force -category 'Helix' "Helix job monitor failed (exit code '$exitCode')."
  exit "$exitCode"
fi
