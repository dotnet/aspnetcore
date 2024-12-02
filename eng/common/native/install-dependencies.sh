#!/bin/sh

set -e

# This is a simple script primarily used for CI to install necessary dependencies
#
# Usage:
#
# ./install-dependencies.sh <OS>

os="$(echo "$1" | tr "[:upper:]" "[:lower:]")"

if [ -z "$os" ]; then
    . "$(dirname "$0")"/init-os-and-arch.sh
fi

case "$os" in
    linux)
        if [ -e /etc/os-release ]; then
            . /etc/os-release
        fi

        if [ "$ID" = "debian" ] || [ "$ID_LIKE" = "debian" ]; then
            apt update

            apt install -y build-essential gettext locales cmake llvm clang lld lldb liblldb-dev libunwind8-dev libicu-dev liblttng-ust-dev \
                libssl-dev libkrb5-dev zlib1g-dev pigz cpio

            localedef -i en_US -c -f UTF-8 -A /usr/share/locale/locale.alias en_US.UTF-8
        elif [ "$ID" = "fedora" ]; then
            dnf install -y cmake llvm lld lldb clang python curl libicu-devel openssl-devel krb5-devel zlib-devel lttng-ust-devel pigz cpio
        elif [ "$ID" = "alpine" ]; then
            apk add build-base cmake bash curl clang llvm-dev lld lldb krb5-dev lttng-ust-dev icu-dev zlib-dev openssl-dev pigz cpio
        else
            echo "Unsupported distro. distro: $ID"
            exit 1
        fi
        ;;

    osx|maccatalyst|ios|iossimulator|tvos|tvossimulator)
        echo "Installed xcode version: $(xcode-select -p)"

        export HOMEBREW_NO_INSTALL_CLEANUP=1
        export HOMEBREW_NO_INSTALLED_DEPENDENTS_CHECK=1
        # Skip brew update for now, see https://github.com/actions/setup-python/issues/577
        # brew update --preinstall

        # Temporarily uninstall pkg-config@0.29.2 to work around https://github.com/actions/runner-images/issues/10984
        brew uninstall --ignore-dependencies --force pkg-config@0.29.2

        brew bundle --no-upgrade --no-lock --file=- <<EOF
brew "cmake"
brew "icu4c"
brew "openssl@3"
brew "pkg-config"
brew "python3"
brew "pigz"
EOF
        ;;

    *)
        echo "Unsupported platform. OS: $os"
        exit 1
        ;;
esac
