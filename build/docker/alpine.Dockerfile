FROM microsoft/dotnet:2.1-runtime-deps-alpine
WORKDIR /code/build

RUN apk add --no-cache \
        bash \
        wget \
        git \
        jq \
        curl \
        icu-libs \
        openssl

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
    && curl -fsSL -o /tmp/dotnet.tar.gz https://dotnetcli.blob.core.windows.net/dotnet/Sdk/$DOTNET_SDK_VERSION/dotnet-sdk-$DOTNET_SDK_VERSION-alpine.3.6-x64.tar.gz \
    && mkdir -p /usr/share/dotnet \
    && tar xzf /tmp/dotnet.tar.gz -C /usr/share/dotnet \
    && ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet
