#!/usr/bin/env bash

set -e

usage()
{
    echo "Usage: $0 [BuildArch] [CodeName] [lldbx.y] [llvmx[.y]] [--skipunmount] --rootfsdir <directory>]"
    echo "BuildArch can be: arm(default), arm64, armel, armv6, ppc64le, riscv64, s390x, x64, x86"
    echo "CodeName - optional, Code name for Linux, can be: xenial(default), zesty, bionic, alpine, alpine3.13 or alpine3.14. If BuildArch is armel, LinuxCodeName is jessie(default) or tizen."
    echo "                              for FreeBSD can be: freebsd12, freebsd13"
    echo "                              for illumos can be: illumos"
    echo "                                for Haiku can be: haiku."
    echo "lldbx.y - optional, LLDB version, can be: lldb3.9(default), lldb4.0, lldb5.0, lldb6.0 no-lldb. Ignored for alpine and FreeBSD"
    echo "llvmx[.y] - optional, LLVM version for LLVM related packages."
    echo "--skipunmount - optional, will skip the unmount of rootfs folder."
    echo "--use-mirror - optional, use mirror URL to fetch resources, when available."
    echo "--jobs N - optional, restrict to N jobs."
    exit 1
}

__CodeName=xenial
__CrossDir=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )
__BuildArch=arm
__AlpineArch=armv7
__FreeBSDArch=arm
__FreeBSDMachineArch=armv7
__IllumosArch=arm7
__QEMUArch=arm
__UbuntuArch=armhf
__UbuntuRepo="http://ports.ubuntu.com/"
__LLDB_Package="liblldb-3.9-dev"
__SkipUnmount=0

# base development support
__UbuntuPackages="build-essential"

__AlpinePackages="alpine-base"
__AlpinePackages+=" build-base"
__AlpinePackages+=" linux-headers"
__AlpinePackages+=" lldb-dev"
__AlpinePackages+=" python3"
__AlpinePackages+=" libedit"

# symlinks fixer
__UbuntuPackages+=" symlinks"

# runtime dependencies
__UbuntuPackages+=" libicu-dev"
__UbuntuPackages+=" liblttng-ust-dev"
__UbuntuPackages+=" libunwind8-dev"

__AlpinePackages+=" gettext-dev"
__AlpinePackages+=" icu-dev"
__AlpinePackages+=" libunwind-dev"
__AlpinePackages+=" lttng-ust-dev"
__AlpinePackages+=" compiler-rt-static"

# runtime libraries' dependencies
__UbuntuPackages+=" libcurl4-openssl-dev"
__UbuntuPackages+=" libkrb5-dev"
__UbuntuPackages+=" libssl-dev"
__UbuntuPackages+=" zlib1g-dev"

__AlpinePackages+=" curl-dev"
__AlpinePackages+=" krb5-dev"
__AlpinePackages+=" openssl-dev"
__AlpinePackages+=" zlib-dev"

__FreeBSDBase="12.3-RELEASE"
__FreeBSDPkg="1.17.0"
__FreeBSDABI="12"
__FreeBSDPackages="libunwind"
__FreeBSDPackages+=" icu"
__FreeBSDPackages+=" libinotify"
__FreeBSDPackages+=" openssl"
__FreeBSDPackages+=" krb5"
__FreeBSDPackages+=" terminfo-db"

__IllumosPackages="icu"
__IllumosPackages+=" mit-krb5"
__IllumosPackages+=" openssl"
__IllumosPackages+=" zlib"

__HaikuPackages="gmp"
__HaikuPackages+=" gmp_devel"
__HaikuPackages+=" krb5"
__HaikuPackages+=" krb5_devel"
__HaikuPackages+=" libiconv"
__HaikuPackages+=" libiconv_devel"
__HaikuPackages+=" llvm12_libunwind"
__HaikuPackages+=" llvm12_libunwind_devel"
__HaikuPackages+=" mpfr"
__HaikuPackages+=" mpfr_devel"

# ML.NET dependencies
__UbuntuPackages+=" libomp5"
__UbuntuPackages+=" libomp-dev"

