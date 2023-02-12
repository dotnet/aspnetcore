#!/bin/sh
#
# This file detects the C/C++ compiler and exports it to the CC/CXX environment variables
#
# NOTE: some scripts source this file and rely on stdout being empty, make sure to not output anything here!

if [ -z "$build_arch" ] || [ -z "$compiler" ]; then
  echo "Usage..."
  echo "build_arch=<ARCH> compiler=<NAME> init-compiler.sh"
  echo "Specify the target architecture."
  echo "Specify the name of compiler (clang or gcc)."
  exit 1
fi

case "$compiler" in
    clang*|-clang*|--clang*)
        # clangx.y or clang-x.y
        version="$(echo "$compiler" | tr -d '[:alpha:]-=')"
        majorVersion="${version%%.*}"
        [ -z "${version##*.*}" ] && minorVersion="${version#*.}"

        if [ -z "$minorVersion" ] && [ -n "$majorVersion" ] && [ "$majorVersion" -le 6 ]; then
            minorVersion=0;
        fi
        compiler=clang
        ;;

    gcc*|-gcc*|--gcc*)
        # gccx.y or gcc-x.y
        version="$(echo "$compiler" | tr -d '[:alpha:]-=')"
        majorVersion="${version%%.*}"
        [ -z "${version##*.*}" ] && minorVersion="${version#*.}"
        compiler=gcc
        ;;
esac

cxxCompiler="$compiler++"

# clear the existing CC and CXX from environment
CC=
CXX=
LDFLAGS=

if [ "$compiler" = "gcc" ]; then cxxCompiler="g++"; fi

check_version_exists() {
    desired_version=-1

    # Set up the environment to be used for building with the desired compiler.
    if command -v "$compiler-$1.$2" > /dev/null; then
        desired_version="-$1.$2"
    elif command -v "$compiler$1$2" > /dev/null; then
        desired_version="$1$2"
    elif command -v "$compiler-$1$2" > /dev/null; then
        desired_version="-$1$2"
    fi

    echo "$desired_version"
}

if [ -z "$CLR_CC" ]; then

    # Set default versions
    if [ -z "$majorVersion" ]; then
        # note: gcc (all versions) and clang versions higher than 6 do not have minor version in file name, if it is zero.
        if [ "$compiler" = "clang" ]; then versions="16 15 14 13 12 11 10 9 8 7 6.0 5.0 4.0 3.9 3.8 3.7 3.6 3.5"
        elif [ "$compiler" = "gcc" ]; then versions="12 11 10 9 8 7 6 5 4.9"; fi

        for version in $versions; do
            _major="${version%%.*}"
            [ -z "${version##*.*}" ] && _minor="${version#*.}"
            desired_version="$(check_version_exists "$_major" "$_minor")"
            if [ "$desired_version" != "-1" ]; then majorVersion="$_major"; break; fi
        done

        if [ -z "$majorVersion" ]; then
            if command -v "$compiler" > /dev/null; then
                if [ "$(uname)" != "Darwin" ]; then
                    echo "Warning: Specific version of $compiler not found, falling back to use the one in PATH."
                fi
                CC="$(command -v "$compiler")"
                CXX="$(command -v "$cxxCompiler")"
            else
                echo "No usable version of $compiler found."
                exit 1
            fi
        else
            if [ "$compiler" = "clang" ] && [ "$majorVersion" -lt 5 ]; then
                if [ "$build_arch" = "arm" ] || [ "$build_arch" = "armel" ]; then
                    if command -v "$compiler" > /dev/null; then
                        echo "Warning: Found clang version $majorVersion which is not supported on arm/armel architectures, falling back to use clang from PATH."
                        CC="$(command -v "$compiler")"
                        CXX="$(command -v "$cxxCompiler")"
                    else
                        echo "Found clang version $majorVersion which is not supported on arm/armel architectures, and there is no clang in PATH."
                        exit 1
                    fi
                fi
            fi
        fi
    else
        desired_version="$(check_version_exists "$majorVersion" "$minorVersion")"
        if [ "$desired_version" = "-1" ]; then
            echo "Could not find specific version of $compiler: $majorVersion $minorVersion."
            exit 1
        fi
    fi

    if [ -z "$CC" ]; then
        CC="$(command -v "$compiler$desired_version")"
        CXX="$(command -v "$cxxCompiler$desired_version")"
        if [ -z "$CXX" ]; then CXX="$(command -v "$cxxCompiler")"; fi
    fi
else
    if [ ! -f "$CLR_CC" ]; then
        echo "CLR_CC is set but path '$CLR_CC' does not exist"
        exit 1
    fi
    CC="$CLR_CC"
    CXX="$CLR_CXX"
fi

if [ -z "$CC" ]; then
    echo "Unable to find $compiler."
    exit 1
fi

# Only lld version >= 9 can be considered stable. lld doesn't support s390x.
if [ "$compiler" = "clang" ] && [ -n "$majorVersion" ] && [ "$majorVersion" -ge 9 ] && [ "$build_arch" != "s390x" ]; then
    if "$CC" -fuse-ld=lld -Wl,--version >/dev/null 2>&1; then
        LDFLAGS="-fuse-ld=lld"
    fi
fi

SCAN_BUILD_COMMAND="$(command -v "scan-build$desired_version")"

export CC CXX LDFLAGS SCAN_BUILD_COMMAND
