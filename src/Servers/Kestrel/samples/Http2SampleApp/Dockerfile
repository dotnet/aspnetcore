FROM microsoft/aspnetcore:2.0.0-stretch

RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    libssl-dev && \
    rm -rf /var/lib/apt/lists/*

ARG CONFIGURATION=Debug

WORKDIR /app

COPY ./bin/${CONFIGURATION}/netcoreapp2.2/publish/ /app

ENTRYPOINT [ "/usr/bin/dotnet", "/app/Http2SampleApp.dll" ]
