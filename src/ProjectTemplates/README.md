# Templates

## Getting Started
These are project templates which are used in .NET Core for creating ASP.NET Core applications.

## Building Templates
1. Run `. .\activate.ps1` if you haven't already.

2. Run `build.cmd -all -pack` in the repository root to build all of the dependencies.
3. Run `build.cmd` in this directory will produce NuGet packages for each class of template in the artifacts directory.
4. Because the templates build against the version of `Microsoft.AspNetCore.App` that was built during step 2 it is NOT advised that you install templates created on your local machine via `dotnet new -i [nupkgPath]`. Instead, use the `Run-[Template]-Locally.ps1` scripts in the script folder. These scripts do `dotnet new -i` with your packages, but also apply a series of fixes and tweeks to the created template which keep the fact that you don't have a production `Microsoft.AspNetCore.App` from interfering.
5. The ASP.NET localhost development certificate must also be installed and trusted or else you'll get a test error "Certificate error: Navigation blocked".

** Note** Templating tests require Visual Studio unless a full build (CI) is performed.
