FROM microsoft/dotnet:latest

RUN apt-get update
RUN apt-get install -y build-essential nodejs nodejs-legacy

COPY . /app

WORKDIR /app

RUN ["dotnet", "restore"]

RUN ["dotnet", "build"]

EXPOSE 5000/tcp

ENTRYPOINT ["dotnet", "run", "--server.urls", "http://0.0.0.0:5000"]
