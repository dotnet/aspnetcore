FROM mcr.microsoft.com/dotnet/runtime-deps:2.1-alpine
ARG USER
ARG USER_ID
ARG GROUP_ID

WORKDIR /code/build
RUN mkdir -p "/home/$USER" && chown "${USER_ID}:${GROUP_ID}" "/home/$USER"
ENV HOME "/home/$USER"

RUN apk add --no-cache \
        bash \
        wget \
        git \
        jq \
        curl \
        icu-libs \
        openssl

USER $USER_ID:$GROUP_ID

# Use a location for .dotnet/ that's not under repo root.
ENV DOTNET_HOME /code/.dotnet

# Disable the invariant mode (set in base image)
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT false
ENV LC_ALL en_US.UTF-8
ENV LANG en_US.UTF-8
ENV LANGUAGE en_US.UTF-8

# Skip package initilization
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
