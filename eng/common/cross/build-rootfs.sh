#!/usr/bin/env bash

usage()
{
    echo "Usage: $0 [BuildArch] [LinuxCodeName] [lldbx.y] [--skipunmount]"
    echo "BuildArch can be: arm(default), armel, arm64, x86"
    echo "LinuxCodeName - optional, Code name for Linux, can be: trusty(default), vivid, wily, xenial, zesty, bionic, alpine. If BuildArch is armel, LinuxCodeName is jessie(default) or tizen."
    echo "lldbx.y - optional, LLDB version, can be: lldb3.6(default), lldb3.8, lldb3.9, lldb4.0, no-lldb. Ignored for alpine"
    echo "--skipunmount - optional, will skip the unmount of rootfs folder."
    exit 1
}

__LinuxCodeName=trusty
__CrossDir=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )
__InitialDir=$PWD
__BuildArch=arm
__UbuntuArch=armhf
__UbuntuRepo="http://ports.ubuntu.com/"
__LLDB_Package="lldb-3.6-dev"
__SkipUnmount=0

# base development support
__UbuntuPackages="build-essential"

__AlpinePackages="alpine-base"
__AlpinePackages+=" build-base"
__AlpinePackages+=" linux-headers"
__AlpinePackages+=" lldb-dev"
__AlpinePackages+=" llvm-dev"

# symlinks fixer
__UbuntuPackages+=" symlinks"

# CoreCLR and CoreFX dependencies
__UbuntuPackages+=" libicu-dev"
__UbuntuPackages+=" liblttng-ust-dev"
__UbuntuPackages+=" libunwind8-dev"

__AlpinePackages+=" gettext-dev"
__AlpinePackages+=" icu-dev"
__AlpinePackages+=" libunwind-dev"
__AlpinePackages+=" lttng-ust-dev"

# CoreFX dependencies
__UbuntuPackages+=" libcurl4-openssl-dev"
__UbuntuPackages+=" libkrb5-dev"
__UbuntuPackages+=" libssl-dev"
__UbuntuPackages+=" zlib1g-dev"

__AlpinePackages+=" curl-dev"
__AlpinePackages+=" krb5-dev"
__AlpinePackages+=" openssl-dev"
__AlpinePackages+=" zlib-dev"

__UnprocessedBuildArgs=
for i in "$@" ; do
    lowerI="$(echo $i | awk '{print tolower($0)}')"
    case $lowerI in
        -?|-h|--help)
            usage
            exit 1
            ;;
        arm)
            __BuildArch=arm
            __UbuntuArch=armhf
            __AlpineArch=armhf
            __QEMUArch=arm
            ;;
        arm64)
            __BuildArch=arm64
            __UbuntuArch=arm64
            __AlpineArch=aarch64
            __QEMUArch=aarch64
            ;;
        armel)
            __BuildArch=armel
            __UbuntuArch=armel
            __UbuntuRepo="http://ftp.debian.org/debian/"
            __LinuxCodeName=jessie
            ;;
        x86)
            __BuildArch=x86
            __UbuntuArch=i386
            __UbuntuRepo="http://archive.ubuntu.com/ubuntu/"
            ;;
        lldb3.6)
            __LLDB_Package="lldb-3.6-dev"
            ;;
        lldb3.8)
            __LLDB_Package="lldb-3.8-dev"
            ;;
        lldb3.9)
            __LLDB_Package="liblldb-3.9-dev"
            ;;
        lldb4.0)
            __LLDB_Package="liblldb-4.0-dev"
            ;;
        no-lldb)
            unset __LLDB_Package
            ;;
        vivid)
            if [ "$__LinuxCodeName" != "jessie" ]; then
                __LinuxCodeName=vivid
            fi
            ;;
        wily)
            if [ "$__LinuxCodeName" != "jessie" ]; then
                __LinuxCodeName=wily
            fi
            ;;
        xenial)
            if [ "$__LinuxCodeName" != "jessie" ]; then
                __LinuxCodeName=xenial
            fi
            ;;
        zesty)
            if [ "$__LinuxCodeName" != "jessie" ]; then
                __LinuxCodeName=zesty
            fi
            ;;
        bionic)
            if [ "$__LinuxCodeName" != "jessie" ]; then
                __LinuxCodeName=bionic
            fi
            ;;
        jessie)
            __LinuxCodeName=jessie
            __UbuntuRepo="http://ftp.debian.org/debian/"
            ;;
        tizen)
            if [ "$__BuildArch" != "armel" ]; then
                echo "Tizen is available only for armel."
                usage;
                exit 1;
            fi
            __LinuxCodeName=
            __UbuntuRepo=
            __Tizen=tizen
            ;;
        alpine)
            __LinuxCodeName=alpine
            __UbuntuRepo=
            ;;
        --skipunmount)
            __SkipUnmount=1
            ;;
        *)
            __UnprocessedBuildArgs="$__UnprocessedBuildArgs $i"
            ;;
    esac
