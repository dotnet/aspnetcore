#!/usr/bin/env bash

# getNonPortableDistroRid
#
# Input:
#   targetOs: (str)
#   targetArch: (str)
#   rootfsDir: (str)
#
# Return:
#   non-portable rid
getNonPortableDistroRid()
{
    local targetOs="$1"
    local targetArch="$2"
    local rootfsDir="$3"
    local nonPortableRid=""

    if [ "$targetOs" = "linux" ]; then
        if [ -e "${rootfsDir}/etc/os-release" ]; then
            source "${rootfsDir}/etc/os-release"

            if [[ "${ID}" == "rhel" || "${ID}" == "rocky" || "${ID}" == "alpine" ]]; then
                # remove the last version digit
                VERSION_ID="${VERSION_ID%.*}"
            fi

            if [[ "${VERSION_ID:-}" =~ ^([[:digit:]]|\.)+$ ]]; then
                nonPortableRid="${ID}.${VERSION_ID}-${targetArch}"
            else
                # Rolling release distros either do not set VERSION_ID, set it as blank or
                # set it to non-version looking string (such as TEMPLATE_VERSION_ID on ArchLinux);
                # so omit it here to be consistent with everything else.
                nonPortableRid="${ID}-${targetArch}"
            fi

        elif [ -e "${rootfsDir}/android_platform" ]; then
            source "$rootfsDir"/android_platform
            nonPortableRid="$RID"
        fi
    fi

    if [ "$targetOs" = "freebsd" ]; then
        # $rootfsDir can be empty. freebsd-version is shell script and it should always work.
        __freebsd_major_version=$($rootfsDir/bin/freebsd-version | { read v; echo "${v%%.*}"; })
        nonPortableRid="freebsd.$__freebsd_major_version-${targetArch}"
    elif command -v getprop && getprop ro.product.system.model 2>&1 | grep -qi android; then
        __android_sdk_version=$(getprop ro.build.version.sdk)
        nonPortableRid="android.$__android_sdk_version-${targetArch}"
    elif [ "$targetOs" = "illumos" ]; then
        __uname_version=$(uname -v)
        case "$__uname_version" in
            omnios-*)
                __omnios_major_version=$(echo "${__uname_version:8:2}")
                nonPortableRid=omnios."$__omnios_major_version"-"$targetArch"
            ;;
            joyent_*)
                __smartos_major_version=$(echo "${__uname_version:7:4}")
                nonPortableRid=smartos."$__smartos_major_version"-"$targetArch"
            ;;
            illumos_*)
                nonPortableRid=openindiana-"$targetArch"
            ;;
        esac
    elif [ "$targetOs" = "solaris" ]; then
        __uname_version=$(uname -v)
        __solaris_major_version=$(echo "${__uname_version%.*}")
        nonPortableRid=solaris."$__solaris_major_version"-"$targetArch"
    elif [ "$targetOs" = "haiku" ]; then
        __uname_release=$(uname -r)
        nonPortableRid=haiku.r"$__uname_release"-"$targetArch"
    fi

    echo "$(echo $nonPortableRid | tr '[:upper:]' '[:lower:]')"
}

# initDistroRidGlobal
#
# Input:
#   os: (str)
#   arch: (str)
#   isPortable: (int)
#   rootfsDir?: (nullable:string)
#
# Return:
#   None
#
# Notes:
#
# It is important to note that the function does not return anything, but it
# exports the following variables on success:
#
#   __DistroRid   : Non-portable rid of the target platform.
#   __PortableTargetOS  : OS-part of the portable rid that corresponds to the target platform.
#
initDistroRidGlobal()
{
    local targetOs="$1"
    local targetArch="$2"
    local isPortable="$3"
    local rootfsDir=""
    if [ "$#" -ge 4 ]; then
        rootfsDir="$4"
    fi

    if [ -n "${rootfsDir}" ]; then
        # We may have a cross build. Check for the existence of the rootfsDir
        if [ ! -e "${rootfsDir}" ]; then
            echo "Error rootfsDir has been passed, but the location is not valid."
            exit 1
        fi
    fi

    __DistroRid=$(getNonPortableDistroRid "${targetOs}" "${targetArch}" "${rootfsDir}")

    if [ -z "${__PortableTargetOS:-}" ]; then
        __PortableTargetOS="$targetOs"

        STRINGS="$(command -v strings || true)"
        if [ -z "$STRINGS" ]; then
            STRINGS="$(command -v llvm-strings || true)"
        fi

        # Check for musl-based distros (e.g Alpine Linux, Void Linux).
        if "${rootfsDir}/usr/bin/ldd" --version 2>&1 | grep -q musl ||
                ( [ -n "$STRINGS" ] && "$STRINGS" "${rootfsDir}/usr/bin/ldd" 2>&1 | grep -q musl ); then
            __PortableTargetOS="linux-musl"
        fi
    fi

    export __DistroRid __PortableTargetOS
}
