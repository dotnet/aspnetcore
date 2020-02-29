# Build ASP.NET Core from Source

Building ASP.NET Core from source allows you to tweak and customize ASP.NET Core, and to contribute your improvements back to the project.

See <https://github.com/dotnet/aspnetcore/labels/area-infrastructure> for known issues and to track ongoing work.

## Install pre-requisites

### Windows

Building ASP.NET Core on Windows requires:

* Windows 10, version 1803 or newer
* At least 10 GB of disk space and a good internet connection (our build scripts download a lot of tools and dependencies)
* Visual Studio 2019. <https://visualstudio.com>
  * To install the exact required components, run [eng/scripts/InstallVisualStudio.ps1](/eng/scripts/InstallVisualStudio.ps1).

    ```ps1
    PS> ./eng/scripts/InstallVisualStudio.ps1
    ```

    However, any Visual Studio 2019 instance that meets the requirements should be fine. See [global.json](/global.json)
    and [eng/scripts/vs.json](/eng/scripts/vs.json) for those requirements. By default, the script will install Visual Studio Enterprise Edition, however you can use a different edition by passing the `-Edition` flag.
* Git. <https://git-scm.org>
* NodeJS. LTS version of 10.14.2 or newer <https://nodejs.org>
* Java Development Kit 11 or newer. Either:
  * OpenJDK <https://jdk.java.net/>
  * Oracle's JDK <https://www.oracle.com/technetwork/java/javase/downloads/index.html>
  * To install a version of the JDK that will only be used by this repo, run [eng/scripts/InstallJdk.ps1](/eng/scripts/InstallJdk.ps1)

    ```ps1
    PS> ./eng/scripts/InstallJdk.ps1
    ```

    However, the build should find any JDK 11 or newer installation on the machine.
* Chrome - Selenium-based tests require a version of Chrome to be installed. Download and install it from <https://www.google.com/chrome>

### macOS/Linux

Building ASP.NET Core on macOS or Linux requires:

* If using macOS, you need macOS Sierra or newer.
* If using Linux, you need a machine with all .NET Core Linux prerequisites: <https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites>
* At least 10 GB of disk space and a good internet connection (our build scripts download a lot of tools and dependencies)
* Git <https://git-scm.org>
* NodeJS. LTS version of 10.14.2 or newer <https://nodejs.org>
* Java Development Kit 11 or newer. Either:
  * OpenJDK <https://jdk.java.net/>
  * Oracle's JDK <https://www.oracle.com/technetwork/java/javase/downloads/index.html>

## Clone the source code

ASP.NET Core uses git submodules to include the source from a few other projects.

For a new copy of the project, run:

```ps1
git clone --recursive https://github.com/dotnet/aspnetcore
```

To update an existing copy, run:

```ps1
git submodule update --init --recursive
```

## Building in Visual Studio

Before opening our .sln files in Visual Studio or VS Code, you need to perform the following actions.

1. Executing the following on command-line:

   ```ps1
   .\restore.cmd
   ```

   This will download the required tools and build the entire repository once. At that point, you should be able to open .sln files to work on the projects you care about.

   > :bulb: Pro tip: you will also want to run this command after pulling large sets of changes. On the master branch, we regularly update the versions of .NET Core SDK required to build the repo.
   > You will need to restart Visual Studio every time we update the .NET Core SDK.

2. Use the `startvs.cmd` script to open Visual Studio .sln files. This script first sets the required environment variables.

### Solution files

We don't have a single .sln file for all of ASP.NET Core because Visual Studio doesn't currently handle projects of this scale.
Instead, we have many .sln files which include a sub-set of projects. These principles guide how we create and manage .sln files:

1. Solution files are not used by CI or command line build scripts. They are meant for use by developers only.
2. Solution files group together projects which are frequently edited at the same time.
3. Can't find a solution that has the projects you care about? Feel free to make a PR to add a new .sln file.

> :bulb: Pro tip: `dotnet new sln` and `dotnet sln` are one of the easiest ways to create and modify solutions.

### Common error: CS0006

Opening solution files and building may produce an error code CS0006 with a message such

