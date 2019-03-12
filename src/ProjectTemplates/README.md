# Templates

## Getting Started
These are project templates which are used in .NET Core for creating ASP.NET Core applications.

## Building Templates
- Run `build.cmd -Pack` in the repository root to build all of the dependencies.
- Run `build.cmd` in this directory will produce NuGet packages for each class of template in the artifacts directory. These can be installed via `dotnet new -i {nugetpackage path}`
- The ASP.NET localhost development certificate must also be installed and trusted or else you'll get a test error "Certificate error: Navigation blocked".
- You also need to get the packages these templates depend on into your package cache or else `dotnet new` restore will fail. The easiest way to get them to run is by letting the build run at least 1 test. Note currently some packages are missed. https://github.com/aspnet/AspNetCore/issues/7388