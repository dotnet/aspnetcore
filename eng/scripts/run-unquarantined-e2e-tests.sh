#!/usr/bin/env bash
# Run unquarantined Blazor E2E tests and fail if no tests match the filter
set -euo pipefail

CONFIGURATION="$1"
SOURCES_DIR="$2"
OUTPUT_LOG="e2e-test-output.log"

.dotnet/dotnet test ./src/Components/test/E2ETest \
  -c "${CONFIGURATION}" \
  --no-build \
  --filter 'Quarantined!=true|Quarantined=false' \
  -p:VsTestUseMSBuildOutput=false \
  --logger:"trx;LogFileName=Microsoft.AspNetCore.Components.E2ETests.trx" \
  --logger:"html;LogFileName=Microsoft.AspNetCore.Components.E2ETests.html" \
  --results-directory "${SOURCES_DIR}/artifacts/TestResults/${CONFIGURATION}/Unquarantined" \
  | tee "${OUTPUT_LOG}"

if grep -q "No test matches the given testcase filte" "${OUTPUT_LOG}"; then
  echo "##vso[task.logissue type=error] No tests matched the filter."
  exit 1
fi
