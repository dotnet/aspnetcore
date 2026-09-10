# See here for image contents: https://github.com/devcontainers/images/tree/main/src/dotnet

FROM mcr.microsoft.com/devcontainers/dotnet:latest

# [Option] Install Node.js
ARG INSTALL_NODE="true"
ARG NODE_VERSION="lts/*"
RUN if [ "${INSTALL_NODE}" = "true" ]; then su vscode -c "umask 0002 && . /usr/local/share/nvm/nvm.sh && nvm install ${NODE_VERSION} 2>&1"; fi
