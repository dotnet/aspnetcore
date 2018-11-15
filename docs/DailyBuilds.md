How to get daily builds of ASP.NET Core
=======================================

Daily builds include the latest source code changes. They are not supported for production use and are subject to frequent changes, but we strive to make sure daily builds function correctly.

If you want to download the latest daily build and use it in a project, then you need to:

- Obtain the latest [build of the .NET Core SDK](https://github.com/dotnet/core-sdk#installers-and-binaries)
- Add a NuGet.Config to your project directory with the following content:

  ```xml
  <?xml version="1.0" encoding="utf-8"?>
  <configuration>
      <packageSources>
          <clear />
          <add key="dotnet-core" value="https://dotnet.myget.org/F/dotnet-core/api/v3/index.json" />
          <add key="NuGet.org" value="https://api.nuget.org/v3/index.json" />
      </packageSources>
  </configuration>
  ```

  *NOTE: This NuGet.Config should be with your application unless you want nightly packages to potentially start being restored for other apps on the machine.*

Some features, such as new target frameworks, may require prerelease tooling builds for Visual Studio.
These are available in the [Visual Studio Preview](https://www.visualstudio.com/vs/preview/).

## NuGet packages

Daily builds of ackages can be found on <https://dotnet.myget.org/gallery/dotnet-core>. This feed may include
packages that will not be supported in a officially released build.

Commonly referenced packages:

[app-metapackage-myget]:  https://dotnet.myget.org/feed/dotnet-core/package/nuget/Microsoft.AspNetCore.App
[app-metapackage-myget-badge]: https://img.shields.io/dotnet.myget/dotnet-core/vpre/Microsoft.AspNetCore.App.svg?style=flat-square&label=myget

[metapackage-myget]:  https://dotnet.myget.org/feed/dotnet-core/package/nuget/Microsoft.AspNetCore
[metapackage-myget-badge]: https://img.shields.io/dotnet.myget/dotnet-core/vpre/Microsoft.AspNetCore.svg?style=flat-square&label=myget

Package                           | MyGet
:---------------------------------|:---------------------------------------------------------
Microsoft.AspNetCore.App          | [![][app-metapackage-myget-badge]][app-metapackage-myget]
Microsoft.AspNetCore              | [![][metapackage-myget-badge]][metapackage-myget]

## Runtime installers

Updated versions of the ASP.NET Core runtime can be installed separately from SDK updates. Runtime-only installers can be downloaded here:

[badge-master]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/master/aspnetcore-runtime-win-x64-version-badge.svg
[win-x64-zip]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/master/aspnetcore-runtime-latest-win-x64.zip
[win-x64-exe]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/master/aspnetcore-runtime-latest-win-x64.exe
[win-x86-zip]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/master/aspnetcore-runtime-latest-win-x86.zip
[win-x86-exe]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/master/aspnetcore-runtime-latest-win-x86.exe
[linux-x64-tar]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/master/aspnetcore-runtime-latest-linux-x64.tar.gz
[linux-arm-tar]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/master/aspnetcore-runtime-latest-linux-arm.tar.gz
[linux-arm64-tar]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/master/aspnetcore-runtime-latest-linux-arm64.tar.gz
[osx-x64-tar]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/master/aspnetcore-runtime-latest-osx-x64.tar.gz
[debian-x64-deb]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/master/aspnetcore-runtime-latest-x64.deb
[redhat-x64-rpm]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/master/aspnetcore-runtime-latest-x64.rpm
[linux-musl-x64-tar]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/master/aspnetcore-runtime-latest-linux-musl-x64.tar.gz

[badge-rel-22]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/release/2.2/aspnetcore-runtime-win-x64-version-badge.svg
[win-x64-zip-rel-22]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/release/2.2/aspnetcore-runtime-latest-win-x64.zip
[win-x64-exe-rel-22]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/release/2.2/aspnetcore-runtime-latest-win-x64.exe
[win-x86-zip-rel-22]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/release/2.2/aspnetcore-runtime-latest-win-x86.zip
[win-x86-exe-rel-22]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/release/2.2/aspnetcore-runtime-latest-win-x86.exe
[linux-x64-tar-rel-22]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/release/2.2/aspnetcore-runtime-latest-linux-x64.tar.gz
[osx-x64-tar-rel-22]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/release/2.2/aspnetcore-runtime-latest-osx-x64.tar.gz
[debian-x64-deb-rel-22]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/release/2.2/aspnetcore-runtime-latest-x64.deb
[redhat-x64-rpm-rel-22]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/release/2.2/aspnetcore-runtime-latest-x64.rpm
[linux-arm-tar-rel-22]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/release/2.2/aspnetcore-runtime-latest-linux-arm.tar.gz
[linux-musl-x64-tar-rel-22]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/release/2.2/aspnetcore-runtime-latest-linux-musl-x64.tar.gz

[badge-rel-21]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/release/2.1/aspnetcore-runtime-win-x64-version-badge.svg
[win-x64-zip-rel-21]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/release/2.1/aspnetcore-runtime-latest-win-x64.zip
[win-x64-exe-rel-21]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/release/2.1/aspnetcore-runtime-latest-win-x64.exe
[win-x86-zip-rel-21]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/release/2.1/aspnetcore-runtime-latest-win-x86.zip
[win-x86-exe-rel-21]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/release/2.1/aspnetcore-runtime-latest-win-x86.exe
[linux-x64-tar-rel-21]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/release/2.1/aspnetcore-runtime-latest-linux-x64.tar.gz
[osx-x64-tar-rel-21]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/release/2.1/aspnetcore-runtime-latest-osx-x64.tar.gz
[debian-x64-deb-rel-21]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/release/2.1/aspnetcore-runtime-latest-x64.deb
[redhat-x64-rpm-rel-21]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/release/2.1/aspnetcore-runtime-latest-x64.rpm
[linux-arm-tar-rel-21]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/release/2.1/aspnetcore-runtime-latest-linux-arm.tar.gz
[linux-musl-x64-tar-rel-21]: https://dotnetcli.blob.core.windows.net/dotnet/aspnetcore/Runtime/release/2.1/aspnetcore-runtime-latest-linux-musl-x64.tar.gz

Platform              | Latest (master branch) <br> ![][badge-master]                      | release/2.2 <br> ![][badge-rel-22] | release/2.1 <br> ![][badge-rel-21]
:---------------------|:----------------------------------------------------------------|:------------------------------------------------------------------------- |:-------------------------------------------------------------------------
Channel name<sup>1</sup> | `master` | `release/2.2` | `release/2.1`
Windows (x64)         | [Installer (exe)][win-x64-exe]<br>[Archive (zip)][win-x64-zip]   | [Installer (exe)][win-x64-exe-rel-22]<br>[Archive (zip)][win-x64-zip-rel-22] | [Installer (exe)][win-x64-exe-rel-21]<br>[Archive (zip)][win-x64-zip-rel-21]
Windows (x86)         | [Installer (exe)][win-x86-exe]<br>[Archive (zip)][win-x86-zip]   | [Installer (exe)][win-x86-exe-rel-22]<br>[Archive (zip)][win-x86-zip-rel-22] | [Installer (exe)][win-x86-exe-rel-21]<br>[Archive (zip)][win-x86-zip-rel-21]
macOS (x64)           | [Archive (tar.gz)][osx-x64-tar]                                  | [Archive (tar.gz)][osx-x64-tar-rel-22] | [Archive (tar.gz)][osx-x64-tar-rel-21]
Linux (x64)<br>_(for glibc based OS - most common)_ | [Archive (tar.gz)][linux-x64-tar]                                | [Archive (tar.gz)][linux-x64-tar-rel-22] | [Archive (tar.gz)][linux-x64-tar-rel-21]
Linux (x64 - musl)<br>_(for musl based OS, such as Alpine Linux)_ | [Archive (tar.gz)][linux-musl-x64-tar]                           | [Archive (tar.gz)][linux-musl-x64-tar-rel-22] | [Archive (tar.gz)][linux-musl-x64-tar-rel-21]
Linux (arm32)         | [Archive (tar.gz)][linux-arm-tar]                                | [Archive (tar.gz)][linux-arm-tar-rel-22] | [Archive (tar.gz)][linux-arm-tar-rel-21]
Linux (arm64)         | [Archive (tar.gz)][linux-arm64-tar]                                | |
Debian/Ubuntu (x64)   | [Installer (deb)][debian-x64-deb]                                | [Installer (deb)][debian-x64-deb-rel-22] | [Installer (deb)][debian-x64-deb-rel-21]
RedHat/Fedora (x64)   | [Installer (rpm)][redhat-x64-rpm]                                | [Installer (rpm)][redhat-x64-rpm-rel-22] | [Installer (rpm)][redhat-x64-rpm-rel-21]

> <sup>1</sup> For use with the `-Channel` argument in [dotnet-install.ps1/sh](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script).