__Keyring=
__UseMirror=0

__UnprocessedBuildArgs=
while :; do
    if [[ "$#" -le 0 ]]; then
        break
    fi

    lowerI="$(echo "$1" | tr "[:upper:]" "[:lower:]")"
    case $lowerI in
        -\?|-h|--help)
            usage
            exit 1
            ;;
        arm)
            __BuildArch=arm
            __UbuntuArch=armhf
            __AlpineArch=armv7
            __QEMUArch=arm
            ;;
        arm64)
            __BuildArch=arm64
            __UbuntuArch=arm64
            __AlpineArch=aarch64
            __QEMUArch=aarch64
            __FreeBSDArch=arm64
            __FreeBSDMachineArch=aarch64
            ;;
        armel)
            __BuildArch=armel
            __UbuntuArch=armel
            __UbuntuRepo="http://ftp.debian.org/debian/"
            __CodeName=jessie
            ;;
        armv6)
            __BuildArch=armv6
            __UbuntuArch=armhf
            __QEMUArch=arm
            __UbuntuRepo="http://raspbian.raspberrypi.org/raspbian/"
            __CodeName=buster
            __LLDB_Package="liblldb-6.0-dev"

            if [[ -e "/usr/share/keyrings/raspbian-archive-keyring.gpg" ]]; then
                __Keyring="--keyring /usr/share/keyrings/raspbian-archive-keyring.gpg"
            fi
            ;;
        ppc64le)
            __BuildArch=ppc64le
            __UbuntuArch=ppc64el
            __UbuntuRepo="http://ports.ubuntu.com/ubuntu-ports/"
            __UbuntuPackages=$(echo ${__UbuntuPackages} | sed 's/ libunwind8-dev//')
            __UbuntuPackages=$(echo ${__UbuntuPackages} | sed 's/ libomp-dev//')
            __UbuntuPackages=$(echo ${__UbuntuPackages} | sed 's/ libomp5//')
            unset __LLDB_Package
            ;;
        riscv64)
            __BuildArch=riscv64
            __UbuntuArch=riscv64
            __UbuntuRepo="http://deb.debian.org/debian-ports"
            __CodeName=sid
            __UbuntuPackages=$(echo ${__UbuntuPackages} | sed 's/ libunwind8-dev//')
            unset __LLDB_Package

            if [[ -e "/usr/share/keyrings/debian-ports-archive-keyring.gpg" ]]; then
                __Keyring="--keyring /usr/share/keyrings/debian-ports-archive-keyring.gpg --include=debian-ports-archive-keyring"
            fi
            ;;
        s390x)
            __BuildArch=s390x
            __UbuntuArch=s390x
            __UbuntuRepo="http://ports.ubuntu.com/ubuntu-ports/"
            __UbuntuPackages=$(echo ${__UbuntuPackages} | sed 's/ libunwind8-dev//')
            __UbuntuPackages=$(echo ${__UbuntuPackages} | sed 's/ libomp-dev//')
            __UbuntuPackages=$(echo ${__UbuntuPackages} | sed 's/ libomp5//')
            unset __LLDB_Package
            ;;
        x64)
            __BuildArch=x64
            __UbuntuArch=amd64
            __FreeBSDArch=amd64
            __FreeBSDMachineArch=amd64
            __illumosArch=x86_64
            __UbuntuRepo=
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
        lldb5.0)
            __LLDB_Package="liblldb-5.0-dev"
            ;;
        lldb6.0)
            __LLDB_Package="liblldb-6.0-dev"
            ;;
        no-lldb)
            unset __LLDB_Package
            ;;
        llvm*)
            version="$(echo "$lowerI" | tr -d '[:alpha:]-=')"
            parts=(${version//./ })
            __LLVM_MajorVersion="${parts[0]}"
            __LLVM_MinorVersion="${parts[1]}"
            if [[ -z "$__LLVM_MinorVersion" && "$__LLVM_MajorVersion" -le 6 ]]; then
                __LLVM_MinorVersion=0;
            fi
            ;;
        xenial) # Ubuntu 16.04
            if [[ "$__CodeName" != "jessie" ]]; then
                __CodeName=xenial
            fi
            ;;
        zesty) # Ubuntu 17.04
            if [[ "$__CodeName" != "jessie" ]]; then
                __CodeName=zesty
            fi
            ;;
        bionic) # Ubuntu 18.04
            if [[ "$__CodeName" != "jessie" ]]; then
                __CodeName=bionic
            fi
            ;;
        jessie) # Debian 8
            __CodeName=jessie
            __UbuntuRepo="http://ftp.debian.org/debian/"
            ;;
        stretch) # Debian 9
            __CodeName=stretch
            __UbuntuRepo="http://ftp.debian.org/debian/"
            __LLDB_Package="liblldb-6.0-dev"
            ;;
        buster) # Debian 10
            __CodeName=buster
            __UbuntuRepo="http://ftp.debian.org/debian/"
            __LLDB_Package="liblldb-6.0-dev"
            ;;
        tizen)
            __CodeName=
            __UbuntuRepo=
            __Tizen=tizen
            ;;
        alpine|alpine3.13)
            __CodeName=alpine
            __UbuntuRepo=
            __AlpineVersion=3.13
            __AlpinePackages+=" llvm10-libs"
            ;;
        alpine3.14)
            __CodeName=alpine
            __UbuntuRepo=
            __AlpineVersion=3.14
            __AlpinePackages+=" llvm11-libs"
            ;;
        freebsd12)
            __CodeName=freebsd
            __SkipUnmount=1
            ;;
        freebsd13)
            __CodeName=freebsd
            __FreeBSDBase="13.0-RELEASE"
            __FreeBSDABI="13"
            __SkipUnmount=1
            ;;
        illumos)
            __CodeName=illumos
            __SkipUnmount=1
            ;;
        haiku)
            __CodeName=haiku
            __BuildArch=x64
            __SkipUnmount=1
            ;;
        --skipunmount)
            __SkipUnmount=1
            ;;
        --rootfsdir|-rootfsdir)
            shift
            __RootfsDir="$1"
            ;;
        --use-mirror)
            __UseMirror=1
            ;;
        --use-jobs)
            shift
            MAXJOBS=$1
            ;;
        *)
            __UnprocessedBuildArgs="$__UnprocessedBuildArgs $1"
            ;;
    esac

    shift
