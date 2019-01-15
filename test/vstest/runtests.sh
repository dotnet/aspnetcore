#!/usr/bin/env bash
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version $2 --install-dir $HELIX_CORRELATION_PAYLOAD/sdk
if [ $? -ne 0 ]; then 
    curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version $2 --install-dir $HELIX_CORRELATION_PAYLOAD/sdk
fi
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --runtime dotnet --version $3 --install-dir $HELIX_CORRELATION_PAYLOAD/sdk
if [ $? -ne 0 ]; then 
    curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --runtime dotnet --version $3 --install-dir $HELIX_CORRELATION_PAYLOAD/sdk
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

$HELIX_CORRELATION_PAYLOAD/sdk/dotnet vstest $1 --logger:trx
