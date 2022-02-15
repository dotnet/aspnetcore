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

Microsoft.DotNet.Web.Spa.ProjectTemplates.csproj contains the Single Page Application templates, including Angular, React. **This is brought in by a submodule from the dotnet/spa-templates repo.**

To build the ProjectTemplates, use one of:

1. Run `eng\build.cmd -all -pack -configuration Release` in the repository root to build and pack all of the repo, including template projects.
1. Run `src\ProjectTemplates\build.cmd -pack -configuration Release` to produce NuGet packages only for the template projects.
    - This will also build and pack the shared framework.

**Note** use `eng/build.sh` or `src/ProjectTemplates/build.sh` on non-Windows platforms.

### Test

#### Running ProjectTemplate tests:

To run ProjectTemplate tests, first ensure the ASP.NET localhost development certificate is installed and trusted.
Otherwise, you'll get a test error "Certificate error: Navigation blocked".

Then, use one of:

1. Run `src\ProjectTemplates\build.cmd -test -NoRestore -NoBuild -NoBuilddeps -configuration Release` (or equivalent src\ProjectTemplates\build.sh` command) to run all template tests.
1. To test specific templates, use the `Run-[Template]-Locally.ps1` scripts in the script folder.
    - These scripts do `dotnet new -i` with your packages, but also apply a series of fixes and tweaks to the created template which keep the fact that you don't have a production `Microsoft.AspNetCore.App` from interfering.
1. Run templates manually with `custom-hive` and `disable-sdk-templates` to install to a custom location and turn off the built-in templates e.g.
    - `dotnet new -i Microsoft.DotNet.Web.Spa.ProjectTemplates.6.0.6.0.0-dev.nupkg --debug:custom-hive C:\TemplateHive\`
    - `dotnet new angular --auth Individual --debug:disable-sdk-templates --debug:custom-hive C:\TemplateHive\`
1. Install the templates to an existing Visual Studio installation.
    1. Pack the ProjectTemplates: `src\ProjectTemplates\build.cmd -pack -configuration Release`
        - This will produce the `*dev.nupkg` containing the ProjectTemplates at `artifacts\packages\Release\Shipping\Microsoft.DotNet.Web.ProjectTemplates.7.0.7.0.0-dev.nupkg`
    2. Install ProjectTemplates in local Visual Studio instance: `dotnet new -i "<REPO_PATH>\artifacts\packages\Release\Shipping\Microsoft.DotNet.Web.ProjectTemplates.7.0.7.0.0-dev.nupkg"`
    3. Run Visual Studio and test out templates manually.
    4. Uninstall ProjectTemplates from local Visual Studio instance: `dotnet new --uninstall Microsoft.DotNet.Web.ProjectTemplates.7.0`

**Note** ProjectTemplates tests require Visual Studio unless a full build (CI) is performed.

**Note** Because the templates build against the version of `Microsoft.AspNetCore.App` that was built during the
previous step, it is NOT advised that you install templates created on your local machine using just
`dotnet new -i [nupkgPath]`.

## More Information

For more information, see the [ASP.NET Core README](../../README.md).
