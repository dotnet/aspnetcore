# Build the ASP.NET Core Repo

If you're reading these instructions, you're probably a contributor looking to understand how to build this repo locally on your machine so that you can build, debug, and test changes.

To get started, you'll need to have a fork of the repo cloned locally. This workflow assumes that you have [git installed on your development machine](https://git-scm.com/book/en/v2/Getting-Started-Installing-Git).

1. Create your own fork, click the **Fork** button from our GitHub repo as a signed-in user and your own fork will be created. For more information on managing forks, you can review the [GitHub docs on working with forks](https://docs.github.com/en/github/collaborating-with-pull-requests/working-with-forks).
2. Clone the repo locally using the `git clone` command. Since this repo contains submodules, you'll need to pass the `--recursive` flag to pull the sources for the submodules locally.

```
git clone --recursive https://github.com/YOUR_USERNAME/aspnetcore
```

3. If you've already cloned the repo without passing the `--recursive` flag, you can fetch submodule sources at any time using:

```
git submodule update --init --recursive
```

The experience for building the repo is slightly different based on what environment you are looking to develop in. Select one of the links below to navigate to the instructions for your environment of choice.

* [Visual Studio on Windows](#visual-studio-in-windows)
* [Visual Studio Code on Windows, Linux, Mac](visual-studio-code-on-windows-linux-mac)
* [Codespaces on GitHub](codespaces-on-github)

## Visual Studio on Windows

1. Setting up the repo on Windows will require executing some scripts in PowerShell. You'll need to update the execution policy on your machine to support this. For more information on execution policies, review [the execution policy docs](https://docs.microsoft.com/powershell/module/microsoft.powershell.security/set-executionpolicy).

```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

2. In order to install Visual Studio on your machine, you can use the official installer script in the repo. Even if you have the appropriate Visual Studio installed on your machine, we recommend running this installation script so that the correct components are installed.

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
We have a single .sln file for all of ASP.NET Core, but most people don't work with it directly because Visual Studio doesn't currently handle projects of this scale very well. Instead, we have many Solution Filter (.slnf) files which include a sub-set of projects. For more information on solution files, you can review the [official Visual Studio doc](https://docs.microsoft.com/visualstudio/ide/filtered-solutions).These principles guide how we create and manage .slnf files:
Solution files are not used by CI or command line build scripts. They are meant for use by developers only.
Solution files group together projects which are frequently edited at the same time.
Can't find a solution that has the projects you care about? Feel free to make a PR to add a new .slnf file.
</details>

5. You can now build, debug, and test using Visual Studio. For more information on using Visual Studio to build and run projects, you can review the [official Visual Studio docs](https://docs.microsoft.com/en-us/visualstudio/get-started/csharp/run-program).

## Visual Studio Code on Windows, Linux, Mac

1. In order to use Visual Studio Code for development on this repo, you'll need to have [VS Code](https://code.visualstudio.com/) installed and [the `code` command installed](https://code.visualstudio.com/docs/setup/mac#_launching-from-the-command-line) on your machine.
2. The repo constains some JavaScript dependencies, so you will need to install [Node](https://nodejs.org/en/) and [yarn](https://yarnpkg.com/) on your machine.
3. Prior to opening the code in Visual Studio code, you'll need to run the `restore` script locally to install the required dotnet dependencies and setup the repo. The `restore` script is located in the root of the repo.

```bash
./restore.sh
```

```
./restore.ps1
```

2. After the restore script has finished executing, activate the locally installed .NET by running the following command.

```bash
source activate.sh
```

```
. ./activate.ps1
```

3. After activating the locally installed .NET, you can open your project of choice by running the `code` command in the directory of choice. For example, if you want to modify code in the `src/Http` project, you can use the following:

```bash
cd src/Http
code .
```

4. Once you've opened the project in VS Code, you can build and test changes by running the `./build.sh` command in the terminal.

```bash
./build.sh
./build.sh -test
```

5. Alternatively, you can use the `dotnet test` and `dotnet build` commands directly once you've activated the locally installed .NET SDK.

```
source activate.sh
dotnet build
dotnet test --filter "MySpecificUnitTest"
```

## Codespaces on GitHub

If you have [Codespaces enabled on your GitHub user account](https://github.com/codespaces), you can use Codespaces to make code changes in the repo using a cloud based editor environment.

1. Navigate to the fork and branch you would like to make code changes in. Note: if you have not created a new branch yet, do so using the GitHub UI or locally checking out and pushing a new branch.
2. Open a Codespace for your branch by navigating to the "Code" button > selecting the "Codespaces" tab > clicking the "New codespace" button.

![Screen Shot 2021-10-05 at 9 05 39 AM](https://user-images.githubusercontent.com/1857993/136060792-6b4c6158-0a2c-4dd6-8639-08d83da6d2d1.png)

3. The Codespace will spend a few minutes building and initializating. Once this is done, you'll be able to navigate the Codespace in a web-based VS Code environment. You can use the `dotnet build` and `dotnet test` commands to build and test the repo. Note: you do not need to activate the locally installed .NET SDK or run the restore script. This is done as part of the initialization process.

------

### Troubleshooting

See [BuildErrors](https://github.com/dotnet/aspnetcore/blob/main/docs/BuildErrors.md) for a description of common issues you might run into while building the repo.

### A Guide to the Build Script

The `build.cmd` and `build.sh` scripts contain multiple flags that can be used control the behavior of the build script. See [BuildScript](./BuildScript.md) for information about the flags available on the script and helpful invocation patterns.

### Using the results of a local build

There are some scenarios where you might want to use the contents of the build to validate changes end-to-end. See [UsingBuildResults](UsingBuildResults.md) for information on patterns for validating changes in E2E scenarios.

### A Complete List of Repo Dependencies

Depending on what project area you want to work in, you may need to install additional dependencies to support building different projects in the repo. A complete list of dependencies is document in the [RepoDependencies](RepoDependencies.md) doc.
