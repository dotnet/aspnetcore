# Dockerfile that creates a container suitable to build dotnet-cli
FROM mcr.microsoft.com/dotnet-buildtools/prereqs:rhel-7-rpmpkg-e1b4a89-20175311035359

# Setup User to match Host User, and give superuser permissions
ARG USER
ARG USER_ID
ARG GROUP_ID
ARG WORKDIR

WORKDIR ${WORKDIR}

RUN useradd -m ${USER} --uid ${USER_ID} -g root
RUN echo '${USER} ALL=(ALL) NOPASSWD:ALL' >> /etc/sudoers

# With the User Change, we need to change permssions on these directories
RUN chmod -R a+rwx /usr/local
RUN chmod -R a+rwx /home
RUN chown root:root /usr/bin/sudo && chmod 4755 /usr/bin/sudo

# Set user to the one we just created
USER $USER_ID

# Skip package initilization
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
