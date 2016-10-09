FROM microsoft/dotnet-nightly:1.1-sdk-projectjson-nanoserver

SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

RUN New-Item -Path \MusicStore\samples\MusicStore.Standalone -Type Directory
WORKDIR MusicStore

ADD samples/MusicStore.Standalone/project.json samples/MusicStore.Standalone/project.json
ADD NuGet.config .
RUN dotnet restore .\samples\MusicStore.Standalone

ADD samples samples
RUN dotnet build .\samples\MusicStore.Standalone

EXPOSE 5000
ENV ASPNETCORE_URLS http://0.0.0.0:5000
CMD dotnet run -p .\samples\MusicStore.Standalone
