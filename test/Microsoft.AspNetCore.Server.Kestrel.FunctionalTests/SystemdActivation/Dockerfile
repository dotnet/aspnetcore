FROM microsoft/dotnet-nightly:2.0-runtime-deps

# The "container" environment variable is read by systemd.
ENV container=docker

# This is required by systemd and won't work without "dotnet run --privileged".
VOLUME ["/sys/fs/cgroup"]

# Create activate-kestrel.service to launch the "publish" app on new requests to 8080.
EXPOSE 8080
ADD .dotnet/ /usr/share/dotnet/
ADD publish/ /publish/
ADD activate-kestrel.socket /etc/systemd/system/activate-kestrel.socket
ADD activate-kestrel.service /etc/systemd/system/activate-kestrel.service

# Install and configure systemd which requires dbus for graceful shutdown.
RUN ["apt-get", "-o", "Acquire::Check-Valid-Until=false", "update"]
RUN ["apt-get", "install", "-y", "dbus"]
RUN ["systemctl", "mask", "getty.target", "console-getty.service"]
RUN ["cp", "/lib/systemd/system/dbus.service", "/etc/systemd/system/"]
RUN ["sed", "-i", "s/OOMScoreAdjust=-900//", "/etc/systemd/system/dbus.service"]

# Automatically start activate-kestrel.service on boot.
RUN ["ln", "-s", "/usr/share/dotnet/dotnet", "/usr/bin/dotnet"]
RUN ["ln", "-s", "/usr/lib/systemd/system/activate-kestrel.service", "/etc/systemd/system/multi-user.target.wants/activate-kestrel.service"]

# Launch systemd.
CMD ["/sbin/init"]
