# Platform: Docker Linux Repro

Use for Linux-specific bugs, container deployment issues, Docker-specific behavior.

## Prerequisites

```bash
docker --version    # verify Docker is available
docker info         # verify Docker daemon is running
```

## Create → Build → Run → Verify

### Create the app

```bash
mkdir -p /tmp/aspnetcore/repro/{number}-docker && cd /tmp/aspnetcore/repro/{number}-docker
dotnet new webapi -n Repro{number}Docker --no-openapi
cd Repro{number}Docker
```

### Create Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Repro{number}Docker.dll"]
```

For Alpine (to test musl libc issues):
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
# ...
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine
```

### Build and Run

```bash
docker build -t repro-{number} .
docker run -d --name repro-{number} -p 8080:8080 repro-{number}
sleep 3  # wait for startup
curl -sv http://localhost:8080/test
docker stop repro-{number}
docker rm repro-{number}
```

### Linux x64 (forced, from macOS ARM64)

```bash
docker build --platform linux/amd64 -t repro-{number}-x64 .
docker run -d --platform linux/amd64 --name repro-{number}-x64 -p 8081:8080 repro-{number}-x64
curl http://localhost:8081/test
docker stop repro-{number}-x64 && docker rm repro-{number}-x64
```

### Linux ARM64

```bash
docker build --platform linux/arm64 -t repro-{number}-arm64 .
docker run -d --platform linux/arm64 --name repro-{number}-arm64 -p 8082:8080 repro-{number}-arm64
curl http://localhost:8082/test
docker stop repro-{number}-arm64 && docker rm repro-{number}-arm64
```

## Debug Inside Container

```bash
docker run -it --name repro-debug repro-{number} /bin/bash
# Inside container:
dotnet --info
ls /app/
./Repro{number}Docker
```

## Viewing Container Logs

```bash
docker logs repro-{number}
```

## Common Linux-Specific Issues

- **Case sensitivity:** File paths that work on macOS/Windows may fail on Linux
- **Port binding:** Use `0.0.0.0` not `localhost` in `ASPNETCORE_URLS`
- **File permissions:** Ensure published files are readable
- **Missing fonts:** If text rendering is involved: `RUN apt-get update && apt-get install -y fontconfig`
- **SSL certificates:** Dev certs don't work in Docker without explicit configuration

## Cleanup

```bash
docker rm -f repro-{number} 2>/dev/null
docker rmi repro-{number} 2>/dev/null
rm -rf /tmp/aspnetcore/repro/{number}-docker
```
