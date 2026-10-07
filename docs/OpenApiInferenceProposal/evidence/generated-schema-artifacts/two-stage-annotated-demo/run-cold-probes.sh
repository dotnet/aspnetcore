#!/usr/bin/env bash
set -euo pipefail

output="$1"
application="$2"
mkdir -p "$(dirname "$output")"
echo "scenario,elapsed_ns,allocated_bytes,value" > "$output"
for scenario in same-pass native-exporter image-load; do
  for _ in {1..30}; do
    dotnet "$application" --cold-probe "$scenario" >> "$output"
  done
done
