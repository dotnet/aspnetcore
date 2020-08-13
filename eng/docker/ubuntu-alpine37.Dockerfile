FROM mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-16.04-cross-arm64-alpine10fcdcf-20190208200917
ARG USER
ARG USER_ID
ARG GROUP_ID
ARG WORKDIR

WORKDIR ${WORKDIR}
RUN mkdir -p "/home/$USER" && chown "${USER_ID}:${GROUP_ID}" "/home/$USER"
ENV HOME "/home/$USER"

USER $USER_ID:$GROUP_ID

# Disable the invariant mode (set in base image)
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT false
ENV LC_ALL en_US.UTF-8
ENV LANG en_US.UTF-8

# Skip package initilization
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
