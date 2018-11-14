#!/bin/bash

set -e

cd /publish
systemd-socket-activate -l 8080 -E ASPNETCORE_BASE_PORT=7000 dotnet SystemdTestApp.dll &
socat TCP-LISTEN:8081,fork TCP-CONNECT:127.0.0.1:7000 &
socat TCP-LISTEN:8082,fork TCP-CONNECT:127.0.0.1:7001 &
systemd-socket-activate -l /tmp/activate-kestrel.sock -E ASPNETCORE_BASE_PORT=7100 dotnet SystemdTestApp.dll &
socat TCP-LISTEN:8083,fork UNIX-CLIENT:/tmp/activate-kestrel.sock &
socat TCP-LISTEN:8084,fork TCP-CONNECT:127.0.0.1:7100 &
socat TCP-LISTEN:8085,fork TCP-CONNECT:127.0.0.1:7101 &
trap 'exit 0' SIGTERM
wait
