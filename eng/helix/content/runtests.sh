#!/usr/bin/env bash

test_binary_path="$1"
dotnet_sdk_version="$2"
dotnet_runtime_version="$3"
helix_queue_name="$4"

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

# Used by SkipOnHelix attribute
export helix="$helix_queue_name"


RESET="\033[0m"
RED="\033[0;31m"
YELLOW="\033[0;33m"
MAGENTA="\033[0;95m"

curl -o dotnet-install.sh -sSL https://dot.net/v1/dotnet-install.sh
if [ $? -ne 0 ]; then
    download_retries=3
    while [ $download_retries -gt 0 ]; do
        curl -o dotnet-install.sh -sSL https://dot.net/v1/dotnet-install.sh
        if [ $? -ne 0 ]; then
            let download_retries=download_retries-1
            echo -e "${YELLOW}Failed to download dotnet-install.sh. Retries left: $download_retries.${RESET}"
        else
            download_retries=0
        fi
    done
fi

# Call "sync" between "chmod" and execution to prevent "text file busy" error in Docker (aufs)
chmod +x "dotnet-install.sh"; sync

./dotnet-install.sh --version $dotnet_sdk_version --install-dir "$DOTNET_ROOT"
if [ $? -ne 0 ]; then
    sdk_retries=3
    while [ $sdk_retries -gt 0 ]; do
        ./dotnet-install.sh --version $dotnet_sdk_version --install-dir "$DOTNET_ROOT"
        if [ $? -ne 0 ]; then
            let sdk_retries=sdk_retries-1
            echo -e "${YELLOW}Failed to install .NET Core SDK $version. Retries left: $sdk_retries.${RESET}"
        else
            sdk_retries=0
        fi
    done
fi

./dotnet-install.sh --runtime dotnet --version $dotnet_runtime_version --install-dir "$DOTNET_ROOT"
if [ $? -ne 0 ]; then
    runtime_retries=3
    while [ $runtime_retries -gt 0 ]; do
        ./dotnet-install.sh --runtime dotnet --version $dotnet_runtime_version --install-dir "$DOTNET_ROOT"
        if [ $? -ne 0 ]; then
            let runtime_retries=runtime_retries-1
            echo -e "${YELLOW}Failed to install .NET Core runtime $version. Retries left: $runtime_retries.${RESET}"
        else
            runtime_retries=0
        fi
    done
fi

$DOTNET_ROOT/dotnet vstest $test_binary_path -lt >discovered.txt
if grep -q "Exception thrown" discovered.txt; then
    echo -e "${RED}Exception thrown during test discovery${RESET}".
    cat discovered.txt
    exit 1
fi

# Run non-flaky tests first
# We need to specify all possible Flaky filters that apply to this environment, because the flaky attribute
# only puts the explicit filter traits the user provided in the flaky attribute
# Filter syntax: https://github.com/Microsoft/vstest-docs/blob/master/docs/filter.md
NONFLAKY_FILTER="Flaky:All!=true&Flaky:Helix:All!=true&Flaky:Helix:Queue:All!=true&Flaky:Helix:Queue:$helix_queue_name!=true"
echo "Running non-flaky tests."
$DOTNET_ROOT/dotnet vstest $test_binary_path --logger:trx --TestCaseFilter:"$NONFLAKY_FILTER"
nonflaky_exitcode=$?
if [ $nonflaky_exitcode != 0 ]; then
    echo "Non-flaky tests failed!" 1>&2
    # DO NOT EXIT
fi

FLAKY_FILTER="Flaky:All=true|Flaky:Helix:All=true|Flaky:Helix:Queue:All=true|Flaky:Helix:Queue:$helix_queue_name=true"
echo "Running known-flaky tests."
$DOTNET_ROOT/dotnet vstest $test_binary_path --TestCaseFilter:"$FLAKY_FILTER"
if [ $? != 0 ]; then
    echo "Flaky tests failed!" 1>&2
    # DO NOT EXIT
fi

exit $nonflaky_exitcode
