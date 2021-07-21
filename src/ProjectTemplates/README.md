# Templates

These are project templates which are used in .NET Core for creating ASP.NET Core applications.

## Description

The following contains a description of each sub-directory in the `ProjectTemplates` directory.

- `BlazorTemplates.Tests`: Contains the source files for the Blazor template tests, these are currently split out due to not being Helix ready yet.
- `Shared`: Contains a collection of shared constants and helper methods/classes including the infrastructure for managing dotnet processes to create, build, run template tests.
- `Web.Client.ItemTemplates`: Contains the Web Client-Side File templates, includes things like less, scss, and typescript
- `Web.ItemTemplates`: Contains the Web File templates, includes things like: protobuf, razor component, razor page, view import and start pages
- `Web.ProjectTemplates`: Contains the ASP.NET Core Web Template pack, including Blazor Server, WASM, Empty, Grpc, Razor Class Library, RazorPages, MVC, WebApi.
- `migrations`: Contains migration related scripts.
- `scripts`: Contains a collection of scripts that help running tests locally that avoid having to install the templates to the machine.
- `test`: Contains the end to end template tests.
- `testassets`: Contains assets used by the tests, like a dotnet tools installer

### Build

Some projects in this repository (like SignalR Java Client) require JDK installation and configuration of `JAVA_HOME` environment variable.

1. If you don't have the JDK installed, you can find it from https://www.oracle.com/technetwork/java/javase/downloads/index.html
1. After installation define a new environment variable named `JAVA_HOME` pointing to the root of the latest JDK installation (for Windows it will be something like `c:\Program Files\Java\jdk-12`).
1. Add the `%JAVA_HOME%\bin` directory to the `PATH` environment variable

`Web.Spa.ProjectTemplates`folder contains the Single Page Application templates, including Anuglar, React. Those are brought in by a submodule from the dotnet/spa-templates repo.**

To build the ProjectTemplates, use one of:

1. Run `eng\build.cmd -all -pack -configuration Release` in the repository root to build and pack all of the repo, including template projects.
1. Run `src\ProjectTemplates\build.cmd -pack -configuration Release` to produce NuGet packages only for the template projects.

**Note** use `eng/build.sh` or `src/ProjectTemplates/build.sh` on non-Windows platforms.

### Test

To run the ProjectTemplate tests:

1. Because the templates build against the version of `Microsoft.AspNetCore.App` that was built during the previous step, it is NOT advised that you install templates created on your local machine via `dotnet new -i [nupkgPath]`. Instead, use the `Run-[Template]-Locally.ps1` scripts in the script folder. These scripts do `dotnet new -i` with your packages, but also apply a series of fixes and tweaks to the created template which keep the fact that you don't have a production `Microsoft.AspNetCore.App` from interfering.
1. The ASP.NET localhost development certificate must also be installed and trusted or else you'll get a test error "Certificate error: Navigation blocked".
1. Run `eng\build.cmd -test -NoRestore -NoBuild -NoBuilddeps -configuration Release "/p:RunTemplateTests=true"` to run template tests.

**Note** ProjectTemplates tests require Visual Studio unless a full build (CI) is performed.

## More Information

For more information, see the [ASP.NET Core README](../../README.md).