done

if [ "$__BuildArch" == "armel" ]; then
    __LLDB_Package="lldb-3.5-dev"
fi
__UbuntuPackages+=" ${__LLDB_Package:-}"

__RootfsDir="$__CrossDir/rootfs/$__BuildArch"

if [[ -n "$ROOTFS_DIR" ]]; then
    __RootfsDir=$ROOTFS_DIR
fi

if [ -d "$__RootfsDir" ]; then
    if [ $__SkipUnmount == 0 ]; then
        umount $__RootfsDir/*
    fi
    rm -rf $__RootfsDir
fi

if [[ "$__LinuxCodeName" == "alpine" ]]; then
    __ApkToolsVersion=2.9.1
    __AlpineVersion=3.7
    __ApkToolsDir=$(mktemp -d)
    wget https://github.com/alpinelinux/apk-tools/releases/download/v$__ApkToolsVersion/apk-tools-$__ApkToolsVersion-x86_64-linux.tar.gz -P $__ApkToolsDir
    tar -xf $__ApkToolsDir/apk-tools-$__ApkToolsVersion-x86_64-linux.tar.gz -C $__ApkToolsDir
    mkdir -p $__RootfsDir/usr/bin
    cp -v /usr/bin/qemu-$__QEMUArch-static $__RootfsDir/usr/bin
    $__ApkToolsDir/apk-tools-$__ApkToolsVersion/apk \
      -X http://dl-cdn.alpinelinux.org/alpine/v$__AlpineVersion/main \
      -X http://dl-cdn.alpinelinux.org/alpine/v$__AlpineVersion/community \
      -X http://dl-cdn.alpinelinux.org/alpine/edge/testing \
      -U --allow-untrusted --root $__RootfsDir --arch $__AlpineArch --initdb \
      add $__AlpinePackages
    rm -r $__ApkToolsDir
elif [[ -n $__LinuxCodeName ]]; then
    qemu-debootstrap --arch $__UbuntuArch $__LinuxCodeName $__RootfsDir $__UbuntuRepo
    cp $__CrossDir/$__BuildArch/sources.list.$__LinuxCodeName $__RootfsDir/etc/apt/sources.list
    chroot $__RootfsDir apt-get update
    chroot $__RootfsDir apt-get -f -y install
    chroot $__RootfsDir apt-get -y install $__UbuntuPackages
    chroot $__RootfsDir symlinks -cr /usr

    if [ $__SkipUnmount == 0 ]; then
        umount $__RootfsDir/*
    fi

    if [[ "$__BuildArch" == "arm" && "$__LinuxCodeName" == "trusty" ]]; then
        pushd $__RootfsDir
        patch -p1 < $__CrossDir/$__BuildArch/trusty.patch
        patch -p1 < $__CrossDir/$__BuildArch/trusty-lttng-2.4.patch
        popd
    fi
elif [ "$__Tizen" == "tizen" ]; then
    ROOTFS_DIR=$__RootfsDir $__CrossDir/$__BuildArch/tizen-build-rootfs.sh
else
    echo "Unsupported target platform."
    usage;
    exit 1
fi
