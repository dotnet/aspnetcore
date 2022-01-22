FROM microsoft/dotnet:2.1-runtime-deps-bionic

ARG USER
ARG USER_ID
ARG GROUP_ID
ARG WORKDIR

WORKDIR ${WORKDIR}
RUN mkdir -p "/home/$USER" && chown "${USER_ID}:${GROUP_ID}" "/home/$USER"
ENV HOME "/home/$USER"

RUN apt-get update && \
    apt-get -qqy install --no-install-recommends \
        jq \
        wget \
        locales \
        python \
        fakeroot \
        debhelper \
        build-essential \
        devscripts \
        unzip && \
    rm -rf /var/lib/apt/lists/*

# Resolves warnings about locale in the perl scripts for building debian installers
RUN locale-gen en_US.UTF-8
ENV LANGUAGE=en_US.UTF-8 \
    LANG=en_US.UTF-8 \
    LC_ALL=en_US.UTF-8

# Set the user to non-root
USER $USER_ID:$GROUP_ID

# Skip package initilization
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
