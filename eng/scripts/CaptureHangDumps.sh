#!/usr/bin/env bash

# Captures process dumps of any live .NET / Roslyn compiler processes so that
# build or test *hangs* can be investigated. A hang produces no crash dump (so
# COMPlus_DbgEnableMiniDump never fires) and the job is simply canceled when it
# hits its timeout. This script is the non-Windows counterpart to
# StartDumpCollectionForHangingBuilds.ps1.
#
# It is meant to run as an `or(failed(), canceled())` step inside the job's
# cancelTimeoutInMinutes grace window, immediately before "Kill processes"
# terminates the hung processes. Dumps are written next to the crash dumps that
# upload-cores.sh already collects (dotnet-<pid>.core in the working directory),
# so no additional upload wiring is required.
#
# We deliberately capture *minidumps* rather than full dumps. A full
# dump of a hung .NET process can be several GB; capturing several of them and
# then uploading them as artifacts does not fit inside the cancelTimeout grace
# window, so the upload is killed and no dump is ever published. A minidump is a
# few tens of MB, captures in seconds, and uploads comfortably. The runtime
# diagnostics IPC channel requests a .NET-aware dump containing the thread
# stacks and runtime/module memory the DAC needs, so `clrthreads` / `clrstack`
# still work to identify the hung process and its managed stack -- which is all
# a hang investigation needs (heap inspection such as `dumpheap` is not).

set -uo pipefail

RESET="\033[0m"
YELLOW="\033[0;33m"

__warn() {
  echo -e "${YELLOW}warning: $*${RESET}"
  if [ -n "${TF_BUILD:-}" ]; then
    echo "##vso[task.logissue type=warning]$*"
  fi
}

# A 24-byte Server/OK response with a zero HRESULT.
serverOkResponse="444f544e45545f4950435f5631001800ff00000000000000"

# Use a watchdog when timeout is unavailable, as on a default macOS installation.
run_with_timeout() {
  local seconds="$1"
  local inputFile="$2"
  shift 2

  if command -v timeout >/dev/null 2>&1; then
    timeout "$seconds" "$@" < "$inputFile"
    return
  fi

  "$@" < "$inputFile" &
  local commandPid=$!
  (
    sleep "$seconds"
    kill "$commandPid" 2>/dev/null
  ) &
  local watchdogPid=$!
  local status=0

  wait "$commandPid" || status=$?
  kill "$watchdogPid" 2>/dev/null || true
  wait "$watchdogPid" 2>/dev/null || true

  return "$status"
}

byte() {
  printf "\\$(printf '%03o' "$1")"
}

u16le() {
  byte "$(($1 & 255))"
  byte "$((($1 >> 8) & 255))"
}

u32le() {
  byte "$(($1 & 255))"
  byte "$((($1 >> 8) & 255))"
  byte "$((($1 >> 16) & 255))"
  byte "$((($1 >> 24) & 255))"
}

