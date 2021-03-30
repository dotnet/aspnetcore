#
# Copyright (c) .NET Foundation. All rights reserved.
# Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
#

# Dockerfile that creates a container suitable to build dotnet-cli
FROM ubuntu:18.04

# Misc Dependencies for build
RUN apt-get update && \
    apt-get -qqy install \
        curl \
        unzip \
        gettext \
        sudo \
        libunwind8 \
        libkrb5-3 \
        libicu60 \
        liblttng-ust0 \
        libssl1.0.0 \
        locales \
        zlib1g \
        libuuid1 \
        debhelper \
        build-essential \
        devscripts \
        git \
        cmake \
        clang-3.5 \
        lldb-3.6 \
        wget && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/* && \
    locale-gen "en_US.UTF-8"

# Use clang as c++ compiler
RUN update-alternatives --install /usr/bin/c++ c++ /usr/bin/clang++-3.5 100
RUN update-alternatives --set c++ /usr/bin/clang++-3.5

# Setup User to match Host User, and give superuser permissions
ARG USER_ID=0
RUN useradd -m code_executor -u ${USER_ID} -g sudo
RUN echo 'code_executor ALL=(ALL) NOPASSWD:ALL' >> /etc/sudoers

# Preset well-known locale
ENV LC_ALL en_US.UTF-8
ENV LANG en_US.UTF-8
ENV LANGUAGE en_US.UTF-8

# With the User Change, we need to change permissions on these directories
RUN chmod -R a+rwx /usr/local
RUN chmod -R a+rwx /home
RUN chmod -R 755 /usr/lib/sudo

# Set user to the one we just created
USER ${USER_ID}

# Set working directory
WORKDIR /opt/code
