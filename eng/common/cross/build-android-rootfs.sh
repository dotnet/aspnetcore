#!/usr/bin/env bash
set -e
__NDK_Version=r21

usage()
{
    echo "Creates a toolchain and sysroot used for cross-compiling for Android."
    echo.
    echo "Usage: $0 [BuildArch] [ApiLevel]"
    echo.
    echo "BuildArch is the target architecture of Android. Currently only arm64 is supported."
    echo "ApiLevel is the target Android API level. API levels usually match to Android releases. See https://source.android.com/source/build-numbers.html"
    echo.
    echo "By default, the toolchain and sysroot will be generated in cross/android-rootfs/toolchain/[BuildArch]. You can change this behavior"
    echo "by setting the TOOLCHAIN_DIR environment variable"
    echo.
    echo "By default, the NDK will be downloaded into the cross/android-rootfs/android-ndk-$__NDK_Version directory. If you already have an NDK installation,"
    echo "you can set the NDK_DIR environment variable to have this script use that installation of the NDK."
    echo "By default, this script will generate a file, android_platform, in the root of the ROOTFS_DIR directory that contains the RID for the supported and tested Android build: android.28-arm64. This file is to replace '/etc/os-release', which is not available for Android."
    exit 1
}

__ApiLevel=28 # The minimum platform for arm64 is API level 21 but the minimum version that support glob(3) is 28. See $ANDROID_NDK/toolchains/llvm/prebuilt/linux-x86_64/sysroot/usr/include/glob.h
__BuildArch=arm64
__AndroidArch=aarch64
__AndroidToolchain=aarch64-linux-android

for i in "$@"
    do
        lowerI="$(echo $i | awk '{print tolower($0)}')"
        case $lowerI in
        -?|-h|--help)
            usage
            exit 1
            ;;
        arm64)
            __BuildArch=arm64
            __AndroidArch=aarch64
            __AndroidToolchain=aarch64-linux-android
            ;;
        arm)
            __BuildArch=arm
            __AndroidArch=arm
            __AndroidToolchain=arm-linux-androideabi
            ;;
        *[0-9])
            __ApiLevel=$i
            ;;
        *)
            __UnprocessedBuildArgs="$__UnprocessedBuildArgs $i"
            ;;
    esac
done

# Obtain the location of the bash script to figure out where the root of the repo is.
__CrossDir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
__Android_Cross_Dir="$(cd "$__CrossDir/../../../.tools/android-rootfs" && pwd)"

__NDK_Dir="$__Android_Cross_Dir/android-ndk-$__NDK_Version"
__lldb_Dir="$__Android_Cross_Dir/lldb"
__ToolchainDir="$__Android_Cross_Dir/android-ndk-$__NDK_Version"

if [[ -n "$TOOLCHAIN_DIR" ]]; then
    __ToolchainDir=$TOOLCHAIN_DIR
fi

if [[ -n "$NDK_DIR" ]]; then
    __NDK_Dir=$NDK_DIR
fi

echo "Target API level: $__ApiLevel"
echo "Target architecture: $__BuildArch"
echo "NDK location: $__NDK_Dir"
echo "Target Toolchain location: $__ToolchainDir"

# Download the NDK if required
if [ ! -d $__NDK_Dir ]; then
    echo Downloading the NDK into $__NDK_Dir
    mkdir -p $__NDK_Dir
    wget -nv -nc --show-progress https://dl.google.com/android/repository/android-ndk-$__NDK_Version-linux-x86_64.zip -O $__Android_Cross_Dir/android-ndk-$__NDK_Version-linux-x86_64.zip
    unzip -q $__Android_Cross_Dir/android-ndk-$__NDK_Version-linux-x86_64.zip -d $__Android_Cross_Dir
fi

if [ ! -d $__lldb_Dir ]; then
    mkdir -p $__lldb_Dir
    echo Downloading LLDB into $__lldb_Dir
    wget -nv -nc --show-progress https://dl.google.com/android/repository/lldb-2.3.3614996-linux-x86_64.zip -O $__Android_Cross_Dir/lldb-2.3.3614996-linux-x86_64.zip
    unzip -q $__Android_Cross_Dir/lldb-2.3.3614996-linux-x86_64.zip -d $__lldb_Dir
fi

echo "Download dependencies..."
mkdir -p $__Android_Cross_Dir/tmp/$arch/

# combined dependencies for coreclr, installer and libraries
__AndroidPackages="libicu"
__AndroidPackages+=" libandroid-glob"
__AndroidPackages+=" liblzma"
__AndroidPackages+=" krb5"
__AndroidPackages+=" openssl"

for path in $(wget -qO- http://termux.net/dists/stable/main/binary-$__AndroidArch/Packages |\
    grep -A15 "Package: \(${__AndroidPackages// /\\|}\)" | grep -v "static\|tool" | grep Filename); do

    if [[ "$path" != "Filename:" ]]; then
        echo "Working on: $path"
        wget -qO- http://termux.net/$path | dpkg -x - $__Android_Cross_Dir/tmp/$__AndroidArch/
    fi
done

cp -R $__Android_Cross_Dir/tmp/$__AndroidArch/data/data/com.termux/files/usr/* $__ToolchainDir/sysroot/usr/

# Generate platform file for build.sh script to assign to __DistroRid
echo "Generating platform file..."
echo "RID=android.${__ApiLevel}-${__BuildArch}" > $__ToolchainDir/sysroot/android_platform

echo "Now to build coreclr, libraries and installers; run:"
echo ROOTFS_DIR=\$\(realpath $__ToolchainDir/sysroot\) ./build.sh --cross --arch $__BuildArch \
    --subsetCategory coreclr \
    --subsetCategory libraries \
    --subsetCategory installer
