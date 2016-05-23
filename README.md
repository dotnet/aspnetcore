
[![Join the chat at https://gitter.im/aspnet/Home](https://badges.gitter.im/aspnet/Home.svg)](https://gitter.im/aspnet/Home?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

We are pleased to [announce](https://blogs.msdn.microsoft.com/webdev/2016/05/16/announcing-asp-net-core-rc2/) the release of ASP.NET Core RC2!

[Get started with ASP.NET Core RC2](https://docs.asp.net/en/1.0.0-rc2/getting-started.html)

If you are looking for the homepage of .NET Core, with getting started guides and downloads for released versions of ASP.NET Core then you should go to http://dot.net. This is the home page of our source code repositories and is intended for those contributing to ASP.NET Core or using bleeding edge nightly builds.

If you want a more stable, released, experience or getting started instructions then go to one of the following:

- [.NET Homepage](http://dot.net)
Check out dot.net for released versions of .NET, getting started guides, and learning resources.

- [ASP.NET Core Documentation](http://docs.asp.net) or [.NET Core Documentation](http://microsoft.com/net/core). We intend to merge these in the future.

If you want to follow along with development of ASP.NET Core then go to:

- [Community Standup](http://live.asp.net)
The community standup is held every week and streamed live to YouTube. You can view past standups in the linked playlist.
- [Roadmap](https://github.com/aspnet/Home/wiki/Roadmap)
The schedule and milestone themes for ASP.NET Core.

## Getting ASP.NET Core

- Stable builds of ASP.NET Core should be obtained from http://dot.net.
- If you want to use nightlies then you need to:
    - Obtain the latest nightly CLI from http://github.com/dotnet/cli
    - Add a NuGet.Config to your app with the following content:
    ```xml
    <?xml version="1.0" encoding="utf-8"?>
    <configuration>
        <packageSources>
            <clear />
            <add key="AspNetCI" value="https://www.myget.org/F/aspnetvnext/api/v3/index.json" />
            <add key="NuGet.org" value="https://api.nuget.org/v3/index.json" />
        </packageSources>
    </configuration>
    ```
    *NOTE: This NuGet.Config should be with your application unless you want nightly packages to potentially start being restored for other apps on the machine.*
    - Change your applications dependencies to have a `*` to get the latest version, `1.0.0-*` for example.

## Repos and Projects

These are some of the most common repos:

* [DependencyInjection](https://github.com/aspnet/DependencyInjection) - basic dependency injection infrastructure and default implementation
* [Docs](https://github.com/aspnet/Docs) - documentation sources for https://docs.asp.net/en/latest/
* [EntityFramework](https://github.com/aspnet/EntityFramework) - data access technology
* [Identity](https://github.com/aspnet/Identity) - users and membership system
* [MVC](https://github.com/aspnet/Mvc) - MVC framework for web apps and services
* [Razor](https://github.com/aspnet/Razor) - template language and syntax for CSHTML files
* [Templates](https://github.com/aspnet/Templates) - project templates for Visual Studio
* [Tooling](https://github.com/aspnet/Tooling) - Visual Studio tooling, editors, and dialogs

## NuGet feeds and branches

See the [NuGet feeds](https://github.com/aspnet/Home/wiki/NuGet-feeds) wiki page.

## Build tools

This project produces builds using JetBrains TeamCity.

<a href="https://www.jetbrains.com/teamcity/"><img src="https://github.com/aspnet/Home/wiki/images/logo_TeamCity.png"></a>

# Feedback

Check out the [contributing](CONTRIBUTING.md) page to see the best places to log issues and start discussions.

