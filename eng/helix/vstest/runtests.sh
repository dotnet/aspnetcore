#!/usr/bin/env bash
curl -o dotnet-install.sh -sSL https://dot.net/v1/dotnet-install.sh
if [ $? -ne 0 ]; then
    download_retries=3
    while [ $download_retries -gt 0 ]; do
        curl -sSL https://dot.net/v1/dotnet-install.sh
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

./dotnet-install.sh --version $2 --install-dir $HELIX_CORRELATION_PAYLOAD/sdk
if [ $? -ne 0 ]; then
    sdk_retries=3
    while [ $sdk_retries -gt 0 ]; do
        ./dotnet-install.sh --version $2 --install-dir $HELIX_CORRELATION_PAYLOAD/sdk
        if [ $? -ne 0 ]; then
            let sdk_retries=sdk_retries-1
            echo -e "${YELLOW}Failed to install .NET Core SDK $version. Retries left: $sdk_retries.${RESET}"
        else
            sdk_retries=0
        fi
    done
fi

./dotnet-install.sh --runtime dotnet --version $3 --install-dir $HELIX_CORRELATION_PAYLOAD/sdk
if [ $? -ne 0 ]; then
    runtime_retries=3
    while [ $runtime_retries -gt 0 ]; do
        ./dotnet-install.sh --runtime dotnet --version $3 --install-dir $HELIX_CORRELATION_PAYLOAD/sdk
        if [ $? -ne 0 ]; then
            let runtime_retries=runtime_retries-1
            echo -e "${YELLOW}Failed to install .NET Core runtime $version. Retries left: $runtime_retries.${RESET}"
        else
            runtime_retries=0
        fi
    done
fi

export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# Ensures every invocation of dotnet apps uses the same dotnet.exe
export DOTNET_ROOT="$HELIX_CORRELATION_PAYLOAD/sdk"

# Ensure dotnet comes first on PATH
export PATH="$DOTNET_ROOT:$PATH"

# Prevent fallback to global .NET locations. This ensures our tests use the shared frameworks we specify and don't rollforward to something else that might be installed on the machine
export DOTNET_MULTILEVEL_LOOKUP=0

# Avoid contaminating userprofiles
export DOTNET_CLI_HOME="$HELIX_CORRELATION_PAYLOAD/home"

export helix="true"

$HELIX_CORRELATION_PAYLOAD/sdk/dotnet vstest $1 --logger:trx
