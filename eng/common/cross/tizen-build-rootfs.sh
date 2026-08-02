#!/usr/bin/env bash
set -e

ARCH=$1
LINK_ARCH=$ARCH

case "$ARCH" in
    arm)
        TIZEN_ARCH="armv7hl"
        ;;
    armel)
        TIZEN_ARCH="armv7l"
        LINK_ARCH="arm"
        ;;
    arm64)
        TIZEN_ARCH="aarch64"
        ;;
    x86)
        TIZEN_ARCH="i686"
        ;;
    x64)
        TIZEN_ARCH="x86_64"
        LINK_ARCH="x86"
        ;;
    riscv64)
        TIZEN_ARCH="riscv64"
        LINK_ARCH="riscv"
        ;;
    *)
        echo "Unsupported architecture for tizen: $ARCH"
        exit 1
esac

__CrossDir=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )
__TIZEN_CROSSDIR="$__CrossDir/${ARCH}/tizen"

if [[ -z "$ROOTFS_DIR" ]]; then
    echo "ROOTFS_DIR is not defined."
    exit 1;
fi

TIZEN_TMP_DIR=$ROOTFS_DIR/tizen_tmp
mkdir -p $TIZEN_TMP_DIR

# Download files
echo ">>Start downloading files"
VERBOSE=1 $__CrossDir/tizen-fetch.sh $TIZEN_TMP_DIR $TIZEN_ARCH
echo "<<Finish downloading files"

echo ">>Start constructing Tizen rootfs"
TIZEN_RPM_FILES=`ls $TIZEN_TMP_DIR/*.rpm`
cd $ROOTFS_DIR
for f in $TIZEN_RPM_FILES; do
    rpm2cpio $f  | cpio -idm --quiet
done
echo "<<Finish constructing Tizen rootfs"

# Cleanup tmp
rm -rf $TIZEN_TMP_DIR

# Configure Tizen rootfs
echo ">>Start configuring Tizen rootfs"
ln -sfn asm-${LINK_ARCH} ./usr/include/asm
patch -p1 < $__TIZEN_CROSSDIR/tizen.patch
if [[ "$TIZEN_ARCH" == "riscv64" ]]; then
    echo "Fixing broken symlinks in $PWD"
    rm ./usr/lib64/libresolv.so
    ln -s ../../lib64/libresolv.so.2 ./usr/lib64/libresolv.so
    rm ./usr/lib64/libpthread.so
    ln -s ../../lib64/libpthread.so.0 ./usr/lib64/libpthread.so
    rm ./usr/lib64/libdl.so
    ln -s ../../lib64/libdl.so.2 ./usr/lib64/libdl.so
    rm ./usr/lib64/libutil.so
    ln -s ../../lib64/libutil.so.1 ./usr/lib64/libutil.so
    rm ./usr/lib64/libm.so
    ln -s ../../lib64/libm.so.6 ./usr/lib64/libm.so
    rm ./usr/lib64/librt.so
    ln -s ../../lib64/librt.so.1 ./usr/lib64/librt.so
    rm ./lib/ld-linux-riscv64-lp64d.so.1
    ln -s ../lib64/ld-linux-riscv64-lp64d.so.1 ./lib/ld-linux-riscv64-lp64d.so.1
fi
echo "<<Finish configuring Tizen rootfs"
