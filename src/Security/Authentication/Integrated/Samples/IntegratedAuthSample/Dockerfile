FROM mcr.microsoft.com/dotnet/core-nightly/aspnet:3.0.0-preview4-stretch-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core-nightly/sdk:3.0.100-preview4-stretch AS build
WORKDIR /src
COPY ["IntegratedAuthSample.csproj", ""]
RUN dotnet restore "IntegratedAuthSample.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "IntegratedAuthSample.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "IntegratedAuthSample.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "IntegratedAuthSample.dll"]
