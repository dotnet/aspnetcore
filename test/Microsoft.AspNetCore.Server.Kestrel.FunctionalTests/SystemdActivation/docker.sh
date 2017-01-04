#!/usr/bin/env bash

set -e

# Ensure that dotnet is added to the PATH.
# build.sh should always be run before this script to create the .build/ directory and restore packages.
scriptDir=$(dirname "${BASH_SOURCE[0]}")
repoDir=$(cd $scriptDir/../../.. && pwd)
source ./.build/KoreBuild.sh -r $repoDir --quiet

dotnet publish -f netcoreapp1.1 ./samples/SampleApp/
cp -R ./samples/SampleApp/bin/Debug/netcoreapp1.1/publish/ $scriptDir
cp -R ~/.dotnet/ $scriptDir

image=$(docker build -qf $scriptDir/Dockerfile $scriptDir)
container=$(docker run -Ptd --privileged $image)

# Try to connect to SampleApp once a second up to 10 times.
for i in {1..10}; do curl $(docker port $container 8080/tcp) && exit 0 || sleep 1; done

exit -1
