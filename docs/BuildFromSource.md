# Build the ASP.NET Core repo

If you're reading these instructions, you're probably a contributor looking to understand how to build this repo locally on your machine so that you can build, debug, and test changes.

To get started, fork this repo and then clone it locally. This workflow assumes that you have [git installed on your development machine](https://git-scm.com/book/en/v2/Getting-Started-Installing-Git).

1. To create your own fork of the repo, sign in to GitHub and click the repo's **Fork** button. For more information on managing forks, you can review the [GitHub docs on working with forks](https://docs.github.com/en/github/collaborating-with-pull-requests/working-with-forks).

1. Clone the repo locally using the `git clone` command. Since this repo contains submodules, include the `--recursive` argument to pull the sources for the submodules locally.

    ```bash
    git clone --recursive https://github.com/YOUR_USERNAME/aspnetcore
    ```

    If you've already cloned the repo without passing the `--recursive` flag, fetch the submodule sources at any time with:

    ```bash
    git submodule update --init --recursive
    ```

    > :bulb: All other steps below will be against your fork of the aspnetcore repo (e.g. `YOUR_USERNAME/aspnetcore`), not the official `dotnet/aspnetcore` repo.

1. If you're on Windows, update the PowerShell execution policy on your machine. For more information on execution policies, review [the execution policy docs](https://learn.microsoft.com/powershell/module/microsoft.powershell.security/set-executionpolicy). To do this, open a PowerShell prompt and issue the following command:

    ```powershell
    Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
    ```

    > :warning: All Windows commands below assume a PowerShell prompt.

1. If you're on Windows, install Visual Studio (even if you aren't using it to build) to get the required C++ components and native tools. To install Visual Studio on your machine, use the official installer script in the repo.

    > :warning: Even if you have Visual Studio installed on your machine, we recommend running this installation script to make sure that the correct Visual Studio components are installed.
    >
    > To modify an existing Visual Studio installation, [follow the instructions for installing from a configuration file](https://learn.microsoft.com/visualstudio/install/import-export-installation-configurations#import-a-configuration) and use the `.vsconfig` file located in the root of the repository:

    ```powershell
    ./eng/scripts/InstallVisualStudio.ps1 Enterprise Preview
    ```

    Replace `Enterprise` with `Professional` or `Community` if that is your preferred Visual Studio edition.
    The preview channel is currently required as it supports the preview version of the SDK that is used.

    If you are seeing errors similar to `the imported project "....\aspnetcore.tools\msbuild\17.1.0\tools\MSBuild\Microsoft\VC\v170\Microsoft.Cpp.Default.props" was not found`, try installing/updating Visual Studio as above.

The steps you follow next depend on your preferred development environment:

- [Visual Studio on Windows](#visual-studio-on-windows)
- [Visual Studio Code (VS Code) or other editors on Windows, Linux, Mac](#visual-studio-code-or-other-editor-on-windows-linux-mac)
- [Codespaces on GitHub](#codespaces-on-github)

## Visual Studio on Windows

1. This repo has JavaScript dependencies, so you need [Node.js](https://nodejs.org/en/).

1. Before you open project in Visual Studio, install the required dependencies and set up the repo by running the `restore.cmd` script in the root of the repo:

    ```powershell
    ./restore.cmd
    ```

1. You'll typically focus on one project in the repo. You can use the `startvs.cmd` command to launch Visual Studio in a particular project area. For example, to launch Visual Studio in the `src/Http` project, after you have built it with `./build.cmd`:

    > :bulb: The `build.cmd` script will be local to the directory of the project you opened. For example, the script located in the `src/Http` directory. If you want to build the whole tree, use the `build.cmd` that is located in the `eng` directory.

    ```powershell
    cd src/Http
    ./build.cmd
    ./startvs.cmd
    ```

    <details>
     <summary>A brief interlude on Solution Files</summary>

    We have a single _.sln_ file for all of ASP.NET Core, but most people don't work with it directly because Visual Studio doesn't currently handle projects of this scale very well.

    Instead, we have many Solution Filter (.slnf) files which include a sub-set of projects. For more information on solution files, you can review the [official Visual Studio doc](https://learn.microsoft.com/visualstudio/ide/filtered-solutions).

    These principles guide how we create and manage .slnf files:

    - Solution files are not used by CI or command line build scripts. They are meant for use by developers only.
    - Solution files group together projects which are frequently edited at the same time.
    - Can't find a solution that has the projects you care about? Feel free to make a PR to add a new .slnf file.

    </details>

1. You can now build, debug, and test using Visual Studio. For more information on using Visual Studio to build and run projects, you can review the [official Visual Studio docs](https://learn.microsoft.com/visualstudio/get-started/csharp/run-program).

## Visual Studio Code or other editor on Windows, Linux, Mac

> :bulb: These steps also apply to editors other than Visual Studio Code. If you use a different editor, replace `code` in the steps below with your editor's equivalent command.

1. To use Visual Studio Code for developing in this repo, you need [Visual Studio Code installed](https://code.visualstudio.com/) and the ability to [launch `code` from the command line](https://code.visualstudio.com/docs/setup/mac#_launching-from-the-command-line).

1. This repo has JavaScript dependencies, so you need [Node.js](https://nodejs.org/en/).

1. Before you open anything in Visual Studio Code, run the `restore` script in the root of the repo to install the required .NET dependencies.

    ```bash
    # Linux or Mac
    ./restore.sh
    ```

    ```powershell
    # Windows
    ./restore.cmd
    ```

1. After the `restore` script finishes, activate the locally installed .NET by running the following command.

    ```bash
    # Linux or Mac
    source activate.sh
    ```

    ```powershell
    # Windows - note the leading period followed by a space
    . ./activate.ps1
    ```

1. After you've activated the locally installed .NET, open the project you want to modify by running the `code` command in the project's directory. For example, if you want to modify the`src/Http` project:

    ```bash
    cd src/Http
    code .
    ```

    > :bulb: If you're using a different editor, replace `code` with your editor's equivalent launch command (for example, `vim`).

1. Once you've opened the project in VS Code, you can build and test changes by running the `./build.sh` command in the terminal.

    > :bulb: The `build.sh` or `build.cmd` script will be local to the directory of the project you opened. For example, the script located in the `src/Http` directory. If you want to build the whole tree, use the `build.sh` or `build.cmd` that is located in the `eng` directory.

    ```bash
    # Linux or Mac
    ./build.sh
    ./build.sh -test
    ```

    ```powershell
    # Windows
    ./build.cmd
    ./build.cmd -test
    ```

1. Alternatively, you can use the `dotnet test` and `dotnet build` commands, **alongside specific project files**, once you've activated the locally installed .NET SDK.

    ```bash
    # Linux or Mac
    source activate.sh
    dotnet build
    dotnet test --filter "MySpecificUnitTest"
    ```

    ```powershell
    # Windows
    . ./activate.ps1
    dotnet build
    dotnet test --filter "MySpecificUnitTest"
    ```

## Codespaces on GitHub

If you have [Codespaces enabled on your GitHub user account](https://github.com/codespaces), you can use Codespaces to make code changes in the repo by using a cloud-based editor environment.

1. Navigate to your fork of the repo and select the branch in which you'd like to make your code changes.

    If you haven't yet created a working branch, do so by using the GitHub UI or locally by first checking out and then pushing the new branch.

1. Open a Codespace for your branch by selecting the **Code** button > **Codespaces** tab > **Create codespace**.

    ![How to open a project in Codespaces](https://user-images.githubusercontent.com/1857993/136060792-6b4c6158-0a2c-4dd6-8639-08d83da6d2d1.png)

    The Codespace will spend a few minutes building and initializing. Once it's done, you'll be able to navigate the Codespace in a web-based VS Code environment.

1. You can use the `dotnet build` and `dotnet test` commands to build and test specific projects within the repo.

    You don't need to activate the locally installed .NET SDK or run the `restore` script because it's done during the Codespace initialization process.

---

### Troubleshooting

See [BuildErrors](https://github.com/dotnet/aspnetcore/blob/main/docs/BuildErrors.md) for a description of common issues you might run into while building the repo.

## Guide to the build script

This ASP.NET Core repo contains a top-level build script located at `eng/build.cmd` and `eng/build.sh` and local build scripts within each directory. The scripts can be used to restore, build, and test the repo with support for a variety of flags. This section documents the common flags and some recommended invocation patterns.

> :warning: We do _not_ recommend running the top-level build script for the repo. You'll rarely need to build the entire repo; building a sub-project is usually sufficient for your workflow.

### Common arguments

Common arguments that can be invoked on the `build.cmd` or `build.sh` scripts include:

| Property           | Description                                                             |
| ------------------ | ----------------------------------------------------------------------- |
| Configuration      | `Debug` or `Release`. Default = `Debug`.                                |
| TargetArchitecture | The CPU architecture to build for (x64, x86, arm, arm64).               |
| TargetOsName       | The base runtime identifier to build for (win, linux, osx, linux-musl). |

### Common invocations

| Command                              | What does it do?                                                                                          |
| ------------------------------------ | --------------------------------------------------------------------------------------------------------- |
| `.\build.cmd -Configuration Release` | Build projects in a subdirectory using a `Release` configuration. Can be run in any project subdirectory. |
| `.\build.cmd -test`                  | Run all unit tests in the current project. Can be run in any project subdirectory.                        |

### Repo-level invocations

While it's typically better to use the project-specific build scripts, the repo-level build scripts located in the `eng` directory can also be used for project-specific invocations.

| Command                                                                                          | What does it do?                                                                                                                        |
| ------------------------------------------------------------------------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------- |
| `.\eng\build.cmd -all -pack -arch x64`                                                           | Build development packages for all the shipping projects in the repo. Must be run from the root of the repo.                            |
| `.\eng\build.cmd -test -projects .\src\Framework\test\Microsoft.AspNetCore.App.UnitTests.csproj` | Run all the unit tests in the `Microsoft.AspNetCore.App.UnitTests` project.                                                             |
| `.\eng\build.cmd -noBuildNative -noBuildManage`                                                  | Builds the repo and skips native and managed projects, a quicker alternative to `./restore.cmd`. Must be run from the root of the repo. |

## Complete list of repo dependencies

To support building and testing the projects in the repo, several dependencies must be installed. Some dependencies are required regardless of the project area you want to work in. Other dependencies are optional depending on the project. Most required dependencies are installed automatically by the `restore` scripts, included by default in most modern operating systems, or installed automatically by the Visual Studio installer.

### Required dependencies

| Dependency                                                              | Use                                                                                                                               |
| ----------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| [Git source control](https://git-scm.org/)                              | Used for cloning, branching, and other source control-related activities in the repo.                                             |
| [.NET](https://dotnet.microsoft.com/)                                   | A preview version of the .NET SDK is used for building projects within the repo. Installed automatically by the `restore` script. |
| [curl](https://curl.haxx.se/)/[wget](https://www.gnu.org/software/wget) | Used for downloading installation files and assets from the web.                                                                  |
| [tar](https://man7.org/linux/man-pages/man1/tar.1.html)                 | Used for unzipping installation assets. Included by default on macOS, Linux, and Windows 10 and above.                            |

### Optional dependencies

| Dependency                                                   | Use                                                                                                                                                           | Notes                                                                                                                                               |
| ------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| [Selenium](https://www.selenium.dev/)                        | Used to run integration tests in the `Components` (aka Blazor) project.                                                                                       |                                                                                                                                                     |
| [Playwright](https://playwright.dev/)                        | Used to run template tests defined in the `ProjectTemplates` work.                                                                                            |                                                                                                                                                     |
| [Chrome](https://www.google.com/chrome/)                     | Required when running tests with Selenium or Playwright in the projects above. When using Playwright, the dependency is automatically installed.              |                                                                                                                                                     |
| [Java Development Kit (v11 or newer)](https://jdk.java.net/) | Required when building the Java SignalR client. Can be installed using the `./eng/scripts/InstallJdk.ps1` script on Windows.                                  | Ensure that the `JAVA_HOME` directory points to the installation and that the `PATH` has been updated to include the `$(jdkInstallDir)/bin` folder. |
| [Wix](https://wixtoolset.org/releases/)                      | Required when working with the Windows installers in the [Windows installers project](https://github.com/dotnet/aspnetcore/tree/main/src/Installers/Windows). |                                                                                                                                                     |
| [Node.js](https://nodejs.org/en/)                            | Used for building JavaScript assets in the repo, such as those in Blazor and SignalR.                                                                         | Required a minimum version of the current NodeJS LTS.                                                                                               |
