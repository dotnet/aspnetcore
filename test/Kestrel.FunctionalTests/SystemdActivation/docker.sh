#!/usr/bin/env bash

set -e

scriptDir=$(dirname "${BASH_SOURCE[0]}")
PATH="$HOME/.dotnet/:$PATH"
dotnet publish -f netcoreapp2.0 ./samples/SampleApp/
cp -R ./samples/SampleApp/bin/Debug/netcoreapp2.0/publish/ $scriptDir
cp -R ~/.dotnet/ $scriptDir

image=$(docker build -qf $scriptDir/Dockerfile $scriptDir)
container=$(docker run -Pd $image)

# Try to connect to SampleApp once a second up to 10 times via all available ports.
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
