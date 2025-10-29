# Use the nightly Microsoft .NET SDK image for building
FROM mcr.microsoft.com/dotnet/nightly/sdk:10.0-preview AS build
WORKDIR /src

# Set environment variable to bypass OpenSSL 3.x compatibility issues
ENV OPENSSL_CONF=/dev/null

# Copy necessary build property files from repository root
COPY ["Directory.Build.props", "/src/"]
COPY ["Directory.Build.targets", "/src/"]
COPY ["global.json", "/src/"]
COPY ["NuGet.config", "/src/"]
COPY ["eng/", "/src/eng/"]
COPY ["artifacts/bin/GenerateFiles/", "/src/artifacts/bin/GenerateFiles/"]

# Copy all source projects (needed for Reference resolution)
COPY ["src/", "/src/src/"]

# Set working directory to the sample project
WORKDIR /src/src/Identity/samples/IdentitySample.DefaultUI

# Restore dependencies
RUN dotnet restore "IdentitySample.DefaultUI.csproj" /p:AllowMissingPrunePackageData=true

# Build the application
RUN dotnet build "IdentitySample.DefaultUI.csproj" -c Release -o /app/build /p:AllowMissingPrunePackageData=true

# Publish the application
FROM build AS publish
RUN dotnet publish "IdentitySample.DefaultUI.csproj" -c Release -o /app/publish \
    /p:AllowMissingPrunePackageData=true \
    /p:UseAppHost=false \
    --self-contained false

# Use the official .NET runtime image for running the app
FROM mcr.microsoft.com/dotnet/nightly/aspnet:10.0-preview AS final

# Install the specific runtime version needed
RUN apt-get update && apt-get install -y curl && \
    curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh && \
    chmod +x /tmp/dotnet-install.sh && \
    /tmp/dotnet-install.sh --channel 10.0 --quality preview --runtime dotnet --install-dir /usr/share/dotnet && \
    /tmp/dotnet-install.sh --channel 10.0 --quality preview --runtime aspnetcore --install-dir /usr/share/dotnet && \
    rm /tmp/dotnet-install.sh && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app
EXPOSE 80
EXPOSE 443

# Set environment variables
ENV DOTNET_ROLL_FORWARD=LatestMinor
ENV DOTNET_ROLL_FORWARD_TO_PRERELEASE=1

# Copy the published application
COPY --from=publish /app/publish .

# Set the entry point
ENTRYPOINT ["dotnet", "IdentitySample.DefaultUI.dll"]
