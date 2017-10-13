#
# Copyright (c) .NET Foundation. All rights reserved.
# Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
#

FROM ubuntu:16.04

# Install the base toolchain we need to build anything (clang, cmake, make and the like)
# this does not include libraries that we need to compile different projects, we'd like
# them in a different layer.
RUN apt-get update && \
    apt-get install -y cmake \
        make \
        llvm-3.5 \
        clang-3.5 \
        git \
        curl \
        tar \
        sudo \
        debhelper \
        build-essential \
        devscripts \
        libunwind8 \
        libkrb5-3 \
        libicu55 \
        liblttng-ust0 \
        libssl1.0.0 \
        zlib1g \
        libuuid1 \
        liblldb-3.6 \
        wget && \
    apt-get clean

# Setup User to match Host User, and give superuser permissions
ARG USER_ID=0
RUN useradd -m code_executor -u ${USER_ID} -g sudo
RUN echo 'code_executor ALL=(ALL) NOPASSWD:ALL' >> /etc/sudoers

# With the User Change, we need to change permissions on these directories
RUN chmod -R a+rwx /usr/local
RUN chmod -R a+rwx /home
RUN chmod -R 755 /usr/lib/sudo

# Set user to the one we just created
USER ${USER_ID}

# Set working directory
WORKDIR /opt/code