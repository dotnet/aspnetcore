FROM microsoft/dotnet-nightly:2.0-runtime-deps

# The "container" environment variable is read by systemd.
ENV container=docker

# Install and configure systemd which requires dbus for graceful shutdown.
RUN ["apt-get", "-o", "Acquire::Check-Valid-Until=false", "update"]
RUN ["apt-get", "install", "-y", "--no-install-recommends", "systemd-sysv"]

# Set proper systemd default target.
RUN ["systemctl", "set-default", "multi-user.target"]

# Remove non-vital systemd services.
RUN ["find", "/etc/systemd/system", "/lib/systemd/system", \
    "-path", "*.wants/*", \
    "-not", "-name", "*systemd-journald*", \
    "-not", "-name", "*systemd-tmpfiles*", \
    "-not", "-name", "*systemd-user-sessions*", \
    "-delete"]

# Copy .NET installation.
ADD .dotnet/ /usr/share/dotnet/
RUN ["ln", "-s", "/usr/share/dotnet/dotnet", "/usr/bin/dotnet"]

# Create activate-kestrel.service to launch the "publish" app on new requests to 8080.
EXPOSE 8080
ADD publish/ /publish/
ADD activate-kestrel.service /etc/systemd/system/activate-kestrel.service
ADD activate-kestrel.socket /etc/systemd/system/activate-kestrel.socket

# Automatically start activate-kestrel.socket on boot.
RUN ["systemctl", "enable", "activate-kestrel.socket"]

# Launch systemd, with workaround for docker/docker#27202, technique based on comments from docker/docker#9212.
CMD ["/bin/bash", "-c", "exec /sbin/init --log-target=journal 3>&1"]
STOPSIGNAL SIGRTMIN+3
