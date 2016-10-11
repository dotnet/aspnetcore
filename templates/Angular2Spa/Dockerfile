FROM microsoft/dotnet:1.0.0-preview2-onbuild

RUN apt-get update
RUN wget -qO- https://deb.nodesource.com/setup_4.x | bash -
RUN apt-get install -y build-essential nodejs

WORKDIR /app

COPY project.json .
RUN ["dotnet", "restore"]

COPY . /app
RUN ["dotnet", "build"]

EXPOSE 5000/tcp

ENTRYPOINT ["dotnet", "run", "--server.urls", "http://0.0.0.0:5000"]
