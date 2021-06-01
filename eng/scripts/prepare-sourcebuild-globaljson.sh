#!/usr/bin/env bash

set -euo pipefail

jq \
    '.tools.runtimes = {.tools.runtimes | .dotnet = ."dotnet/x64" | del(."dotnet/x64") | del(."dotnet/x86")}' \
    global.json > global.json.swap
mv global.json.swap global.json
