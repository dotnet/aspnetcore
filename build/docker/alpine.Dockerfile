FROM microsoft/dotnet:2.1.0-preview1-runtime-deps-alpine
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

# Disable the invariant mode (set in base image)
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT false
ENV LC_ALL en_US.UTF-8
ENV LANG en_US.UTF-8

# Skip package initilization
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# Workarounds https://github.com/dotnet/cli/issues/8738
ENV DOTNET_INSTALL_SKIP_PREREQS=1
ENV KOREBUILD_SKIP_RUNTIME_INSTALL=1

COPY global.json /tmp/global.json
RUN DOTNET_SDK_VERSION="$(jq -r '.sdk.version' /tmp/global.json)" \
    && echo "Installing SDK ${DOTNET_SDK_VERSION}" \
    && wget -q --tries 10 -O /tmp/dotnet.tar.gz https://dotnetcli.blob.core.windows.net/dotnet/Sdk/$DOTNET_SDK_VERSION/dotnet-sdk-$DOTNET_SDK_VERSION-linux-musl-x64.tar.gz \
    && mkdir -p "$HOME/.dotnet" \
    && tar xzf /tmp/dotnet.tar.gz -C "$HOME/.dotnet"
