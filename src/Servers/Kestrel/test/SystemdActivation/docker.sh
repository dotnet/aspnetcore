#!/usr/bin/env bash

set -e

scriptDir=$(dirname "${BASH_SOURCE[0]}")
dotnetDir="$PWD/.build/.dotnet"
PATH="$dotnetDir:$PATH"
dotnet publish -f netcoreapp2.2 ./samples/SystemdTestApp/
cp -R ./samples/SystemdTestApp/bin/Debug/netcoreapp2.2/publish/ $scriptDir
cp -R $dotnetDir $scriptDir

image=$(docker build -qf $scriptDir/Dockerfile $scriptDir)
container=$(docker run -Pd $image)

# Try to connect to SystemdTestApp once a second up to 10 times via all available ports.
for i in {1..10}; do
    curl -f http://$(docker port $container 8080/tcp) \
    && curl -f http://$(docker port $container 8081/tcp) \
    && curl -fk https://$(docker port $container 8082/tcp) \
    && curl -f http://$(docker port $container 8083/tcp) \
    && curl -f http://$(docker port $container 8084/tcp) \
    && curl -fk https://$(docker port $container 8085/tcp) \
    && exit 0 || sleep 1;
done

exit -1
