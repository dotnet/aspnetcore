#!/usr/bin/env bash

set -e

scriptDir=$(dirname "${BASH_SOURCE[0]}")
~/.dotnet/dotnet publish -f netcoreapp2.0 ./samples/SampleApp/
cp -R ./samples/SampleApp/bin/Debug/netcoreapp2.0/publish/ $scriptDir
cp -R ~/.dotnet/ $scriptDir

image=$(docker build -qf $scriptDir/Dockerfile $scriptDir)
container=$(docker run -Ptd --privileged $image)

# Try to connect to SampleApp once a second up to 10 times.
for i in {1..10}; do curl $(docker port $container 8080/tcp) && exit 0 || sleep 1; done

exit -1
