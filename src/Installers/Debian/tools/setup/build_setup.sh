#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

install_dependencies(){
    # Add LLdb 3.6 package source
    echo "deb http://llvm.org/apt/trusty/ llvm-toolchain-trusty-3.6 main" | tee /etc/apt/sources.list.d/llvm.list
    wget -O - http://llvm.org/apt/llvm-snapshot.gpg.key | apt-key add -

    #Install Deps
    apt-get update
    apt-get install -y debhelper build-essential devscripts git liblttng-ust-dev lldb-3.6-dev
}

setup(){
    install_dependencies
}

setup