done

if [[ "$__BuildArch" == "armel" ]]; then
    __LLDB_Package="lldb-3.5-dev"
fi

__UbuntuPackages+=" ${__LLDB_Package:-}"

if [[ -n "$__LLVM_MajorVersion" ]]; then
    __UbuntuPackages+=" libclang-common-${__LLVM_MajorVersion}${__LLVM_MinorVersion:+.$__LLVM_MinorVersion}-dev"
fi

if [[ -z "$__RootfsDir" && -n "$ROOTFS_DIR" ]]; then
    __RootfsDir="$ROOTFS_DIR"
fi

if [[ -z "$__RootfsDir" ]]; then
    __RootfsDir="$__CrossDir/../../../.tools/rootfs/$__BuildArch"
fi

if [[ -d "$__RootfsDir" ]]; then
    if [[ "$__SkipUnmount" == "0" ]]; then
        umount "$__RootfsDir"/* || true
    fi
    rm -rf "$__RootfsDir"
fi

mkdir -p "$__RootfsDir"
__RootfsDir="$( cd "$__RootfsDir" && pwd )"

if [[ "$__CodeName" == "alpine" ]]; then
    __ApkToolsVersion=2.9.1
    __ApkToolsDir="$(mktemp -d)"
    wget "https://github.com/alpinelinux/apk-tools/releases/download/v$__ApkToolsVersion/apk-tools-$__ApkToolsVersion-x86_64-linux.tar.gz" -P "$__ApkToolsDir"
    tar -xf "$__ApkToolsDir/apk-tools-$__ApkToolsVersion-x86_64-linux.tar.gz" -C "$__ApkToolsDir"
    mkdir -p "$__RootfsDir"/usr/bin
    cp -v "/usr/bin/qemu-$__QEMUArch-static" "$__RootfsDir/usr/bin"

    "$__ApkToolsDir/apk-tools-$__ApkToolsVersion/apk" \
      -X "http://dl-cdn.alpinelinux.org/alpine/v$__AlpineVersion/main" \
      -X "http://dl-cdn.alpinelinux.org/alpine/v$__AlpineVersion/community" \
      -U --allow-untrusted --root "$__RootfsDir" --arch "$__AlpineArch" --initdb \
      add $__AlpinePackages

    rm -r "$__ApkToolsDir"
elif [[ "$__CodeName" == "freebsd" ]]; then
    mkdir -p "$__RootfsDir"/usr/local/etc
    JOBS=${MAXJOBS:="$(getconf _NPROCESSORS_ONLN)"}
    wget -O - "https://download.freebsd.org/ftp/releases/${__FreeBSDArch}/${__FreeBSDMachineArch}/${__FreeBSDBase}/base.txz" | tar -C "$__RootfsDir" -Jxf - ./lib ./usr/lib ./usr/libdata ./usr/include ./usr/share/keys ./etc ./bin/freebsd-version
    echo "ABI = \"FreeBSD:${__FreeBSDABI}:${__FreeBSDMachineArch}\"; FINGERPRINTS = \"${__RootfsDir}/usr/share/keys\"; REPOS_DIR = [\"${__RootfsDir}/etc/pkg\"]; REPO_AUTOUPDATE = NO; RUN_SCRIPTS = NO;" > "${__RootfsDir}"/usr/local/etc/pkg.conf
    echo "FreeBSD: { url: \"pkg+http://pkg.FreeBSD.org/\${ABI}/quarterly\", mirror_type: \"srv\", signature_type: \"fingerprints\", fingerprints: \"${__RootfsDir}/usr/share/keys/pkg\", enabled: yes }" > "${__RootfsDir}"/etc/pkg/FreeBSD.conf
    mkdir -p "$__RootfsDir"/tmp
    # get and build package manager
    wget -O - "https://github.com/freebsd/pkg/archive/${__FreeBSDPkg}.tar.gz" | tar -C "$__RootfsDir"/tmp -zxf -
    cd "$__RootfsDir/tmp/pkg-${__FreeBSDPkg}"
    # needed for install to succeed
    mkdir -p "$__RootfsDir"/host/etc
    ./autogen.sh && ./configure --prefix="$__RootfsDir"/host && make -j "$JOBS" && make install
    rm -rf "$__RootfsDir/tmp/pkg-${__FreeBSDPkg}"
    # install packages we need.
    INSTALL_AS_USER=$(whoami) "$__RootfsDir"/host/sbin/pkg -r "$__RootfsDir" -C "$__RootfsDir"/usr/local/etc/pkg.conf update
    INSTALL_AS_USER=$(whoami) "$__RootfsDir"/host/sbin/pkg -r "$__RootfsDir" -C "$__RootfsDir"/usr/local/etc/pkg.conf install --yes $__FreeBSDPackages
elif [[ "$__CodeName" == "illumos" ]]; then
    mkdir "$__RootfsDir/tmp"
    pushd "$__RootfsDir/tmp"
    JOBS=${MAXJOBS:="$(getconf _NPROCESSORS_ONLN)"}
    echo "Downloading sysroot."
    wget -O - https://github.com/illumos/sysroot/releases/download/20181213-de6af22ae73b-v1/illumos-sysroot-i386-20181213-de6af22ae73b-v1.tar.gz | tar -C "$__RootfsDir" -xzf -
    echo "Building binutils. Please wait.."
    wget -O - https://ftp.gnu.org/gnu/binutils/binutils-2.33.1.tar.bz2 | tar -xjf -
    mkdir build-binutils && cd build-binutils
    ../binutils-2.33.1/configure --prefix="$__RootfsDir" --target="${__illumosArch}-sun-solaris2.10" --program-prefix="${__illumosArch}-illumos-" --with-sysroot="$__RootfsDir"
    make -j "$JOBS" && make install && cd ..
    echo "Building gcc. Please wait.."
    wget -O - https://ftp.gnu.org/gnu/gcc/gcc-8.4.0/gcc-8.4.0.tar.xz | tar -xJf -
    CFLAGS="-fPIC"
    CXXFLAGS="-fPIC"
    CXXFLAGS_FOR_TARGET="-fPIC"
    CFLAGS_FOR_TARGET="-fPIC"
    export CFLAGS CXXFLAGS CXXFLAGS_FOR_TARGET CFLAGS_FOR_TARGET
    mkdir build-gcc && cd build-gcc
    ../gcc-8.4.0/configure --prefix="$__RootfsDir" --target="${__illumosArch}-sun-solaris2.10" --program-prefix="${__illumosArch}-illumos-" --with-sysroot="$__RootfsDir" --with-gnu-as       \
        --with-gnu-ld --disable-nls --disable-libgomp --disable-libquadmath --disable-libssp --disable-libvtv --disable-libcilkrts --disable-libada --disable-libsanitizer \
        --disable-libquadmath-support --disable-shared --enable-tls
    make -j "$JOBS" && make install && cd ..
    BaseUrl=https://pkgsrc.joyent.com
    if [[ "$__UseMirror" == 1 ]]; then
        BaseUrl=http://pkgsrc.smartos.skylime.net
    fi
    BaseUrl="$BaseUrl/packages/SmartOS/trunk/${__illumosArch}/All"
    echo "Downloading manifest"
    wget "$BaseUrl"
    echo "Downloading dependencies."
    read -ra array <<<"$__IllumosPackages"
    for package in "${array[@]}"; do
        echo "Installing '$package'"
        package="$(grep ">$package-[0-9]" All | sed -En 's/.*href="(.*)\.tgz".*/\1/p')"
        echo "Resolved name '$package'"
        wget "$BaseUrl"/"$package".tgz
        ar -x "$package".tgz
        tar --skip-old-files -xzf "$package".tmp.tg* -C "$__RootfsDir" 2>/dev/null
    done
    echo "Cleaning up temporary files."
    popd
    rm -rf "$__RootfsDir"/{tmp,+*}
    mkdir -p "$__RootfsDir"/usr/include/net
    mkdir -p "$__RootfsDir"/usr/include/netpacket
    wget -P "$__RootfsDir"/usr/include/net https://raw.githubusercontent.com/illumos/illumos-gate/master/usr/src/uts/common/io/bpf/net/bpf.h
    wget -P "$__RootfsDir"/usr/include/net https://raw.githubusercontent.com/illumos/illumos-gate/master/usr/src/uts/common/io/bpf/net/dlt.h
    wget -P "$__RootfsDir"/usr/include/netpacket https://raw.githubusercontent.com/illumos/illumos-gate/master/usr/src/uts/common/inet/sockmods/netpacket/packet.h
    wget -P "$__RootfsDir"/usr/include/sys https://raw.githubusercontent.com/illumos/illumos-gate/master/usr/src/uts/common/sys/sdt.h
