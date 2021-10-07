# Dockerfile that creates a container suitable to build dotnet-cli
FROM mcr.microsoft.com/dotnet-buildtools/prereqs:centos-7-rpmpkg-20210714125435-9b5bbc2

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

# Workaround per https://github.com/dotnet/aspnetcore/pull/37192#issuecomment-936589233
RUN yum --enablerepo=centos-sclo-rh -y install rh-ruby25
RUN echo -e "#!/bin/bash\n\
source /opt/rh/rh-ruby25/enable\n\
export X_SCLS=\"`scl enable rh-ruby25 'echo $X_SCLS'`\"\n" >> /etc/profile.d/rh-ruby25.sh

