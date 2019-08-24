# Templates

## Getting Started
These are project templates which are used in .NET Core for creating ASP.NET Core applications.

## Prerequisites
Some projects in this repository (like SignalR Java Client) require JDK installation and configuration of `JAVA_HOME` environment variable.
1. If you don't have the JDK installed, you can find it from https://www.oracle.com/technetwork/java/javase/downloads/index.html
1. After installation define a new environment variable named `JAVA_HOME` pointing to the root of the latest JDK installation (for Windows it will be something like `c:\Program Files\Java\jdk-12`).
1. Add the `%JAVA_HOME%\bin` directory to the `PATH` environment variable

## Building Templates
1. Run `. .\activate.ps1` if you haven't already.

1. Run `git submodule update --init --recursive` if you haven't already.
1. Run `git submodule update` to update submodules.
1. Run `build.cmd -all -pack -configuration Release` in the repository root to build all of the dependencies.
1. Run `build.cmd -pack -NoRestore -NoBuilddeps -configuration Release` in this directory will produce NuGet packages for each class of template in the artifacts directory.
1. Because the templates build against the version of `Microsoft.AspNetCore.App` that was built during the previous step, it is NOT advised that you install templates created on your local machine via `dotnet new -i [nupkgPath]`. Instead, use the `Run-[Template]-Locally.ps1` scripts in the script folder. These scripts do `dotnet new -i` with your packages, but also apply a series of fixes and tweaks to the created template which keep the fact that you don't have a production `Microsoft.AspNetCore.App` from interfering.
1. The ASP.NET localhost development certificate must also be installed and trusted or else you'll get a test error "Certificate error: Navigation blocked".
1. Run `.\build.cmd -test -NoRestore -NoBuild -NoBuilddeps -configuration Release "/p:RunTemplateTests=true"` to run template tests.

** Note** Templating tests require Visual Studio unless a full build (CI) is performed.
