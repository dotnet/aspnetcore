Build ASP.NET Core from Source
==============================

Building ASP.NET Core from source allows you tweak and customize ASP.NET Core, and to contribute your improvements back to the project.

See https://github.com/aspnet/AspNetCore/labels/area-infrastructure for known issues and to track ongoing work.

## Install pre-requistes

### Windows

Building ASP.NET Core on Windows requires:

* Windows 10
* At least 10 GB of disk space and a good internet connection (our build scripts download a lot of tools and dependencies)
* Visual Studio **2019 Preview**. <https://visualstudio.com>
    * To install the exact required components, run [eng/scripts/InstallVisualStudio.ps1](/eng/scripts/InstallVisualStudio.ps1).
        ```ps1
        PS> ./eng/scripts/InstallVisualStudio.ps1
        ```
* Git. <https://git-scm.org>
* (Optional) some optional components, like the SignalR Java client, may require
    * NodeJS. LTS version of 10.14.2 or newer recommended <https://nodejs.org>
    * Java Development Kit (JDK) v8 with Java Runtime Environment (JRE) v8. See https://www.oracle.com/technetwork/java/javase/downloads/index.html

### macOS/Linux

Building ASP.NET Core on macOS or Linux requires:

* If using macOS, you need macOS Sierra or newer.
* If using Linux, you need a machine with all .NET Core Linux prerequisites: <https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites>
* At least 10 GB of disk space and a good internet connection (our build scripts download a lot of tools and dependencies)
* Git <https://git-scm.org>
* (Optional) some optional components, like the SignalR Java client, may require
    * NodeJS. LTS version of 10.14.2 or newer recommended <https://nodejs.org>
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

## Building in Visual Studio

Before opening our .sln files in Visual Studio or VS Code, you need to perform the following actions.

1. Executing the following on command-line:
   ```
   .\restore.cmd
   ```
   This will download required tools and build the entire repository once. At that point, you should be able to open .sln files to work on the projects you care about.

   > :bulb: Pro tip: you will also want to run this command after pulling large sets of changes. On the master branch, we regularly update the versions of .NET Core SDK required to build the repo.
   > You will need to restart Visual Studio every time we update the .NET Core SDK.

2. Use the `startvs.cmd` script to open Visual Studio .sln files. This script first sets required environment variables.

### Solution files

We don't have a single .sln file for all of ASP.NET Core because Visual Studio doesn't currently handle projects of this scale.
Instead, we have many .sln files which include a sub-set of projects. These principles guide how we create and manage .slns:

1. Solution files are not used by CI or command line build scripts. They are for meant for use by developers only.
2. Solution files group together projects which are frequently edited at the same time.
3. Can't find a solution that has the projects you care about? Feel free to make a PR to add a new .sln file.

> :bulb: Pro tip: `dotnet new sln` and `dotnet sln` are one of the easiest ways to create and modify solutions.

### Known issue: NU1105

Opening solution files may produce an error code NU1105 with a message such

> Unable to find project information for 'C:\src\AspNetCore\src\Hosting\Abstractions\src\Microsoft.AspNetCore.Hosting.Abstractions.csproj'. Inside Visual Studio, this may be because the project is unloaded or not part of current solution. Otherwise the project file may be invalid or missing targets required for restore.

This is a known issue in NuGet (<https://github.com/NuGet/Home/issues/5820>) and we are working with them for a solution. See also <https://github.com/aspnet/AspNetCore/issues/4183> to track progress on this.

**The workaround** for now is to add all projects to the solution. You can either do this one by one using `dotnet sln`

    dotnet sln add C:\src\AspNetCore\src\Hosting\Abstractions\src\Microsoft.AspNetCore.Hosting.Abstractions.csproj

Or you can use this script to automatically traverse the project reference graph, which then invokes `dotnet sln` for you: [eng/scripts/AddAllProjectRefsToSolution.ps1](/eng/scripts/AddAllProjectRefsToSolution.ps1).

    ./eng/scripts/AddAllProjectRefsToSolution.ps1 -WorkingDir src/Mvc/

## Building with Visual Studio Code

Using Visual Studio Code with this repo requires setting environment variables on command line first.
Use these command to launch VS Code with the right settings.

On Windows (requires PowerShell):
```ps1
# The extra dot at the beginning is required to 'dot source' this file into the right scope.

. .\activate.ps1
code .
```

On macOS/Linux:
```
source activate.sh
code .
```

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

### Using `dotnet` on command line in this repo

Because we are using pre-release versions of .NET Core, you have to set a handful of environment variables
to make the .NET Core command line tool work well. You can set these environment variables like this

On Windows (requires PowerShell):

```ps1
# The extra dot at the beginning is required to 'dot source' this file into the right scope.

. .\activate.ps1
```

On macOS/Linux:
```bash
source ./activate.sh
```

## Running tests on command-line

Tests are not run by default. Use the `-test` option to run tests in addition to building.

On Windows:
```
.\build.cmd -test
```

On macOS/Linux:
```
./build.sh --test
```

## Building a subset of the code

This repository is large. Look for `build.cmd`/`.sh` scripts in subfolders. These scripts can be used to invoke build and test on a smaller set of projects.

Furthermore, you can use flags on `build.cmd`/`.sh` to build subsets based on language type, like C++, TypeScript, or C#. Run `build.sh --help` or `build.cmd -help` for details.

## Build properties

Additional properties can be added as an argument in the form `/property:$name=$value`, or `/p:$name=$value` for short. For example:
```
.\build.cmd /p:Configuration=Release
```

Common properties include:

Property                 | Description
-------------------------|-------------------------------------------------------------------------------------------------------------
BuildNumberSuffix        | (string). A specific build number, typically from a CI counter, which is appended to the pre-release label.
Configuration            | `Debug` or `Release`. Default = `Debug`.
TargetArchitecture       | The CPU architecture to build for (x64, x86, arm, arm64).
TargetOsName             | The base runtime identifier to build for (win, linux, osx, linux-musl).

## Use the result of your build

After building ASP.NET Core from source, you will need to install and use your local version of ASP.NET Core.
See ["Artifacts"](./Artifacts.md) for more explanation of the different folders produced by a build.

- Run the installers produced in `artifacts/installers/{Debug, Release}/` for your platform.
- Add a NuGet.Config to your project directory with the following content:

  ```xml
  <?xml version="1.0" encoding="utf-8"?>
  <configuration>
      <packageSources>
          <clear />
          <add key="MyBuildOfAspNetCore" value="C:\src\aspnet\AspNetCore\artifacts\packages\Debug\Shipping\" />
          <add key="NuGet.org" value="https://api.nuget.org/v3/index.json" />
      </packageSources>
  </configuration>
  ```

  *NOTE: This NuGet.Config should be with your application unless you want nightly packages to potentially start being restored for other apps on the machine.*

- Update the versions on `PackageReference` items in your .csproj project file to point to the version from your local build.
  ```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.SpaServices" Version="3.0.0-preview-0" />
  </ItemGroup>
  ```

Some features, such as new target frameworks, may require prerelease tooling builds for Visual Studio.
These are available in the [Visual Studio Preview](https://www.visualstudio.com/vs/preview/).

## Resx files

If you need to make changes to a .resx file, run `dotnet msbuild /t:Resx <path to csproj>`. This will update the generated C#.
