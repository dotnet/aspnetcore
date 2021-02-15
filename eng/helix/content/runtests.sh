#!/usr/bin/env bash

dotnet_sdk_version="$2"
dotnet_runtime_version="$3"

RESET="\033[0m"
RED="\033[0;31m"
YELLOW="\033[0;33m"
MAGENTA="\033[0;95m"
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Ensures every invocation of dotnet apps uses the same dotnet.exe
# Add $random to path to ensure tests don't expect dotnet to be in a particular path
export DOTNET_ROOT="$DIR/.dotnet$RANDOM"

# Ensure dotnet comes first on PATH
export PATH="$DOTNET_ROOT:$PATH:$DIR/node/bin"

# Prevent fallback to global .NET locations. This ensures our tests use the shared frameworks we specify and don't rollforward to something else that might be installed on the machine
export DOTNET_MULTILEVEL_LOOKUP=0

# Avoid contaminating userprofiles
# Add $random to path to ensure tests don't expect home to be in a particular path
export DOTNET_CLI_HOME="$DIR/.home$RANDOM"

export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

RESET="\033[0m"
RED="\033[0;31m"
YELLOW="\033[0;33m"
MAGENTA="\033[0;95m"

. eng/common/tools.sh

if [[ -z "${10:-}" ]]; then
    echo "InstallDotNet $DOTNET_ROOT $dotnet_sdk_version '' '' true"
    InstallDotNet $DOTNET_ROOT $dotnet_sdk_version "" "" true || {
      exit_code=$?
      Write-PipelineTelemetryError -Category 'InitializeToolset' -Message "dotnet-install.sh failed (exit code '$exit_code')." >&2
      ExitWithExitCode $exit_code
    }
    echo

    echo "InstallDotNet $DOTNET_ROOT $dotnet_runtime_version '' dotnet true"
    InstallDotNet $DOTNET_ROOT $dotnet_runtime_version "" dotnet true || {
      exit_code=$?
      Write-PipelineTelemetryError -Category 'InitializeToolset' -Message "dotnet-install.sh failed (exit code '$exit_code')." >&2
      ExitWithExitCode $exit_code
    }
else
    echo "InstallDotNet $DOTNET_ROOT $dotnet_sdk_version '' '' true https://dotnetclimsrc.blob.core.windows.net/dotnet ..."
    InstallDotNet $DOTNET_ROOT $dotnet_sdk_version "" "" true https://dotnetclimsrc.blob.core.windows.net/dotnet ${11} || {
      exit_code=$?
      Write-PipelineTelemetryError -Category 'InitializeToolset' -Message "dotnet-install.sh failed (exit code '$exit_code')." >&2
      ExitWithExitCode $exit_code
    }
    echo

    echo "InstallDotNet $DOTNET_ROOT $dotnet_runtime_version '' dotnet true https://dotnetclimsrc.blob.core.windows.net/dotnet ..."
    InstallDotNet $DOTNET_ROOT $dotnet_runtime_version "" dotnet true https://dotnetclimsrc.blob.core.windows.net/dotnet ${11} || {
      exit_code=$?
      Write-PipelineTelemetryError -Category 'InitializeToolset' -Message "dotnet-install.sh failed (exit code '$exit_code')." >&2
      ExitWithExitCode $exit_code
    }
fi
echo

if [ -e /proc/self/coredump_filter ]; then
  # Include memory in private and shared file-backed mappings in the dump.
  # This ensures that we can see disassembly from our shared libraries when
  # inspecting the contents of the dump. See 'man core' for details.
  echo -n 0x3F > /proc/self/coredump_filter
fi

# dotnet-install.sh seems to affect the Linux filesystem and causes test flakiness unless we sync the filesystem before running tests
sync

exit_code=0

echo "Restore: $DOTNET_ROOT/dotnet restore RunTests/RunTests.csproj --ignore-failed-sources"
$DOTNET_ROOT/dotnet restore RunTests/RunTests.csproj --ignore-failed-sources

echo "Running tests: $DOTNET_ROOT/dotnet run --no-restore --project RunTests/RunTests.csproj -- --target $1 --runtime $4 --queue $5 --arch $6 --quarantined $7 --ef $8 --helixTimeout $9"
$DOTNET_ROOT/dotnet run --no-restore --project RunTests/RunTests.csproj -- --target $1 --runtime $4 --queue $5 --arch $6 --quarantined $7 --ef $8 --helixTimeout $9 --playwright $10
exit_code=$?
echo "Finished tests...exit_code=$exit_code"

# dotnet-install.sh leaves the temporary SDK archive on the helix machine which slowly fills the disk, we'll be nice and clean it until the script fixes the issue
rm -r -f ${TMPDIR:-/tmp}/dotnet.*

exit $exit_code
