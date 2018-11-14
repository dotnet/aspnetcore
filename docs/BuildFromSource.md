Build ASP.NET Core from Source
==============================

Building ASP.NET Core from source allows you tweak and customize ASP.NET Core, and
to contribute your improvements back to the project.

## :warning: Temporary instructions

We are currently in the middle of restructing our repositories. While this work is being done, the following instructions will help you be more productive while working on this repo.

1. Before opening a solution, run `build.cmd /p:_ProjectsOnly=true /p:SkipTests=true`. This will only build the projects which have merged into this repo, not the git submodules.
2. Use (or create) a solution which is scoped to your project file. The build system does not use .sln files. These only exist for developer productivity in Visual Studio, so feel free to adjust the projects in .sln files to match your workload.
3. Questions? Contact @aspnet for help.

## Install pre-requistes

### Windows

Building ASP.NET Core on Windows requires:

* Windows 7 or higher
* At least 10 GB of disk space and a good internet connection (our build scripts download a lot of tools and dependencies)
* Visual Studio 2017. <https://visualstudio.com>
* Git. <https://git-scm.org>
* (Optional) some optional components, like the SignalR Java client, may require
    * NodeJS <https://nodejs.org>
    * Java Development Kit 10 or newer. Either:
        * OpenJDK <http://jdk.java.net/10/>
        * Oracle's JDK <https://www.oracle.com/technetwork/java/javase/downloads/index.html>

### macOS/Linux

Building ASP.NET Core on macOS or Linux requires:

* If using macOS, you need macOS Sierra or newer.
* If using Linux, you need a machine with all .NET Core Linux prerequisites: <https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites>
* At least 10 GB of disk space and a good internet connection (our build scripts download a lot of tools and dependencies)
* Git <https://git-scm.org>
* (Optional) some optional components, like the SignalR Java client, may require
    * NodeJS  <https://nodejs.org>
    * Java Development Kit 10 or newer. Either:
        * OpenJDK <http://jdk.java.net/10/>
        * Oracle's JDK <https://www.oracle.com/technetwork/java/javase/downloads/index.html>

## Clone the source code

ASP.NET Core uses git submodules to include source from a few other projects.

For a new copy of the project, run:
```
git clone --recursive https://github.com/aspnet/AspNetCore
```

To update an existing copy, run:
```
git submodule update --init --recursive
```

## Building in Visual Studio / Code

Before opening our .sln files in Visual Studio or VS Code, executing the following on command-line:
```
.\build.cmd /t:Restore
```
This will download required tools.

#### PATH

For VS Code and Visual Studio to work correctly, you must place the following location in your PATH.
```
Windows: %USERPROFILE%\.dotnet\x64
Linux/macOS: $HOME/.dotnet
```
This must come **before** any other installation of `dotnet`. In Windows, we recommend removing `C:\Program Files\dotnet` from PATH in system variables and adding `%USERPROFILE%\.dotnet\x64` to PATH in user variables.

<img src="http://i.imgur.com/Tm2PAfy.png" width="400" />

## Building on command-line

You can also build the entire project on command line with the `build.cmd`/`.sh` scripts.

On Windows:
```
.\build.cmd
```

On macOS/Linux:
```
./build.sh
```

#### Build properties

Additional properties can be added as an argument in the form `/property:$name=$value`, or `/p:$name=$value` for short. For example:
```
.\build.cmd /p:Configuration=Release
```

Common properties include:

Property                 | Description
-------------------------|---------------------------------------------------------
BuildNumber              | (string). A specific build number, typically from a CI counter
Configuration            | `Debug` or `Release`. Default = `Debug`.
SkipTests                | `true` or `false`. When true, builds without running tests.
NoBuild                  | `true` or `false`. Runs tests without rebuilding.

## Use the result of your build

After building ASP.NET Core from source, you will need to install and use your local version of ASP.NET Core.

- Run the installers produced in `artifacts/installers/` for your platform.
- Add a NuGet.Config to your project directory with the following content:

  ```xml
  <?xml version="1.0" encoding="utf-8"?>
  <configuration>
      <packageSources>
          <clear />
          <add key="MyBuildOfAspNetCore" value="C:\src\aspnet\AspNetCore\artifacts\build\" />
          <add key="NuGet.org" value="https://api.nuget.org/v3/index.json" />
      </packageSources>
  </configuration>
  ```

  *NOTE: This NuGet.Config should be with your application unless you want nightly packages to potentially start being restored for other apps on the machine.*

- Update the versions on `PackageReference` items in your .csproj project file to point to the version from your local build.
  ```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Mvc" Version="3.0.0-alpha1-t000" />
  </ItemGroup>
  ```


Some features, such as new target frameworks, may require prerelease tooling builds for Visual Studio.
These are available in the [Visual Studio Preview](https://www.visualstudio.com/vs/preview/).
