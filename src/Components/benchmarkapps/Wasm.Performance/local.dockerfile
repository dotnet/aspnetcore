# NOTE: This has been commented out because it is causing our builds to get a build warning
#       because we are pulling this container image from docker.io. We should consider whether
#       we need this code in our repo at all, and if not remove it.
#       If we do need it then we need to get the container image imported into mcr.microsoft.com
#
#       I have opened up a PR to do this, however it is not certain we'll be allowed to do this
#       and there is further legal/compliance work that needs to be done. In the meantime commenting
#       this out should get our builds to green again whilst this issue is resolved.
#
#       PR: https://github.com/microsoft/mcr/pull/3232
#
# FROM selenium/standalone-chrome:latest as final

ENV StressRunDuration=0

WORKDIR /app
COPY ./Driver/bin/Release/net9.0/linux-x64/publish ./
COPY ./exec.sh ./

ENTRYPOINT [ "bash", "./exec.sh" ]
