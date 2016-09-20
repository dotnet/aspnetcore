FROM microsoft/aspnet:1.0.0-beta5

COPY project.json /app/
WORKDIR /app
RUN ["dnu", "restore"]
COPY . /app

EXPOSE 5004
ENTRYPOINT ["dnx", "project.json", "kestrel"]
