
# ASP.NET Core
[![Join the chat at https://gitter.im/aspnet/Home](https://badges.gitter.im/aspnet/Home.svg)](https://gitter.im/aspnet/Home?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

This is the home page of the ASP.NET Core source code repositories and is intended for those contributing to ASP.NET Core or using bleeding edge nightly builds.

ASP.NET Core is a new open-source and cross-platform framework for building modern cloud based internet connected applications, such as web apps, IoT apps and mobile backends. ASP.NET Core apps can run on .NET Core or on the full .NET Framework. It was architected to provide an optimized development framework for apps that are deployed to the cloud or run on-premises. It consists of modular components with minimal overhead, so you retain flexibility while constructing your solutions. You can develop and run your ASP.NET Core apps cross-platform on Windows, Mac and Linux. [Learn more about ASP.NET Core](https://docs.asp.net/en/latest/intro.html).

We are pleased to [announce](https://blogs.msdn.microsoft.com/webdev/2016/06/27/announcing-asp-net-core-1-0/) the release of ASP.NET Core 1.0!

## Get Started

Follow the [Getting Started](https://docs.asp.net/en/latest/getting-started.html) instructions in the [ASP.NET Core docs](https://docs.asp.net).

Also checkout the [.NET Homepage](http://dot.net) for released versions of .NET, getting started guides, and learning resources.

## Builds

If you want to use the latest dev build then you need to:

- Obtain the latest [build of the .NET Core SDK](https://github.com/dotnet/cli#installers-and-binaries)
- Add a NuGet.Config to your app with the following content:
    
  ```xml
  <?xml version="1.0" encoding="utf-8"?>
  <configuration>
      <packageSources>
          <clear />
          <add key="AspNetVNext" value="https://www.myget.org/F/aspnetvnext/api/v3/index.json" />
          <add key="NuGet.org" value="https://api.nuget.org/v3/index.json" />
      </packageSources>
  </configuration>
  ```
    
  *NOTE: This NuGet.Config should be with your application unless you want nightly packages to potentially start being restored for other apps on the machine.*
    
- Change your applications dependencies to have a `*` to get the latest version (ex. `1.0.0-*`).

Prerelease tooling builds for Visual Studio are available from the [Tooling](https://github.com/aspnet/tooling/#pre-release-builds) repo.

## Community and roadmap

To follow along with the development of ASP.NET Core:

- [Community Standup](http://live.asp.net): The community standup is held every week and streamed live to YouTube. You can view past standups in the linked playlist.
- [Roadmap](https://github.com/aspnet/Home/wiki/Roadmap): The schedule and milestone themes for ASP.NET Core.

## Repos and projects

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

