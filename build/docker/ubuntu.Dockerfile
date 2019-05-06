FROM microsoft/dotnet-buildtools-prereqs:ubuntu-14.04-cross-e435274-20180426002420

ARG USER
ARG USER_ID
ARG GROUP_ID

WORKDIR /code/build
RUN mkdir -p "/home/$USER" && chown "${USER_ID}:${GROUP_ID}" "/home/$USER"
ENV HOME "/home/$USER"

# Set the user to non-root
USER $USER_ID:$GROUP_ID

# Skip package initilization
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
