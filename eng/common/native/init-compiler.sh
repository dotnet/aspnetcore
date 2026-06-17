#!/bin/sh
#
# This file detects the C/C++ compiler and exports it to the CC/CXX environment variables
#
# NOTE: some scripts source this file and rely on stdout being empty, make sure
# to not output *anything* here, unless it is an error message that fails the
# build.

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

        # LLVM based on v18 released in early 2024, with two releases per year
        maxVersion="$((18 + ((($(date +%Y) - 2024) * 12 + $(date +%-m) - 3) / 6)))"
        compiler=clang
        ;;

    gcc*|-gcc*|--gcc*)
        # gccx.y or gcc-x.y
        version="$(echo "$compiler" | tr -d '[:alpha:]-=')"
        majorVersion="${version%%.*}"

        # GCC based on v14 released in early 2024, with one release per year
        maxVersion="$((14 + ((($(date +%Y) - 2024) * 12 + $(date +%-m) - 3) / 12)))"
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
    if command -v "$compiler-$1" > /dev/null; then
        desired_version="-$1"
    elif command -v "$compiler$1" > /dev/null; then
        desired_version="$1"
    fi

    echo "$desired_version"
}

__baseOS="$(uname)"
set_compiler_version_from_CC() {
    if [ "$__baseOS" = "Darwin" ]; then
        # On Darwin, the versions from -version/-dumpversion refer to Xcode
        # versions, not llvm versions, so we can't rely on them.
        return
    fi

    version="$("$CC" -dumpversion)"
    if [ -z "$version" ]; then
        echo "Error: $CC -dumpversion didn't provide a version"
        exit 1
    fi

    # gcc and clang often display 3 part versions. However, gcc can show only 1 part in some environments.
    IFS=. read -r majorVersion _ <<EOF
$version
EOF
}

if [ -z "$CLR_CC" ]; then

    # Set default versions
    if [ -z "$majorVersion" ]; then
        minVersion=8
        maxVersion="$((maxVersion + 1))" # +1 for headspace
        i="$maxVersion"
        while [ "$i" -ge $minVersion ]; do
            desired_version="$(check_version_exists "$i")"
            if [ "$desired_version" != "-1" ]; then majorVersion="$i"; break; fi
            i=$((i - 1))
        done

        if [ -z "$majorVersion" ]; then
            if ! command -v "$compiler" > /dev/null; then
                echo "Error: No compatible version of $compiler was found within the range of $minVersion to $maxVersion. Please upgrade your toolchain or specify the compiler explicitly using CLR_CC and CLR_CXX environment variables."
                exit 1
            fi

            CC="$(command -v "$compiler" 2> /dev/null)"
            CXX="$(command -v "$cxxCompiler" 2> /dev/null)"
            set_compiler_version_from_CC
        fi
    else
        desired_version="$(check_version_exists "$majorVersion")"
        if [ "$desired_version" = "-1" ]; then
            echo "Error: Could not find specific version of $compiler: $majorVersion."
            exit 1
        fi
    fi

    if [ -z "$CC" ]; then
        CC="$(command -v "$compiler$desired_version" 2> /dev/null)"
        CXX="$(command -v "$cxxCompiler$desired_version" 2> /dev/null)"
        if [ -z "$CXX" ]; then CXX="$(command -v "$cxxCompiler" 2> /dev/null)"; fi
        set_compiler_version_from_CC
    fi
else
    if [ ! -f "$CLR_CC" ]; then
        echo "Error: CLR_CC is set but path '$CLR_CC' does not exist"
        exit 1
    fi
    CC="$CLR_CC"
    CXX="$CLR_CXX"
    set_compiler_version_from_CC
fi

if [ -z "$CC" ]; then
    echo "Error: Unable to find $compiler."
    exit 1
fi

if [ "$__baseOS" != "Darwin" ]; then
    # On Darwin, we always want to use the Apple linker.

    # Only lld version >= 9 can be considered stable. lld supports s390x starting from 18.0.
    if [ "$compiler" = "clang" ] && [ -n "$majorVersion" ] && [ "$majorVersion" -ge 9 ] && { [ "$build_arch" != "s390x" ] || [ "$majorVersion" -ge 18 ]; }; then
        if "$CC" -fuse-ld=lld -Wl,--version >/dev/null 2>&1; then
            LDFLAGS="-fuse-ld=lld"
        fi
    fi
fi

SCAN_BUILD_COMMAND="$(command -v "scan-build$desired_version" 2> /dev/null)"

export CC CXX LDFLAGS SCAN_BUILD_COMMAND
