#!/usr/bin/env bash

# Use uname to determine what the OS is.
OSName=$(uname -s | tr '[:upper:]' '[:lower:]')

if command -v getprop && getprop ro.product.system.model 2>&1 | grep -qi android; then
    OSName="android"
fi

case "$OSName" in
freebsd|linux|netbsd|openbsd|sunos|android|haiku)
    os="$OSName" ;;
darwin)
    os=osx ;;
*)
    echo "Unsupported OS $OSName detected!"
    exit 1 ;;
esac

# On Solaris, `uname -m` is discouraged, see https://docs.oracle.com/cd/E36784_01/html/E36870/uname-1.html
# and `uname -p` returns processor type (e.g. i386 on amd64).
# The appropriate tool to determine CPU is isainfo(1) https://docs.oracle.com/cd/E36784_01/html/E36870/isainfo-1.html.
if [ "$os" = "sunos" ]; then
    if uname -o 2>&1 | grep -q illumos; then
        os="illumos"
    else
        os="solaris"
    fi
    CPUName=$(isainfo -n)
else
    # For the rest of the operating systems, use uname(1) to determine what the CPU is.
    CPUName=$(uname -m)
fi

case "$CPUName" in
    arm64|aarch64)
        arch=arm64
        ;;

    loongarch64)
        arch=loongarch64
        ;;

    riscv64)
        arch=riscv64
        ;;

    amd64|x86_64)
        arch=x64
        ;;

    armv7l|armv8l)
        if (NAME=""; . /etc/os-release; test "$NAME" = "Tizen"); then
            arch=armel
        else
            arch=arm
        fi
        ;;

    armv6l)
        arch=armv6
        ;;

    i[3-6]86)
        echo "Unsupported CPU $CPUName detected, build might not succeed!"
        arch=x86
        ;;

    s390x)
        arch=s390x
        ;;

    ppc64le)
        arch=ppc64le
        ;;
    *)
        echo "Unknown CPU $CPUName detected!"
        exit 1
        ;;
esac
