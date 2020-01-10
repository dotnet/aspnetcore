FROM selenium/standalone-chrome:3.141.59-mercury as final

WORKDIR /app
COPY ./Driver/bin/Release/netcoreapp3.1/linux-x64/publish ./
COPY ./exec.sh ./

ENTRYPOINT [ "bash", "./exec.sh" ]
