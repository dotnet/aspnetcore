Universe
========

Build infrastructure used to produce the whole ASP.NET Core stack.

## Released Builds

### ASP.NET Core Runtime Store

The runtime store can be downloaded from [here](https://microsoft.com/net/download).

### NuGet packages

All published ASP.NET Core packages can be found on <https://www.nuget.org/profiles/aspnet>.

Commonly referenced packages:

[all-metapackage-nuget]:  https://nuget.org/packages/Microsoft.AspNetCore.All
[all-metapackage-nuget-badge]: http://img.shields.io/nuget/v/Microsoft.AspNetCore.All.svg?style=flat-square&label=nuget

[metapackage-nuget]:  https://nuget.org/packages/Microsoft.AspNetCore
[metapackage-nuget-badge]: http://img.shields.io/nuget/v/Microsoft.AspNetCore.svg?style=flat-square&label=nuget

Package                           | NuGet.org
----------------------------------|-------------------
Microsoft.AspNetCore.All          | [![][all-metapackage-nuget-badge]][all-metapackage-nuget]
Microsoft.AspNetCore              | [![][metapackage-nuget-badge]][metapackage-nuget]


## Daily builds

### NuGet packages

Packages can be found on <https://dotnet.myget.org/gallery/aspnetcore-dev>. This feed may include
packages that will not be supported in a officially released build.

Commonly referenced packages:

[all-metapackage-myget]:  https://dotnet.myget.org/feed/aspnetcore-dev/package/nuget/Microsoft.AspNetCore.All
[all-metapackage-myget-badge]: http://img.shields.io/dotnet.myget/aspnetcore-dev/v/Microsoft.AspNetCore.All.svg?style=flat-square&label=myget

[metapackage-myget]:  https://dotnet.myget.org/feed/aspnetcore-dev/package/nuget/Microsoft.AspNetCore
[metapackage-myget-badge]: http://img.shields.io/dotnet.myget/aspnetcore-dev/v/Microsoft.AspNetCore.svg?style=flat-square&label=myget

Package                           | MyGet
----------------------------------|-------------------
Microsoft.AspNetCore.All          | [![][all-metapackage-myget-badge]][all-metapackage-myget]
Microsoft.AspNetCore              | [![][metapackage-myget-badge]][metapackage-myget]

### ASP.NET Core Shared Framework

[win-x64-badge]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/aspnetcore-runtime-win-x64-version-badge.svg
[win-x86-badge]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/aspnetcore-runtime-win-x86-version-badge.svg
[linux-x64-badge]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/aspnetcore-runtime-linux-x64-version-badge.svg
[osx-x64-badge]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/aspnetcore-runtime-osx-x64-version-badge.svg

[win-x64-zip]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/aspnetcore-runtime-latest-win-x64.zip
[win-x64-exe]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/aspnetcore-runtime-latest-win-x64.exe
[win-x86-zip]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/aspnetcore-runtime-latest-win-x86.zip
[win-x86-exe]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/aspnetcore-runtime-latest-win-x86.exe
[linux-x64-tar]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/aspnetcore-runtime-latest-linux-x64.tar.gz
[osx-x64-tar]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/aspnetcore-runtime-latest-osx-x64.tar.gz
[debian-x64-deb]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/aspnetcore-runtime-latest-x64.deb
[redhat-x64-rpm]: https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/aspnetcore-runtime-latest-x64.rpm

Platform              | Latest (dev branch)
----------------------|---------------------
Windows (x64)         | ![][win-x64-badge]<br>[Installer (exe)][win-x64-exe]<br>[Archive (zip)][win-x64-zip]
Windows (x86)         | ![][win-x86-badge]<br>[Installer (exe)][win-x86-exe]<br>[Archive (zip)][win-x86-zip]
macOS (x64)           | ![][osx-x64-badge]<br>[Archive (tar.gz)][osx-x64-tar]
Linux (x64)           | ![][linux-x64-badge]<br>[Archive (tar.gz)][linux-x64-tar]
Debian/Ubuntu (x64)   | ![][linux-x64-badge]<br>[Installer (deb)][debian-x64-deb]
RedHat/Fedora (x64)   | ![][linux-x64-badge]<br>[Installer (rpm)][redhat-x64-rpm]

## Building from source

```
git clone --recursive https://github.com/aspnet/Universe.git
cd Universe
./build.cmd
```

### Useful properties and targets
Property                           | Purpose                                                                        | Example
-----------------------------------|--------------------------------------------------------------------------------|--------
`SkipTests`    | Only build repos, don't run the tests.                                         | `/p:SkipTests=true`
`TestOnly`                      | Don't package or verify things.                                                | `/p:TestOnly=true`
`KOREBUILD_REPOSITORY_INCLUDE` | A list of the repositories to include in build (instead of all of them).       | `$env:KOREBUILD_REPOSITORY_INCLUDE='Antiforgery;CORS'`
`KOREBUILD_REPOSITORY_EXCLUDE` | A list of the repositories to exclude from build (all the rest will be built). | `$env:KOREBUILD_REPOSITORY_EXCLUDE='EntityFramework'`

## More info

This project is part of ASP.NET Core. You can find samples, documentation and getting started instructions for ASP.NET Core at the [Home](https://github.com/aspnet/home) repo.