> Error CS0006 Metadata file 'C:\src\aspnet\AspNetCore\artifacts\bin\Microsoft.AspNetCore.Metadata\Debug\netstandard2.0\Microsoft.AspNetCore.Metadata.dll' could not be found

The cause of this problem is that the solution you are using does not include the project that produces this .dll. This most often occurs after we have added new projects to the repo, but failed to update our .sln files to include the new project. In some cases, it is sometimes the intended behavior of the .sln which has been crafted to only include a subset of projects.

#### You can fix this in one of two ways

1. Build the project on command line. In most cases, running `build.cmd` on command line solves this problem.
2. Update the solution to include the missing project. You can either do this one by one using `dotnet sln`

   ```ps1
   dotnet sln add C:\src\AspNetCore\src\Hosting\Abstractions\src\Microsoft.AspNetCore.Hosting.Abstractions.csproj
   ```

### Common error: Unable to locate the .NET Core SDK

Executing `.\restore.cmd` or `.\build.cmd` may produce these errors:

> error : Unable to locate the .NET Core SDK. Check that it is installed and that the version specified in global.json (if any) matches the installed version.
> error MSB4236: The SDK 'Microsoft.NET.Sdk' specified could not be found.

In most cases, this is because the option _Use previews of the .NET Core SDK_ in VS2019 is not checked. Start Visual Studio, go to _Tools > Options_ and check _Use previews of the .NET Core SDK_ under _Environment > Preview Features_.

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

```bash
source activate.sh
code .
```

Note that if you are using the "Remote-WSL" extension in VSCode, the environment is not supplied
to the process in WSL.  You can workaround this by explicitly setting the environment variables
in `~/.vscode-server/server-env-setup`.
See https://code.visualstudio.com/docs/remote/wsl#_advanced-environment-setup-script for details.

## Building on command-line

You can also build the entire project on command line with the `build.cmd`/`.sh` scripts.

On Windows:

```ps1
.\build.cmd
```

On macOS/Linux:

```bash
./build.sh
```

By default, all of the C# projects are built. Some C# projects require NodeJS to be installed to compile JavaScript assets which are then checked in as source. If NodeJS is detected on the path, the NodeJS projects will be compiled as part of building C# projects. If NodeJS is not detected on the path, the JavaScript assets checked in previously will be used instead. To disable building NodeJS projects, specify /p:BuildNodeJs=false on the command line.

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

```ps1
.\build.cmd -test
```

On macOS/Linux:

```bash
./build.sh --test
```

## Building a subset of the code

This repository is large. Look for `build.cmd`/`.sh` scripts in subfolders. These scripts can be used to invoke build and test on a smaller set of projects.

Furthermore, you can use flags on `build.cmd`/`.sh` to build subsets based on language type, like C++, TypeScript, or C#. Run `build.sh --help` or `build.cmd -help` for details.

## Build properties

Additional properties can be added as an argument in the form `/property:$name=$value`, or `/p:$name=$value` for short. For example:

```ps1
.\build.cmd /p:Configuration=Release
```

Common properties include:

Property                 | Description
-------------------------|-------------------------------------------------------------------------------------------------------------
Configuration            | `Debug` or `Release`. Default = `Debug`.
TargetArchitecture       | The CPU architecture to build for (x64, x86, arm, arm64).
TargetOsName             | The base runtime identifier to build for (win, linux, osx, linux-musl).

## Use the result of your build

After building ASP.NET Core from source, you will need to install and use your local version of ASP.NET Core.
See ["Artifacts"](./Artifacts.md) for more explanation of the different folders produced by a build.

* Run the installers produced in `artifacts/installers/{Debug, Release}/` for your platform.
* Add a NuGet.Config to your project directory with the following content:

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

* Update the versions on `PackageReference` items in your .csproj project file to point to the version from your local build.

  ```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.SpaServices" Version="3.0.0-dev" />
  </ItemGroup>
  ```

Some features, such as new target frameworks, may require prerelease tooling builds for Visual Studio.
These are available in the [Visual Studio Preview](https://www.visualstudio.com/vs/preview/).

## Resx files

If you need to make changes to a .resx file, run `dotnet msbuild /t:Resx <path to csproj>`. This will update the generated C#.
