# Build the ASP.NET Core Repo

If you're reading these instructions, you're probably a contributor looking to understand how to build this repo locally on your machine so that you can build, debug, and test changes.

To get started, you'll need to have a fork of the repo cloned locally. This workflow assumes that you have [git installed on your development machine](https://git-scm.com/book/en/v2/Getting-Started-Installing-Git).

1. To create your own fork, click the **Fork** button from our GitHub repo as a signed-in user and your own fork will be created. For more information on managing forks, you can review the [GitHub docs on working with forks](https://docs.github.com/en/github/collaborating-with-pull-requests/working-with-forks).

2. Clone the repo locally using the `git clone` command. Since this repo contains submodules, you'll need to pass the `--recursive` flag to pull the sources for the submodules locally.

```
git clone --recursive https://github.com/YOUR_USERNAME/aspnetcore
```

> :bulb: All other steps below will be against your fork of the aspnetcore repo (e.g. `YOUR_USERNAME/aspnetcore`), not the official `dotnet/aspnetcore` repo.

> :bulb: If you've already cloned the repo without passing the `--recursive` flag, you can fetch submodule sources at any time using:
>
> ```bash
> git submodule update --init --recursive
> ```

The experience for building the repo is slightly different based on what environment you are looking to develop in. Select one of the links below to navigate to the instructions for your environment of choice.

- [Visual Studio on Windows](#visual-studio-on-windows)
- [VS Code (or other editors) on Windows, Linux, Mac](#visual-studio-code-on-windows-linux-mac)
- [Codespaces on GitHub](#codespaces-on-github)

## Visual Studio on Windows

1. Setting up the repo on Windows will require executing some scripts in PowerShell. You'll need to update the execution policy on your machine to support this. For more information on execution policies, review [the execution policy docs](https://docs.microsoft.com/powershell/module/microsoft.powershell.security/set-executionpolicy).

```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

2. In order to install Visual Studio on your machine, you can use the official installer script in the repo.

> ⚠️ Even if you have the appropriate Visual Studio installed on your machine, we recommend running this installation script so that the correct Visual Studio components are installed.

```powershell
./eng/scripts/InstallVisualStudio.ps1
```

3. Before opening the project in Visual Studio, run the `restore` script locally to install the required dependencies and setup the repo. The `restore` script is located in the root of the repo.

```powershell
.\restore.ps1
```

4. Typically, you'll want to focus on a single project within the repo. You can leverage the `startvs.cmd` command to launch Visual Studio within a particular project area. For example, to launch Visual Studio in the `Components` project.

```powershell
cd src\Components
.\startvs.cmd
```

<details>
 <summary>A brief interlude on Solution Files</summary>

We have a single .sln file for all of ASP.NET Core, but most people don't work with it directly because Visual Studio doesn't currently handle projects of this scale very well.

Instead, we have many Solution Filter (.slnf) files which include a sub-set of projects. For more information on solution files, you can review the [official Visual Studio doc](https://docs.microsoft.com/visualstudio/ide/filtered-solutions).

These principles guide how we create and manage .slnf files:
* Solution files are not used by CI or command line build scripts. They are meant for use by developers only.
* Solution files group together projects which are frequently edited at the same time.
* Can't find a solution that has the projects you care about? Feel free to make a PR to add a new .slnf file.

</details>

5. You can now build, debug, and test using Visual Studio. For more information on using Visual Studio to build and run projects, you can review the [official Visual Studio docs](https://docs.microsoft.com/en-us/visualstudio/get-started/csharp/run-program).

## Visual Studio Code on Windows, Linux, Mac

> :bulb: The instructions below use Visual Studio code as the editor of choice but the same instructions can be used for any other text editor by replacing the `code` command with an invocation to your editor of choice.

1. In order to use Visual Studio Code for development on this repo, you'll need to have [VS Code](https://code.visualstudio.com/) installed and [the `code` command installed](https://code.visualstudio.com/docs/setup/mac#_launching-from-the-command-line) on your machine.
2. The repo contains some JavaScript dependencies, so you will need to install [Node](https://nodejs.org/en/) and [yarn](https://yarnpkg.com/) on your machine.
3. Prior to opening the code in Visual Studio code, you'll need to run the `restore` script locally to install the required dotnet dependencies and setup the repo. The `restore` script is located in the root of the repo.

```bash
./restore.sh
```

```powershell
./restore.ps1
```

4. After the restore script has finished executing, activate the locally installed .NET by running the following command.

```bash
source activate.sh
```

```powershell
. ./activate.ps1
```

5. After activating the locally installed .NET, you can open your project of choice by running the `code` command in the directory of choice. For example, if you want to modify code in the `src/Http` project, you can use the following:

```bash
cd src/Http
code .
```

6. Once you've opened the project in VS Code, you can build and test changes by running the `./build.sh` command in the terminal.

> :bulb: The `build.sh` or `build.ps1` script will be local to the directory of the project that you have opened. For example, the script located in the `src/Http` directory.

```bash
./build.sh
./build.sh -test
```

7. Alternatively, you can use the `dotnet test` and `dotnet build` commands directly once you've activated the locally installed .NET SDK.

```bash
source activate.sh
dotnet build
dotnet test --filter "MySpecificUnitTest"
```

## Codespaces on GitHub

If you have [Codespaces enabled on your GitHub user account](https://github.com/codespaces), you can use Codespaces to make code changes in the repo using a cloud based editor environment.

1. Navigate to the fork and branch you would like to make code changes in. Note: if you have not created a new branch yet, do so using the GitHub UI or locally checking out and pushing a new branch.
2. Open a Codespace for your branch by navigating to the "Code" button > selecting the "Codespaces" tab > clicking the "New codespace" button.

![How to open a project in Codespaces](https://user-images.githubusercontent.com/1857993/136060792-6b4c6158-0a2c-4dd6-8639-08d83da6d2d1.png)

3. The Codespace will spend a few minutes building and initializating. Once this is done, you'll be able to navigate the Codespace in a web-based VS Code environment. You can use the `dotnet build` and `dotnet test` commands to build and test the repo. Note: you do not need to activate the locally installed .NET SDK or run the restore script. This is done as part of the initialization process.

---

### Troubleshooting

See [BuildErrors](https://github.com/dotnet/aspnetcore/blob/main/docs/BuildErrors.md) for a description of common issues you might run into while building the repo.

## A Guide to the Build Script

This ASP.NET Core repo contains a top-level build script located at `eng/build.cmd` and `eng/build.sh` and local build scripts within each directory. The scripts can be used to restore, build, and test the repo with support for a variety of flags. This section documents the common flags and some recommended invocation patterns.

> :warning: It is _not_ recommended to run the top-level build script for the repo. You'll rarely need to build the entire repo and building a particular sub-project is usually sufficient for your workflow.

### Common Arguments

Common arguments that can be invoked on the `build.cmd` or `build.sh` scripts include:

| Property           | Description                                                             |
| ------------------ | ----------------------------------------------------------------------- |
| Configuration      | `Debug` or `Release`. Default = `Debug`.                                |
| TargetArchitecture | The CPU architecture to build for (x64, x86, arm, arm64).               |
| TargetOsName       | The base runtime identifier to build for (win, linux, osx, linux-musl). |

### Common Invocations

| Command                              | What does it do?                                                                                          |
| ------------------------------------ | --------------------------------------------------------------------------------------------------------- |
| `.\build.cmd -Configuration Release` | Build projects in a subdirectory using a `Release` configuration. Can be run in any project subdirectory. |
| `.\build.cmd -test`                  | Run all unit tests in the current project. Can be run in any project subdirectory.                        |

### Repo-level Invocations

While it's typically better to use the project-specific build scripts, the repo-level build scripts located in the `eng` directory can also be used for project-specific invocations.

| Command                                                                                          | What does it do?                                                                                                                        |
| ------------------------------------------------------------------------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------- |
| `.\eng\build.cmd -all -pack -arch x64`                                                           | Build development packages for all the shipping projects in the repo. Must be run from the root of the repo.                            |
| `.\eng\build.cmd -test -projects .\src\Framework\test\Microsoft.AspNetCore.App.UnitTests.csproj` | Run all the unit tests in the `Microsoft.AspNetCore.App.UnitTests` project.                                                             |
| `.\eng\build.cmd -noBuildNative -noBuildManage`                                                  | Builds the repo and skips native and managed projects, a quicker alternative to `./restore.cmd`. Must be run from the root of the repo. |

## A Complete List of Repo Dependencies

To support building and testing all the projects in the repo, various dependencies need to be installed. Some dependencies are required regardless of what project area you want to work in. Other dependencies are optional depending on the project. Most required dependencies are installed automatically by the `restore` scripts, included by default in most modern operating systems, or installed automatically by the Visual Studio installer.

### Required Dependencies

| Dependency                                                              | Use                                                                                                                               |
| ----------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| [Git source control](https://git-scm.org/)                              | Used for cloning, branching, and other source control-related activities in the repo.                                             |
| [.NET](https://dotnet.microsoft.com/)                                   | A preview version of the .NET SDK is used for building projects within the repo. Installed automatically by the `restore` script. |
| [curl](https://curl.haxx.se/)/[wget](https://www.gnu.org/software/wget) | Used for downloading installation files and assets from the web.                                                                  |
| [tar](https://man7.org/linux/man-pages/man1/tar.1.html)                 | Used for unzipping installation assets. Included by default on macOS, Linux, and Windows 10 and above.                            |

### Optional Dependencies

| Dependency                                                   | Use                                                                                                                                                           | Notes                                                                                                                                               |
| ------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| [Selenium](https://www.selenium.dev/)                        | Used to run integration tests in the `Components` (aka Blazor) project.                                                                                       |                                                                                                                                                     |
| [Playwright](https://playwright.dev/)                        | Used to run template tests defined in the `ProjectTemplates` work.                                                                                            |                                                                                                                                                     |
| [Chrome](https://www.google.com/chrome/)                     | Required when running tests with Selenium or Playwright in the projects above. When using Playwright, the dependency is automatically installed.              |                                                                                                                                                     |
| [Java Development Kit (v11 or newer)](https://jdk.java.net/) | Required when building the Java SignalR client. Can be installed using the `./eng/scripts/InstallJdk.ps1` script on Windows.                                  | Ensure that the `JAVA_HOME` directory points to the installation and that the `PATH` has been updated to include the `$(jdkInstallDir)/bin` folder. |
| [Wix](https://wixtoolset.org/releases/)                      | Required when working with the Windows installers in the [Windows installers project](https://github.com/dotnet/aspnetcore/tree/main/src/Installers/Windows). |                                                                                                                                                     |
| [NodeJS](https://nodejs.org/en/)                             | Used for building JavaScript assets in the repo, such as those in Blazor and SignalR.                                                                         | Required a minimum version of the current NodeJS LTS.                                                                                               |
| [Yarn](https://yarnpkg.com/)                                 | Used for installing JavaScript dependencies in the repo, such as those in Blazor and SignalR.                                                                 |                                                                                                                                                     |
