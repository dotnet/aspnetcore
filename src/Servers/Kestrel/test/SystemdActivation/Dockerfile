FROM microsoft/dotnet-nightly:2.0-runtime-deps

# The "container" environment variable is read by systemd.
ENV container=docker

# Install and configure dependencies.
RUN ["apt-get", "-o", "Acquire::Check-Valid-Until=false", "update"]
RUN ["apt-get", "install", "-y", "--no-install-recommends", "systemd", "socat"]

# Copy .NET installation.
ADD .dotnet/ /usr/share/dotnet/
RUN ["ln", "-s", "/usr/share/dotnet/dotnet", "/usr/bin/dotnet"]

# Copy "publish" app.
ADD publish/ /publish/

# Expose target ports.
EXPOSE 8080 8081 8082 8083 8084 8085

# Set entrypoint.
COPY ./docker-entrypoint.sh /
RUN chmod +x /docker-entrypoint.sh
ENTRYPOINT ["/docker-entrypoint.sh"]
