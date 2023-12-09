# See here for image contents: https://github.com/microsoft/vscode-dev-containers/tree/v0.177.0/containers/dotnet/.devcontainer/base.Dockerfile

# full tag list: https://mcr.microsoft.com/v2/devcontainers/dotnet/tags/list
ARG VARIANT="8.0"
FROM mcr.microsoft.com/devcontainers/dotnet:${VARIANT}

# [Option] Install Node.js
ARG INSTALL_NODE="true"
ARG NODE_VERSION="lts/*"
RUN if [ "${INSTALL_NODE}" = "true" ]; then su vscode -c "umask 0002 && . /usr/local/share/nvm/nvm.sh && nvm install ${NODE_VERSION} 2>&1"; fi