elif [[ "$__CodeName" == "haiku" ]]; then
    JOBS=${MAXJOBS:="$(getconf _NPROCESSORS_ONLN)"}

    echo "Building Haiku sysroot for x86_64"
    mkdir -p "$__RootfsDir/tmp"
    cd "$__RootfsDir/tmp"
    git clone -b hrev56235  https://review.haiku-os.org/haiku
    git clone -b btrev43195 https://review.haiku-os.org/buildtools
    cd "$__RootfsDir/tmp/buildtools" && git checkout 7487388f5110021d400b9f3b88e1a7f310dc066d

    # Fetch some unmerged patches
    cd "$__RootfsDir/tmp/haiku"
    ## Add development build profile (slimmer than nightly)
    git fetch origin refs/changes/64/4164/1 && git -c commit.gpgsign=false cherry-pick FETCH_HEAD

    # Build jam
    cd "$__RootfsDir/tmp/buildtools/jam"
    make

    # Configure cross tools
    echo "Building cross-compiler"
    mkdir -p "$__RootfsDir/generated"
    cd "$__RootfsDir/generated"
    "$__RootfsDir/tmp/haiku/configure" -j"$JOBS" --sysroot "$__RootfsDir" --cross-tools-source "$__RootfsDir/tmp/buildtools" --build-cross-tools x86_64

    # Build Haiku packages
    echo "Building Haiku"
    echo 'HAIKU_BUILD_PROFILE = "development-raw" ;' > UserProfileConfig
    "$__RootfsDir/tmp/buildtools/jam/jam0" -j"$JOBS" -q '<build>package' '<repository>Haiku'

    BaseUrl="https://depot.haiku-os.org/__api/v2/pkg/get-pkg"

    # Download additional packages
    echo "Downloading additional required packages"
    read -ra array <<<"$__HaikuPackages"
    for package in "${array[@]}"; do
        echo "Downloading $package..."
        # API documented here: https://github.com/haiku/haikudepotserver/blob/master/haikudepotserver-api2/src/main/resources/api2/pkg.yaml#L60
        # The schema here: https://github.com/haiku/haikudepotserver/blob/master/haikudepotserver-api2/src/main/resources/api2/pkg.yaml#L598
        hpkgDownloadUrl="$(wget -qO- --post-data='{"name":"'"$package"'","repositorySourceCode":"haikuports_x86_64","versionType":"LATEST","naturalLanguageCode":"en"}' \
            --header='Content-Type:application/json' "$BaseUrl" | jq -r '.result.versions[].hpkgDownloadURL')"
        wget -P "$__RootfsDir/generated/download" "$hpkgDownloadUrl"
    done

    # Setup the sysroot
    echo "Setting up sysroot and extracting needed packages"
    mkdir -p "$__RootfsDir/boot/system"
    for file in "$__RootfsDir/generated/objects/haiku/x86_64/packaging/packages/"*.hpkg; do
        "$__RootfsDir/generated/objects/linux/x86_64/release/tools/package/package" extract -C "$__RootfsDir/boot/system" "$file"
    done
    for file in "$__RootfsDir/generated/download/"*.hpkg; do
        "$__RootfsDir/generated/objects/linux/x86_64/release/tools/package/package" extract -C "$__RootfsDir/boot/system" "$file"
    done

    # Cleaning up temporary files
    echo "Cleaning up temporary files"
    rm -rf "$__RootfsDir/tmp"
    for name in "$__RootfsDir/generated/"*; do
        if [[ "$name" =~ "cross-tools-" ]]; then
            : # Keep the cross-compiler
        else
            rm -rf "$name"
        fi
    done
