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

set -uo pipefail

RESET="\033[0m"
YELLOW="\033[0;33m"

__warn() {
  echo -e "${YELLOW}warning: $*${RESET}"
  if [ -n "${TF_BUILD:-}" ]; then
    echo "##vso[task.logissue type=warning]$*"
  fi
}

# Limit how many processes we dump so that a build hang with many MSBuild nodes
# cannot blow past the cancel-timeout grace window or the artifact size budget.
maxDumps="${HANG_DUMP_MAX:-8}"

wd="${SYSTEM_DEFAULTWORKINGDIRECTORY:-$(pwd -P)}"
repoRoot="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd -P)"

# Candidate process names to dump. "dotnet" covers the build host, the MSBuild
# worker nodes, and the in-proc/out-of-proc C# compiler (csc) child processes,
# which all run as `dotnet exec ...`. VBCSCompiler/csc are listed defensively.
candidateNames=("dotnet" "VBCSCompiler" "csc")

# Locate a createdump that matches the build architecture. createdump ships
# alongside the runtime under the repo-local .dotnet, so it is the correct
# architecture for the (single-architecture) job that produced the hang.
createdump="$(find "$repoRoot/.dotnet" -name createdump -type f 2>/dev/null | sort -V | tail -n 1)"
if [ -z "${createdump:-}" ] && [ -n "${DOTNET_ROOT:-}" ]; then
  createdump="$(find "$DOTNET_ROOT" -name createdump -type f 2>/dev/null | sort -V | tail -n 1)"
fi

if [ -z "${createdump:-}" ]; then
  __warn "Could not find createdump under '$repoRoot/.dotnet'. No hang dumps will be captured."
  exit 0
fi
echo "Using createdump from '$createdump'."

# On macOS, attaching to another process requires elevated privileges.
sudo=""
if [ "$(uname -s)" = "Darwin" ]; then
  sudo="sudo"
fi

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
  echo "Capturing full dump for PID $pid -> $out"
  if $sudo "$createdump" --full --name "$out" "$pid"; then
    # createdump may run under sudo and write a root-owned file; ensure the
    # agent account can read it for artifact upload.
    $sudo chmod 0644 "$out" 2>/dev/null || true
    count=$((count + 1))
  else
    __warn "createdump failed for PID $pid."
  fi
done

echo "Done capturing hang dumps ($count captured)."
exit 0
