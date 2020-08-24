FROM selenium/standalone-chrome:3.141.59-mercury as final

ENV StressRunDuration=0

WORKDIR /app
COPY ./Driver/bin/Release/net5.0/linux-x64/publish ./
COPY ./exec.sh ./

ENTRYPOINT [ "bash", "./exec.sh" ]
