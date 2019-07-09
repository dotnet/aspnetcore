#!/usr/bin/env bash
set -e
__NDK_Version=r14

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
    echo "By default, this script will generate a file, android_platform, in the root of the ROOTFS_DIR directory that contains the RID for the supported and tested Android build: android.21-arm64. This file is to replace '/etc/os-release', which is not available for Android."
    exit 1
}

__ApiLevel=21 # The minimum platform for arm64 is API level 21
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

__Android_Cross_Dir="$__CrossDir/android-rootfs"
__NDK_Dir="$__Android_Cross_Dir/android-ndk-$__NDK_Version"
__libunwind_Dir="$__Android_Cross_Dir/libunwind"
__lldb_Dir="$__Android_Cross_Dir/lldb"
__ToolchainDir="$__Android_Cross_Dir/toolchain/$__BuildArch"

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

# Create the RootFS for both arm64 as well as aarch
rm -rf $__Android_Cross_Dir/toolchain

echo Generating the $__BuildArch toolchain
$__NDK_Dir/build/tools/make_standalone_toolchain.py --arch $__BuildArch --api $__ApiLevel --install-dir $__ToolchainDir

# Install the required packages into the toolchain
# TODO: Add logic to get latest pkg version instead of specific version number
rm -rf $__Android_Cross_Dir/deb/
rm -rf $__Android_Cross_Dir/tmp

mkdir -p $__Android_Cross_Dir/deb/
mkdir -p $__Android_Cross_Dir/tmp/$arch/
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/libicu_60.2_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/libicu_60.2_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/libicu-dev_60.2_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/libicu-dev_60.2_$__AndroidArch.deb

wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/libandroid-glob-dev_0.4_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/libandroid-glob-dev_0.4_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/libandroid-glob_0.4_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/libandroid-glob_0.4_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/libandroid-support-dev_22_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/libandroid-support-dev_22_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/libandroid-support_22_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/libandroid-support_22_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/liblzma-dev_5.2.3_$__AndroidArch.deb  -O $__Android_Cross_Dir/deb/liblzma-dev_5.2.3_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/liblzma_5.2.3_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/liblzma_5.2.3_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/libunwind-dev_1.2.20170304_$__AndroidArch.deb  -O $__Android_Cross_Dir/deb/libunwind-dev_1.2.20170304_$__AndroidArch.deb
wget -nv -nc http://termux.net/dists/stable/main/binary-$__AndroidArch/libunwind_1.2.20170304_$__AndroidArch.deb -O $__Android_Cross_Dir/deb/libunwind_1.2.20170304_$__AndroidArch.deb

echo Unpacking Termux packages
dpkg -x $__Android_Cross_Dir/deb/libicu_60.2_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/libicu-dev_60.2_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/libandroid-glob-dev_0.4_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/libandroid-glob_0.4_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/libandroid-support-dev_22_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/libandroid-support_22_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/liblzma-dev_5.2.3_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/liblzma_5.2.3_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/libunwind-dev_1.2.20170304_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/
dpkg -x $__Android_Cross_Dir/deb/libunwind_1.2.20170304_$__AndroidArch.deb $__Android_Cross_Dir/tmp/$__AndroidArch/

cp -R $__Android_Cross_Dir/tmp/$__AndroidArch/data/data/com.termux/files/usr/* $__ToolchainDir/sysroot/usr/

# Generate platform file for build.sh script to assign to __DistroRid
echo "Generating platform file..."

echo "RID=android.21-arm64" > $__ToolchainDir/sysroot/android_platform
echo Now run:
echo CONFIG_DIR=\`realpath cross/android/$__BuildArch\` ROOTFS_DIR=\`realpath $__ToolchainDir/sysroot\` ./build.sh cross $__BuildArch skipgenerateversion skipnuget cmakeargs -DENABLE_LLDBPLUGIN=0

