Launch any command with dotnet-watch
====================================

## Prerequisites

1. Install .NET Core command line. <https://dot.net/core>
2. Install NodeJS. <https://nodejs.org>

## Usage

Open a terminal to the directory containing this project.

```
dotnet restore
dotnet watch msbuild /t:RunMyNpmCommand
```

Changing the .csproj file, or the say-hello.js file will cause dotnet-watch to re-run the 'RunMyNpmCommand' target in MyApp.csproj.
