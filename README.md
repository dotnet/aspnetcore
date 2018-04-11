KestrelHttpServer
=================

[![Join the chat at https://gitter.im/aspnet/KestrelHttpServer](https://badges.gitter.im/aspnet/KestrelHttpServer.svg)](https://gitter.im/aspnet/KestrelHttpServer?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

[![Travis build status][travis-badge]](https://travis-ci.org/aspnet/KestrelHttpServer/branches)
[![AppVeyor build status][appveyor-badge]](https://ci.appveyor.com/project/aspnetci/KestrelHttpServer/branch/dev)

[travis-badge]: https://img.shields.io/travis/aspnet/KestrelHttpServer.svg?label=travis-ci&branch=dev&style=flat-square
[appveyor-badge]: https://img.shields.io/appveyor/ci/aspnetci/KestrelHttpServer/dev.svg?label=appveyor&style=flat-square

This repo contains a cross-platform web server for ASP.NET Core.

This project is part of ASP.NET Core. You can find samples, documentation and getting started instructions for ASP.NET Core at the [Home](https://github.com/aspnet/home) repo.

## Building from source

To run a complete build on command line only, execute `build.cmd` or `build.sh` without arguments. See [developer documentation](https://github.com/aspnet/Home/wiki) for more details.

## File logging for functional test

Turn on file logging for Kestrel functional tests by specifying the environment variable ASPNETCORE_TEST_LOG_DIR to the log output directory.

## Packages

Kestrel is available as a NuGet package.

 Package name                               | Stable                                      | Nightly (`dev` branch)
--------------------------------------------|---------------------------------------------|------------------------------------------
`Microsoft.AspNetCore.Server.Kestrel`       | [![NuGet][main-nuget-badge]][main-nuget]    | [![MyGet][main-myget-badge]][main-myget]
`Microsoft.AspNetCore.Server.Kestrel.Https` | [![NuGet][https-nuget-badge]][https-nuget]  | [![MyGet][https-myget-badge]][https-myget]


[main-nuget]: https://www.nuget.org/packages/Microsoft.AspNetCore.Server.Kestrel/
[main-nuget-badge]: https://img.shields.io/nuget/v/Microsoft.AspNetCore.Server.Kestrel.svg?style=flat-square&label=nuget
[main-myget]: https://dotnet.myget.org/feed/aspnetcore-dev/package/nuget/Microsoft.AspNetCore.Server.Kestrel
[main-myget-badge]: https://img.shields.io/dotnet.myget/aspnetcore-dev/vpre/Microsoft.AspNetCore.Server.Kestrel.svg?style=flat-square&label=myget

[https-nuget]: https://www.nuget.org/packages/Microsoft.AspNetCore.Server.Kestrel.Https/
[https-nuget-badge]: https://img.shields.io/nuget/v/Microsoft.AspNetCore.Server.Kestrel.Https.svg?style=flat-square&label=nuget
[https-myget]: https://dotnet.myget.org/feed/aspnetcore-dev/package/nuget/Microsoft.AspNetCore.Server.Kestrel.Https
[https-myget-badge]: https://img.shields.io/dotnet.myget/aspnetcore-dev/vpre/Microsoft.AspNetCore.Server.Kestrel.Https.svg?style=flat-square&label=myget
