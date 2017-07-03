#!/bin/bash

set -e

cd /publish
systemd-socket-activate -l 8080 -E BASE_PORT=7000 dotnet SampleApp.dll &
socat TCP-LISTEN:8081,fork TCP-CONNECT:127.0.0.1:7000 &
socat TCP-LISTEN:8082,fork TCP-CONNECT:127.0.0.1:7001 &
trap 'exit 0' SIGTERM
wait
