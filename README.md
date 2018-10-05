
# ASP.NET Core

[app-metapackage-nuget]:  https://nuget.org/packages/Microsoft.AspNetCore.All
[app-metapackage-nuget-badge]: http://img.shields.io/nuget/v/Microsoft.AspNetCore.All.svg?style=flat-square&label=aspnet@stable
[app-metapackage-myget]:  https://dotnet.myget.org/feed/dotnet-core/package/nuget/Microsoft.AspNetCore.App
[app-metapackage-myget-badge]: http://img.shields.io/dotnet.myget/dotnet-core/v/Microsoft.AspNetCore.App.svg?style=flat-square&label=aspnet@preview

[![][app-metapackage-nuget-badge]][app-metapackage-nuget]
[![][app-metapackage-myget-badge]][app-metapackage-myget]

[![Join the chat at https://gitter.im/aspnet/Home](https://badges.gitter.im/aspnet/Home.svg)](https://gitter.im/aspnet/Home?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

This is the home page of the ASP.NET Core source code repositories and is intended for those contributing to ASP.NET Core or using bleeding edge nightly builds.

ASP.NET Core is a new open-source and cross-platform framework for building modern cloud based internet connected applications, such as web apps, IoT apps and mobile backends. ASP.NET Core apps can run on .NET Core or on the full .NET Framework. It was architected to provide an optimized development framework for apps that are deployed to the cloud or run on-premises. It consists of modular components with minimal overhead, so you retain flexibility while constructing your solutions. You can develop and run your ASP.NET Core apps cross-platform on Windows, Mac and Linux. [Learn more about ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/).

ASP.NET Core 1.1 is now available! See the [release notes](https://github.com/aspnet/Home/releases/tag/1.1.0) for further details.

ASP.NET Core 2.0 is now available! See the [release notes](https://github.com/aspnet/Home/releases/tag/2.0.0) for further details and check our [releases](https://github.com/aspnet/home/releases) for the latest patch release.

ASP.NET Core 2.1 is now available! See the [release notes](https://github.com/aspnet/Home/releases/tag/2.1.0) for further details and check our [releases](https://github.com/aspnet/Home/releases/) for the latest patch release.

## Get Started

Follow the [Getting Started](https://docs.microsoft.com/en-us/aspnet/core/getting-started) instructions in the [ASP.NET Core docs](https://docs.microsoft.com/en-us/aspnet/index).

Also checkout the [.NET Homepage](https://www.microsoft.com/net) for released versions of .NET, getting started guides, and learning resources.

## Daily builds

If you want to use the latest daily build then you need to:

- Obtain the latest [build of the .NET Core SDK](https://github.com/dotnet/cli#installers-and-binaries)
- Add a NuGet.Config to your app with the following content:

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

Prerelease tooling builds for Visual Studio are available in the [Visual Studio Preview](https://www.visualstudio.com/vs/preview/).


## Community and roadmap

To follow along with the development of ASP.NET Core:

- [Community Standup](http://live.asp.net): The community standup is held every week and streamed live to YouTube. You can view past standups in the linked playlist.
- [Roadmap](https://github.com/aspnet/Home/wiki/Roadmap): The schedule and milestone themes for ASP.NET Core.

## Repos and projects

These are some of the most common repos:

* [DependencyInjection](https://github.com/aspnet/DependencyInjection) - basic dependency injection infrastructure and default implementation
* [Docs](https://github.com/aspnet/Docs) - documentation sources for https://docs.microsoft.com/en-us/aspnet/core/
* [EntityFrameworkCore](https://github.com/aspnet/EntityFrameworkCore) - data access technology
* [Identity](https://github.com/aspnet/Identity) - users and membership system
* [MVC](https://github.com/aspnet/Mvc) - MVC framework for web apps and services
* [Razor](https://github.com/aspnet/Razor) - template language and syntax for CSHTML files
* [SignalR](https://github.com/aspnet/SignalR) - library to add real-time web functionality
* [Templating](https://github.com/aspnet/Templating) - project templates for Visual Studio and the .NET Core SDK
* [Tooling](https://github.com/aspnet/Tooling) - Visual Studio tooling, editors, and dialogs

## NuGet feeds and branches

See the [NuGet feeds](https://github.com/aspnet/Home/wiki/NuGet-feeds) wiki page.

# Feedback

Check out the [contributing](CONTRIBUTING.md) page to see the best places to log issues and start discussions.

# Code of conduct

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).  For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
