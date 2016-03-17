FROM microsoft/aspnet

COPY . /app
WORKDIR /app
RUN ["dnu", "restore"]

EXPOSE 5004
ENTRYPOINT ["dnx", "-p", "project.json", "web"]