elif [[ -n "$__CodeName" ]]; then
    qemu-debootstrap $__Keyring --arch "$__UbuntuArch" "$__CodeName" "$__RootfsDir" "$__UbuntuRepo"
    cp "$__CrossDir/$__BuildArch/sources.list.$__CodeName" "$__RootfsDir/etc/apt/sources.list"
    chroot "$__RootfsDir" apt-get update
    chroot "$__RootfsDir" apt-get -f -y install
    chroot "$__RootfsDir" apt-get -y install $__UbuntuPackages
    chroot "$__RootfsDir" symlinks -cr /usr
    chroot "$__RootfsDir" apt-get clean

    if [[ "$__SkipUnmount" == "0" ]]; then
        umount "$__RootfsDir"/* || true
    fi

    if [[ "$__BuildArch" == "armel" && "$__CodeName" == "jessie" ]]; then
        pushd "$__RootfsDir"
        patch -p1 < "$__CrossDir/$__BuildArch/armel.jessie.patch"
        popd
    fi
elif [[ "$__Tizen" == "tizen" ]]; then
    ROOTFS_DIR="$__RootfsDir" "$__CrossDir/$__BuildArch/tizen-build-rootfs.sh"
else
    echo "Unsupported target platform."
    usage;
    exit 1
fi
