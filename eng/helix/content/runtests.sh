#!/usr/bin/env bash

helixQueue="$3"
installPlaywright="$7"

RESET="\033[0m"
RED="\033[0;31m"
YELLOW="\033[0;33m"
MAGENTA="\033[0;95m"
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Prevent fallback to global .NET locations. This ensures our tests use the shared frameworks we specify and don't rollforward to something else that might be installed on the machine
export DOTNET_MULTILEVEL_LOOKUP=0
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# Avoid https://github.com/dotnet/aspnetcore/issues/41937 in current session.
unset ASPNETCORE_ENVIRONMENT

export PATH="$PATH:$DIR:$DIR/node/bin"

# Set playwright stuff
export PLAYWRIGHT_BROWSERS_PATH="$DIR/ms-playwright"
if [[ "$helixQueue" == *"OSX"* ]]; then
    PLAYWRIGHT_NODE_PATH=$DIR/.playwright/osx/native/node
else
    export PLAYWRIGHT_DRIVER_PATH="$DIR/.playwright/unix/native/playwright.sh"
    PLAYWRIGHT_NODE_PATH=$DIR/.playwright/unix/native/node
fi

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

echo "Running tests: dotnet $HELIX_CORRELATION_PAYLOAD/HelixTestRunner/HelixTestRunner.dll --target $1 --runtime $2 --queue $helixQueue --arch $4 --quarantined $5 --helixTimeout $6 --playwright $installPlaywright"
dotnet $HELIX_CORRELATION_PAYLOAD/HelixTestRunner/HelixTestRunner.dll --target $1 --runtime $2 --queue $helixQueue --arch $4 --quarantined $5 --helixTimeout $6 --playwright $installPlaywright
exit_code=$?
echo "Finished tests...exit_code=$exit_code"

exit $exit_code
