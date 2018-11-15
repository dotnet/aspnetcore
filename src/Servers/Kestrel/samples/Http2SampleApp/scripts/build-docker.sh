#!/usr/bin/env bash
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

dotnet publish --framework netcoreapp2.2 "$DIR/../Http2SampleApp.csproj"

docker build -t kestrel-http2-sample "$DIR/.."