capture_dump() {
  local pid="$1"
  local dump_path="$2"
  local socket=""
  local candidate
  local path_bytes
  local path_chars
  local packet_size
  local packet_file
  local response

  for candidate in "${TMPDIR:-/tmp}"/dotnet-diagnostic-"${pid}"-*-socket; do
    if [ -S "$candidate" ]; then
      socket="$candidate"
      break
    fi
  done

  if [ -z "$socket" ]; then
    echo "Could not find the diagnostics IPC socket for PID $pid." >&2
    return 1
  fi

  if ! path_bytes="$(printf '%s' "$dump_path" | iconv -f UTF-8 -t UTF-16LE | wc -c)"; then
    echo "Could not encode the dump path for PID $pid." >&2
    return 1
  fi
  path_bytes="$(printf '%s' "$path_bytes" | tr -d '[:space:]')"
  path_chars="$((path_bytes / 2 + 1))"
  packet_size="$((20 + 4 + path_bytes + 2 + 4 + 4))"

  if [ "$packet_size" -gt 65535 ]; then
    echo "The diagnostics IPC packet for PID $pid is too large." >&2
    return 1
  fi

  # A file preserves the packet as stdin when the fallback runs nc in the background.
  if ! packet_file="$(mktemp "${TMPDIR:-/tmp}/capture-hang-dump.XXXXXX")"; then
    echo "Could not create the diagnostics IPC packet for PID $pid." >&2
    return 1
  fi

  if ! {
    printf 'DOTNET_IPC_V1\0'
    u16le "$packet_size"
    byte 1 # Dump command set
    byte 1 # CreateCoreDump command
    u16le 0 # Reserved

    u32le "$path_chars"
    printf '%s' "$dump_path" | iconv -f UTF-8 -t UTF-16LE
    printf '\0\0'

    u32le 1 # Normal/minidump
    u32le 1 # Enable generation diagnostics
  } > "$packet_file"; then
    rm -f "$packet_file"
    echo "Could not create the diagnostics IPC packet for PID $pid." >&2
    return 1
  fi

  if ! response="$(run_with_timeout "$dumpTimeoutSeconds" "$packet_file" nc -U "$socket" | od -An -v -tx1 | tr -d '[:space:]')"; then
    rm -f "$packet_file"
    echo "The diagnostics IPC request for PID $pid failed." >&2
    return 1
  fi
  rm -f "$packet_file"

  if [ "$response" != "$serverOkResponse" ]; then
    echo "The diagnostics IPC request for PID $pid returned an unexpected response: ${response:-<empty>}." >&2
    return 1
  fi
}

# Limit how many processes we dump so that a build hang with many MSBuild nodes
# cannot blow past the cancel-timeout grace window or the artifact size budget.
maxDumps="${HANG_DUMP_MAX:-8}"
dumpTimeoutSeconds="${HANG_DUMP_TIMEOUT_SECONDS:-60}"

wd="${SYSTEM_DEFAULTWORKINGDIRECTORY:-$(pwd -P)}"

# Candidate process names to dump. "dotnet" covers the build host, the MSBuild
# worker nodes, and the in-proc/out-of-proc C# compiler (csc) child processes,
# which all run as `dotnet exec ...`. VBCSCompiler/csc are listed defensively.
candidateNames=("dotnet" "VBCSCompiler" "csc")

for requiredCommand in iconv nc od; do
  if ! command -v "$requiredCommand" >/dev/null 2>&1; then
    __warn "Could not find $requiredCommand on PATH. No hang dumps will be captured."
    exit 0
  fi
done

# Gather candidate pids (excluding this script's own process tree is unnecessary
# because the script itself is bash, not dotnet).
candidatePids=""
for name in "${candidateNames[@]}"; do
  hits="$(pgrep -x "$name" 2>/dev/null || true)"
  candidatePids="$candidatePids $hits"
done
candidatePids="$(printf '%s\n' $candidatePids | sort -un)"

if [ -z "$(printf '%s' "$candidatePids" | tr -d '[:space:]')" ]; then
  echo "No candidate processes (${candidateNames[*]}) are alive; nothing to dump."
  exit 0
fi

# Order by CPU usage descending so the spinning, lock-holding process (the one
# actually hung) is always captured first, even if we hit the dump cap.
ordered="$(for pid in $candidatePids; do
  cpu="$(ps -o %cpu= -p "$pid" 2>/dev/null | tr -d ' ')"
  [ -z "$cpu" ] && cpu=0
  echo "$cpu $pid"
done | sort -rn | awk '{print $2}')"

count=0
for pid in $ordered; do
  if [ "$count" -ge "$maxDumps" ]; then
    __warn "Reached max dump count ($maxDumps); skipping remaining processes."
    break
  fi
  out="$wd/dotnet-${pid}.core"
  echo "Capturing minidump for PID $pid -> $out"
  if capture_dump "$pid" "$out"; then
    count=$((count + 1))
  else
    __warn "Diagnostics IPC minidump request failed for PID $pid."
  fi
done

echo "Done capturing hang dumps ($count captured)."
exit 0
