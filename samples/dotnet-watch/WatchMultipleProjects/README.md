Watch multiple projects with dotnet-watch
=========================================

## Prerequisites

Install .NET Core command line. <https://dot.net/core>

## Usage

Open a terminal to the directory containing this project.

```
dotnet restore watch.proj
dotnet watch msbuild watch.proj /t:TestAndRun
```

The "TestAndRun" target in watch.proj will execute "dotnet test" on Test.csproj and then launch the website by calling "dotnet run" on Web.csproj.

Changing any \*.cs file in Test/ or Web/, any \*.csproj file, or watch.proj, will cause dotnet-watch to relaunch the "TestAndRun" target from watch.proj.
