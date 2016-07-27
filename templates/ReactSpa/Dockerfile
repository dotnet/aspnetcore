FROM microsoft/dotnet:latest

COPY . /app

WORKDIR /app

# Add Node.js to the container. If you don't want to wait for this to install every
# time you rebuild your container, consider creating an image that has it preinstalled.
RUN apt-get update
RUN apt-get install -y build-essential nodejs nodejs-legacy

RUN ["dotnet", "restore"]

RUN ["dotnet", "build"]

EXPOSE 5000/tcp

ENTRYPOINT ["dotnet", "run", "--server.urls", "http://0.0.0.0:5000"]
