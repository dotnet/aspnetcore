#!/usr/bin/env bash

dotnet_sdk_version="$2"
dotnet_runtime_version="$3"
helixQueue="$5"
installPlaywright="${10}"

RESET="\033[0m"
RED="\033[0;31m"
YELLOW="\033[0;33m"
MAGENTA="\033[0;95m"
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

export PATH="$PATH:$DIR/node/bin"

# Set playwright stuff
export PLAYWRIGHT_BROWSERS_PATH="$DIR/ms-playwright"
if [[ "$helixQueue" == *"OSX"* ]]; then
    export PLAYWRIGHT_DRIVER_PATH="$DIR/.playwright/osx/native/playwright.sh"
    PLAYWRIGHT_NODE_PATH=$DIR/.playwright/osx/native/node
else
    export PLAYWRIGHT_DRIVER_PATH="$DIR/.playwright/unix/native/playwright.sh"
    PLAYWRIGHT_NODE_PATH=$DIR/.playwright/unix/native/node
fi
export InstallPlaywright="$installPlaywright"
if [ -f "$PLAYWRIGHT_DRIVER_PATH" ]; then
    if [[ "$helixQueue" != *"OSX"* ]]; then
        echo "Installing Playwright requirements..."
        sudo apt-get install -y libdbus-glib-1-2
        sudo apt-get install -y libbrotli1
        sudo apt-get install -y libegl1
        sudo apt-get install -y libnotify4
        sudo apt-get install -y libvpx5
        sudo apt-get install -y libopus0
        sudo apt-get install -y libwoff1
        sudo apt-get install -y libgstreamer-plugins-base1.0-0
        sudo apt-get install -y libgstreamer1.0-0
        sudo apt-get install -y libgstreamer-gl1.0-0
        sudo apt-get install -y libgstreamer-plugins-bad1.0-0
        sudo apt-get install -y libopenjp2-7
        sudo apt-get install -y libwebpdemux2
        sudo apt-get install -y libwebp6
        sudo apt-get install -y libenchant1c2a
        sudo apt-get install -y libsecret-1-0
        sudo apt-get install -y libhyphen0
        sudo apt-get install -y libgles2
        sudo apt-get install -y gstreamer1.0-libav
        sudo apt-get install -y libxkbcommon0
        sudo apt-get install -y libgtk-3-0
        sudo apt-get install -y libharfbuzz-icu0
    fi
    echo "chmod +x $PLAYWRIGHT_DRIVER_PATH"
    chmod +x $PLAYWRIGHT_DRIVER_PATH
    echo "chmod +x $PLAYWRIGHT_NODE_PATH"
    chmod +x $PLAYWRIGHT_NODE_PATH
fi

RESET="\033[0m"
RED="\033[0;31m"
YELLOW="\033[0;33m"
MAGENTA="\033[0;95m"

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

echo "Restore: dotnet restore RunTests/RunTests.csproj --ignore-failed-sources"
dotnet restore RunTests/RunTests.csproj --ignore-failed-sources

echo "Running tests: dotnet run --no-restore --project RunTests/RunTests.csproj -- --target $1 --runtime $4 --queue $helixQueue --arch $6 --quarantined $7 --ef $8 --helixTimeout $9"
dotnet run --no-restore --project RunTests/RunTests.csproj -- --target $1 --runtime $4 --queue $helixQueue --arch $6 --quarantined $7 --ef $8 --helixTimeout $9
exit_code=$?
echo "Finished tests...exit_code=$exit_code"

exit $exit_code
