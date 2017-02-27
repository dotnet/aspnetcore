DotNetTools
===========

[![Travis build status](https://img.shields.io/travis/aspnet/DotNetTools.svg?label=travis-ci&branch=dev&style=flat-square)](https://travis-ci.org/aspnet/DotNetTools/branches)
[![AppVeyor build status](https://img.shields.io/appveyor/ci/aspnetci/DotNetTools/dev.svg?label=appveyor&style=flat-square)](https://ci.appveyor.com/project/aspnetci/DotNetTools/branch/dev)

This project is part of ASP.NET Core. You can find samples, documentation and getting started instructions for ASP.NET Core at <https://docs.asp.net>.

## Projects

The repository contains command-line tools for the .NET Core CLI. Follow the links below for more details on each tool.

 - [dotnet-watch](src/Microsoft.DotNet.Watcher.Tools/) (Microsoft.DotNet.Watcher.Tools)
 - [dotnet-user-secrets](src/Microsoft.Extensions.SecretManager.Tools/) (Microsoft.Extensions.SecretManager.Tools)
 - [dotnet-sql-cache](src/Microsoft.Extensions.Caching.SqlConfig.Tools/) (Microsoft.Extensions.Caching.SqlConfig.Tools)

## How to Install

Install tools by editing your \*.csproj file and adding a `DotNetCliToolReference` with the package name and version.

```xml
  <ItemGroup>
    <DotNetCliToolReference Include="Microsoft.DotNet.Watcher.Tools" Version="1.0.0" />
    <DotNetCliToolReference Include="Microsoft.Extensions.SecretManager.Tools" Version="1.0.0" />
    <DotNetCliToolReference Include="Microsoft.Extensions.Caching.SqlConfig.Tools" Version="1.0.0" />
  </ItemGroup>
```

Then, from command line, change directories to your project and run the following commands:

```sh
# Location of MyProject.csproj which includes DotNetCliToolReference's
cd C:\Source\MyProject\

# Download tools into the project
dotnet restore

# Execute tools
dotnet watch
dotnet user-secrets
dotnet sql-cache
```
