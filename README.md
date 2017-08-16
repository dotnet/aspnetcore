KestrelHttpServer
=================

[![Join the chat at https://gitter.im/aspnet/KestrelHttpServer](https://badges.gitter.im/aspnet/KestrelHttpServer.svg)](https://gitter.im/aspnet/KestrelHttpServer?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

AppVeyor: [![AppVeyor](https://ci.appveyor.com/api/projects/status/nr0s92ykm57q0bjv/branch/dev?svg=true)](https://ci.appveyor.com/project/aspnetci/KestrelHttpServer/branch/dev)

Travis: [![Travis](https://travis-ci.org/aspnet/KestrelHttpServer.svg?branch=dev)](https://travis-ci.org/aspnet/KestrelHttpServer)

This repo contains a cross-platform web server for ASP.NET Core.

This project is part of ASP.NET Core. You can find samples, documentation and getting started instructions for ASP.NET Core at the [Home](https://github.com/aspnet/home) repo.

## Building from source

To run a complete build on command line only, execute `build.cmd` or `build.sh` without arguments.

Before opening this project in Visual Studio or VS Code, execute `build.cmd /t:Restore` (Windows) or `./build.sh /t:Restore` (Linux/macOS).
This will execute only the part of the build script that downloads and initializes a few required build tools and packages.

See [developer documentation](https://github.com/aspnet/Home/wiki) for more details.
