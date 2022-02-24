# Build ASP.NET Core from Source

This document outlines how to build the source in the `aspnetcore` repo locally for development purposes.

For more info on issues related to build infrastructure and ongoing work, see <https://github.com/dotnet/aspnetcore/labels/area-infrastructure>.

## Step 0: Getting started

This tutorial assumes that you are familiar with:

- Git
- The basics of [forking and contributing to GitHub projects](https://guides.github.com/activities/forking/)
- Command line fundamentals in your operating system of choice

## Step 1: Getting the source code

Development is done in your own repo, not directly against the official `dotnet/aspnetcore` repo. To create your own fork, click the __Fork__ button from our GitHub repo as a signed-in user and your own fork will be created.

> :bulb: All other steps below will be against your fork of the aspnetcore repo (e.g. `YOUR_USERNAME/aspnetcore`), not the official `dotnet/aspnetcore` repo.

### Cloning your repo locally

ASP.NET Core uses git submodules to include the source from a few other projects. In order to pull the sources of the these submodules when cloning the repo, be sure to pass the `--recursive` flag to the `git clone` command.

```powershell
git clone --recursive https://github.com/YOUR_USERNAME/aspnetcore
```

If you've already cloned the `aspnetcore` repo without fetching submodule sources, you can fetch them after cloning by running the following command.

```powershell
git submodule update --init --recursive
```

> :bulb: Some ISPs have been known to use web filtering software that has caused issues with git repository cloning, if you experience issues cloning this repo please review <https://help.github.com/en/github/authenticating-to-github/using-ssh-over-the-https-port>.

### Tracking remote changes

The first time you clone your repo locally, you'll want to set an additional Git remote back to the official repo so that you can periodically refresh your repo with the latest official changes.

```powershell
git remote add upstream https://github.com/dotnet/aspnetcore.git
```

You can verify the `upstream` remote has been set correctly.

```powershell
git remote -v
> origin    https://github.com/YOUR_USERNAME/aspnetcore (fetch)
> origin    https://github.com/YOUR_USERNAME/aspnetcore (push)
> upstream  https://github.com/dotnet/aspnetcore.git (fetch)
> upstream  https://github.com/dotnet/aspnetcore.git (push)
```

Once configured, the easiest way to keep your repository current with the upstream repository is using GitHub's feature to [fetch upstream changes](https://github.blog/changelog/2021-05-06-sync-an-out-of-date-branch-of-a-fork-from-the-web/).

### Branching

If you ultimately want to be able to submit a PR back to the project or be able to periodically refresh your `main` branch with the latest code changes, you'll want to do all your work on a new branch.

```powershell
git checkout -b NEW_BRANCH
```

## Step 2: Install pre-requisites

Developing in the `aspnetcore` repo requires some additional tools to build the source code and run integration tests.

### On Windows

Building ASP.NET Core on Windows (10, version 1803 or newer) requires that you have the following tooling installed.

> :bulb: Be sure you have least 10 GB of disk space and a good Internet connection. The build scripts will download several tools and dependencies onto your machine.

#### [Visual Studio 2019](https://visualstudio.com)

Visual Studio 2019 (16.10 Preview 3) is required to build the repo locally. If you don't have visual studio installed you can run [eng/scripts/InstallVisualStudio.ps1](/eng/scripts/InstallVisualStudio.ps1) to install the exact required dependencies.

> :bulb: By default, the script will install Visual Studio Enterprise Edition, however you can use a different edition by passing the `-Edition` flag.
> :bulb: To install Visual Studio from the preview channel, you can use the `-Channel` flag to set the channel (`-Channel Preview`).
> :bulb: Even if you have installed Visual Studio, we still recommend using this script to install again to avoid errors due to missing components.

```powershell
./eng/scripts/InstallVisualStudio.ps1  [-Edition {Enterprise|Community|Professional}] [-Channel {Release|Preview}]
```

> :bulb: To execute the setup script or other PowerShell scripts in the repo, you may need to update the execution policy on your machine.
> You can do so by running the `Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser` command
> in PowerShell. For more information on execution policies, you can read the [execution policy docs](https://docs.microsoft.com/powershell/module/microsoft.powershell.security/set-executionpolicy).

The [global.json](/global.json) file specifies the minimum requirements needed to build using `msbuild`. The [eng/scripts/vs.16.json](/eng/scripts/vs.16.json) file provides a description of the components needed to build within VS. If you plan on developing in Visual Studio, you will need to have these components installed.

> :bulb: The `InstallVisualStudio.ps1` script mentioned above reads from the `vs.17.json` file to determine what components to install.

#### [Git](https://git-scm.org) on Windows

If you're reading this, you probably already have Git installed to support cloning the repo as outlined in Step 1.

#### [NodeJS](https://nodejs.org) on Windows

Building the repo requires version 16.11.0 or newer of Node. You can find installation executables for Node at <https://nodejs.org>.

#### [Yarn](https://yarnpkg.com/) on Windows

NodeJS installs the Node package manager (npm) by default. This repo depends on Yarn, an alternate package manager for the Node ecosystem. You can install Yarn from the command line using the following command.

```powershell
npm install -g yarn
```

#### [tar](http://gnuwin32.sourceforge.net/packages/gtar.htm) on Windows

Building the repo requires tar to be installed. First, check whether `tar.exe` is already in your path i.e. execute `tar -help` (Win10 comes with tar already installed). Then, assuming you have `git` installed, you might add `C:\Program Files\Git\usr\bin\` to your path to pick up the `tar.exe` that ships with `git`. Finally, you can find installation executables of tar at <http://gnuwin32.sourceforge.net/packages/gtar.htm>; download that and add the installation directory to your PATH variable.

#### Java Development Kit on Windows

This repo contains some Java source code that depends on an install of the JDK v11 or newer. The JDK can be installed from either:

- OpenJDK <https://jdk.java.net/>
- Oracle's JDK <https://www.oracle.com/technetwork/java/javase/downloads/index.html>

Alternatively, you can run [eng/scripts/InstallJdk.ps1](/eng/scripts/InstallJdk.ps1) to install a version of the JDK that will only be used in this repo.

```powershell
./eng/scripts/InstallJdk.ps1
```
NOTE : In order to execute the script you may need to set the right Execution policy. If not you may get an error that the script "cannot be loaded because the execution of scripts is disabled on this system". To get past that you can do the following. As an Administrator, you can set the execution policy by typing this into your PowerShell window:

```powershell
Set-ExecutionPolicy RemoteSigned
```

And when you are finished with the script return the execution policy.

```powershell
Set-ExecutionPolicy Restricted
```

The build should find any JDK 11 or newer installation on the machine as long as the `JAVA_HOME` environment variable is set. Typically, your installation will do this automatically. However, if it is not set you can set the environment variable manually:

- Set `JAVA_HOME` to `RepoRoot/.tools/jdk/win-x64/` if you used the `InstallJdk.ps1` script.
- Set `JAVA_HOME` to `C:/Program Files/Java/jdk<version>/` if you installed the JDK globally.

You can temporarily set your environmental variable for the scope of the active powershell session using the command

- $env:JAVA_HOME = "C:/[RepoRoot]/.tools/jdk/win-x64/"

#### Chrome

This repo contains a Selenium-based tests require a version of Chrome to be installed. Download and install it from <https://www.google.com/chrome>.

#### Visual Studio Code Extension

The following extensions are recommended when developing in the ASP.NET Core repository with Visual Studio Code.

- [C# extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)

- [EditorConfig](https://marketplace.visualstudio.com/items?itemName=EditorConfig.EditorConfig)

- [C# XML Documentation Comments](https://marketplace.visualstudio.com/items?itemName=k--kato.docomment)

#### WiX (Optional)

If you plan on working with the Windows installers defined in [src/Installers/Windows](../src/Installers/Windows), you will need to install the WiX toolkit from <https://wixtoolset.org/releases/>.

### On macOS/Linux

You can also build ASP.NET Core on macOS or Linux. macOS Sierra or newer is required if you're building on macOS. If you're building on Linux, your machine will need to meet the [.NET Core Linux prerequisites](https://docs.microsoft.com/dotnet/core/linux-prerequisites).

> :bulb: Be sure you have least 10 GB of disk space and a good Internet connection. The build scripts will download several tools and dependencies onto your machine.

#### curl/wget

`curl` and `wget` are command line tools that can be used to download files from an HTTP server. Either utility will need to be installed in order to complete the setup. Typically, these will be included on your machine by default.

If neither utility is installed, you can install curl (<https://curl.haxx.se>) or wget (<https://www.gnu.org/software/wget>).

#### Git

If you've made it this far, you've already got `Git` installed. Sit back, relax, and move on to the next requirement.

#### [NodeJS](https://nodejs.org)

Building the repo requires version 14.17.6 or newer of Node. You can find installation executables for Node at <https://nodejs.org>.

#### [Yarn](https://yarnpkg.com/)

NodeJS installs the Node package manager (npm) by default. This repo depends on Yarn, an alternate package manager for the Node ecosystem. You can install Yarn from the command line using the following command.

```bash
npm install -g yarn
```

#### Java Development Kit

This repo contains some Java source code that depends on an install of the JDK v11 or newer. The JDK can be installed from either:

- OpenJDK <https://jdk.java.net/>
- Oracle's JDK <https://www.oracle.com/technetwork/java/javase/downloads/index.html>

Similar to [the instructions above for Windows](#java-development-kit-in-windows), be sure that the the `JAVA_HOME` environment variable is set to the location of your Java installation.

## Step 3: Build the repo

Before opening our .sln/.slnf files in Visual Studio or VS Code, you will need to at least restore the repo locally.

### In Visual Studio

To set up your project for development on Visual Studio, you'll need to execute the following command. Building
subsets of the repo e.g. Java projects may (depending on your scenario) be necessary before building within Visual
Studio because those projects are not listed in AspNetCore.sln.

```powershell
.\restore.cmd
```

> :bulb: If you happen to be working on macOS or Linux, you can use the `restore.sh` command.

This will download the required tools and restore all projects inside the repository. At that point, you should be able
to open the .sln file or one of the project specific .slnf files to work on the projects you care about.


> :bulb: Pro tip: you will also want to run this command after pulling large sets of changes. On the main
> branch, we regularly update the versions of .NET Core SDK required to build the repo.
> You will need to restart Visual Studio every time we update the .NET Core SDK.


> :bulb: Rerunning the above command or, perhaps, the quicker `.\build.cmd -noBuildNative -noBuildManaged` may be
> necessary after switching branches, especially if the `$(DefaultNetCoreTargetFramework)` value changes.

Typically, you want to focus on a single project within this large repo. For example,
if you want to work on Blazor WebAssembly, you'll need to launch the solution file for that project by changing into the `src/Components`
directory and executing `startvs.cmd` in that directory like so:

```powershell
cd src\Components
.\startvs.cmd
```

After opening the solution in Visual Studio, you can build/rebuild using the controls in Visual Studio.

#### A brief interlude on solution files

We have a single .sln file for all of ASP.NET Core, but most people don't work with it directly because Visual Studio
doesn't currently handle projects of this scale very well.

Instead, we have many Solution Filter (.slnf) files which include a sub-set of projects. See the Visual Studio
documentation [here](https://docs.microsoft.com/visualstudio/ide/filtered-solutions) for more
information about Solution Filters.

These principles guide how we create and manage .slnf files:

1. Solution files are not used by CI or command line build scripts. They are meant for use by developers only.
2. Solution files group together projects which are frequently edited at the same time.
3. Can't find a solution that has the projects you care about? Feel free to make a PR to add a new .slnf file.

### In Visual Studio Code

Before opening the project in Visual Studio Code, you will need to make sure that you have built the project.
You can find more info on this in the "Building on command-line" section below.

To open specific folder inside Visual studio code, you have to open it with `startvscode.cmd` file. Ths will setup neccessary environment variables and will open given directory in Visual Studio Code.

Using Visual Studio Code with this repo requires setting environment variables on command line first.
Use these command to launch VS Code with the right settings.

> :bulb: Note that you'll need to launch Visual Studio Code from the command line in order to ensure that it picks up the environment variables. To learn more about the Visual Studio Code CLI, you can check out [the docs page](https://code.visualstudio.com/docs/editor/command-line).

On Windows (requires PowerShell):

```powershell
# The extra dot at the beginning is required to 'dot source' this file into the right scope.
. .\activate.ps1
code .
```

On macOS/Linux:

```bash
source activate.sh
code .
```

> :bulb: Note that if you are using the "Remote-WSL" extension in VSCode, the environment is not supplied
> to the process in WSL. You can workaround this by explicitly setting the environment variables
> in `~/.vscode-server/server-env-setup`.
> See <https://code.visualstudio.com/docs/remote/wsl#_advanced-environment-setup-script> for details.

In Visual Studio Code, press `CTRL + SHIFT + P` (`CMD + SHIFT + P` on mac) to open command palette, then search and select for `Run Tasks` option. In task list, there are couple of most used tasks are already defined, in that you can select `Build entire repository` option from it to build the repository. Once you select that option, on next window you need to select configuration type from `Debug` OR `Release`. For development purpose one can go with `Debug` option and for actual testing one can choose `Release` mode as binaries will be optimized in this mode.

### Building on command-line

When developing in VS Code, you'll need to use the `build.cmd` or `build.sh` scripts in order to build the project. You can learn more about the command line options available, check out [the section below](#using-dotnet-on-command-line-in-this-repo).

> :warning: Most of the time, you will want to build a particular project instead of the entire repository. It's faster and will allow you to focus on a particular area of concern. If you need to build all code in the repo for any reason, you can use the top-level build script located under `eng\build.cmd` or `eng\build.sh`.

The source code in this repo is divided into directories for each project area. Each directory will typically contain a `src` directory that contains the source files for a project and a `test` directory that contains the test projects and assets within a project.

Some projects, like the `Components` project or the `Razor` project, might contain additional subdirectories.

To build a code change associated with a modification, run the build script in the directory closest to the modified file. For example, if you've modified `src/Components/WebAssembly/Server/src/WebAssemblyNetDebugProxyAppBuilderExtensions.cs` then run the build script located in `src/Components`.

On Windows, you can run the command script:

```powershell
.\eng\build.cmd
```

On macOS/Linux, you can run the shell script:

```bash
./build.sh
```

> :bulb: Before using the `.\eng\build.cmd` or `.\eng\build.sh` at the top-level or in a subfolder, you will need to make sure that [the dependencies documented above](#step-2-install-pre-requisites) have been installed.

By default, all of the C# projects are built. Some C# projects require NodeJS to be installed to compile JavaScript assets which are then checked in as source. If NodeJS is detected on the path, the NodeJS projects will be compiled as part of building C# projects. If NodeJS is not detected on the path, the JavaScript assets checked in previously will be used instead. To disable building NodeJS projects, specify `-noBuildNodeJS` or `--no-build-nodejs` on the command line.

## Step 4: Make your code changes

At this point, you will have all the dependencies installed and a code editor to up and running to make changes in. Once you've made changes, you will need to rebuild the project locally to pick up your changes. You'll also need to run tests locally to verify that your changes worked.

The section below provides some helpful guides for using the `dotnet` CLI in the ASP.NET Core repo.

### Using `dotnet` on command line in this repo

Because we are using pre-release versions of .NET Core, you have to set a handful of environment variables
to make the .NET Core command line tool work well. You can set these environment variables like this:

On Windows (requires PowerShell):

```powershell
# The extra dot at the beginning is required to 'dot source' this file into the right scope.
. .\activate.ps1
```

On macOS/Linux:

```bash
source ./activate.sh
```

> :bulb: Be sure to set the environment variables using the "activate" script above before executing the `dotnet` command inside the repo.

### Running tests on command-line

Tests are not run by default. When invoking a `build.cmd`/`build.sh` script, use the `-test` option to run tests in addition to building.

On Windows:

```powershell
.\build.cmd -test
```

On macOS/Linux:

```bash
./build.sh --test
```

> :bulb: If you're working on changes for a particular subset of the project, you might not want to execute the entire test suite. Instead, only run the tests within the subdirectory where changes were made. This can be accomplished by passing the `projects` property like so: `.\build.cmd -test -projects .\src\Framework\test\Microsoft.AspNetCore.App.UnitTests.csproj`.

### Build properties

Additional properties can be added as an argument in the form `/property:$name=$value`, or `/p:$name=$value` for short. For example:

```powershell
.\build.cmd -Configuration Release
```

Common properties include:

| Property           | Description                                                             |
| ------------------ | ----------------------------------------------------------------------- |
| Configuration      | `Debug` or `Release`. Default = `Debug`.                                |
| TargetArchitecture | The CPU architecture to build for (x64, x86, arm, arm64).               |
| TargetOsName       | The base runtime identifier to build for (win, linux, osx, linux-musl). |

### Resx files

After making changes to a .resx file, the updated strings and accessor methods will automatically be included in the output assembly when the project is next built.

## Step 5: Use the result of your build

After building ASP.NET Core from source, you will need to install and use your local version of ASP.NET Core.
See ["Artifacts"](./Artifacts.md) for more explanation of the different folders produced by a build.

Building installers does not run as part of `build.cmd` run without parameters, so you should opt-in for building them:

```powershell
.\eng\build.cmd -all -pack -arch x64
.\eng\build.cmd -all -pack -arch x86 -noBuildJava
.\eng\build.cmd -buildInstallers
```

_Note_: Additional build steps listed above aren't necessary on Linux or macOS.

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

  _NOTE: This NuGet.Config should be with your application unless you want nightly packages to potentially start being restored for other apps on the machine._

- Update the versions on `PackageReference` items in your .csproj project file to point to the version from your local build.

  ```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.SpaServices" Version="3.0.0-dev" />
  </ItemGroup>
  ```

Some features, such as new target frameworks, may require prerelease tooling builds for Visual Studio.
These are available in the [Visual Studio Preview](https://www.visualstudio.com/vs/preview/).

## Troubleshooting

See [BuildErrors](./BuildErrors.md) for a description of common issues you might run into while building the repo.
