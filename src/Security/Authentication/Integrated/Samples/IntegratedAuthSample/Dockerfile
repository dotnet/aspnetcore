FROM microsoft/dotnet:3.0-aspnetcore-runtime-stretch-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM microsoft/dotnet:3.0-sdk-stretch AS build
WORKDIR /src
COPY ["Authentication/Integrated/Samples/IntegratedAuthSample/IntegratedAuthSample.csproj", "Authentication/Integrated/Samples/IntegratedAuthSample/"]
RUN dotnet restore "Authentication/Integrated/Samples/IntegratedAuthSample/IntegratedAuthSample.csproj"
COPY . .
WORKDIR "/src/Authentication/Integrated/Samples/IntegratedAuthSample"
RUN dotnet build "IntegratedAuthSample.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "IntegratedAuthSample.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "IntegratedAuthSample.dll"]