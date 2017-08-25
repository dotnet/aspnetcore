Razor
=====

AppVeyor: [![AppVeyor](https://ci.appveyor.com/api/projects/status/olbc8ur2jna0v27j/branch/dev?svg=true)](https://ci.appveyor.com/project/aspnetci/razor/branch/dev)

Travis:   [![Travis](https://travis-ci.org/aspnet/Razor.svg?branch=dev)](https://travis-ci.org/aspnet/Razor)

The Razor syntax provides a fast, terse, clean and lightweight way to combine server code with HTML to create dynamic web content. This repo contains the parser and the C# code generator for the Razor syntax.

This project is part of ASP.NET Core. You can find samples, documentation and getting started instructions for ASP.NET Core at the [Home](https://www.github.com/aspnet/home) repo.

## Building from source

To run a complete build on command line only, execute `build.cmd` or `build.sh` without arguments.

Before opening this project in Visual Studio or VS Code, execute `build.cmd /t:Restore` (Windows) or `./build.sh /t:Restore` (Linux/macOS).
This will execute only the part of the build script that downloads and initializes a few required build tools and packages.

See [developer documentation](https://github.com/aspnet/Home/wiki) for more details.
