ASP.NET Core
============

[![.NET Foundation](https://img.shields.io/badge/.NET%20Foundation-blueviolet.svg)](https://www.dotnetfoundation.org/)
[![MIT License](https://img.shields.io/github/license/dotnet/aspnetcore?color=%230b0&style=flat-square)](https://github.com/dotnet/aspnetcore/blob/main/LICENSE.txt) [![Help Wanted](https://img.shields.io/github/issues/dotnet/aspnetcore/help%20wanted?color=%232EA043&label=help%20wanted&style=flat-square)](https://github.com/dotnet/aspnetcore/issues?q=is%3Aissue+is%3Aopen+label%3A%22help+wanted%22) [![Good First Issues](https://img.shields.io/github/issues/dotnet/aspnetcore/good%20first%20issue?color=%23512BD4&label=good%20first%20issue&style=flat-square)](https://github.com/dotnet/aspnetcore/issues?q=is%3Aissue+is%3Aopen+label%3A%22good+first+issue%22)
[![Discord](https://img.shields.io/discord/732297728826277939?style=flat-square&label=Discord&logo=discord&logoColor=white&color=7289DA)](https://aka.ms/dotnet-discord)

ASP.NET Core is an open-source and cross-platform framework for building modern cloud-based internet-connected applications, such as web apps, IoT apps, and mobile backends. ASP.NET Core apps run on [.NET](https://dot.net), a free, cross-platform, and open-source application runtime. It was architected to provide an optimized development framework for apps that are deployed to the cloud or run on-premises. It consists of modular components with minimal overhead, so you retain flexibility while constructing your solutions. You can develop and run your ASP.NET Core apps cross-platform on Windows, Mac, and Linux. [Learn more about ASP.NET Core](https://learn.microsoft.com/aspnet/core/).

## Get started

Follow the [Getting Started](https://learn.microsoft.com/aspnet/core/getting-started) instructions.

Also check out the [.NET Homepage](https://www.microsoft.com/net) for released versions of .NET, getting started guides, and learning resources.

See the [Triage Process](https://github.com/dotnet/aspnetcore/blob/main/docs/TriageProcess.md) document for more information on how we handle incoming issues.

## How to engage, contribute, and give feedback

Some of the best ways to contribute are to try things out, file issues, join in design conversations,
and make pull-requests.

* [Download our latest daily builds](./docs/DailyBuilds.md)
* Follow along with the development of ASP.NET Core:
    * [Community Standup](https://live.asp.net): The community standup is held every week and streamed live on YouTube. You can view past standups in the linked playlist.
    * [Roadmap](https://aka.ms/aspnet/roadmap): The schedule and milestone themes for ASP.NET Core.
* [Build ASP.NET Core source code](./docs/BuildFromSource.md)
* Check out the [contributing](CONTRIBUTING.md) page to see the best places to log issues and start discussions.

## Reporting security issues and bugs

Security issues and bugs should be reported privately, via email, to the Microsoft Security Response Center (MSRC)  secure@microsoft.com. You should receive a response within 24 hours. If for some reason you do not, please follow up via email to ensure we received your original message. Further information, including the MSRC PGP key, can be found in the [Security TechCenter](https://technet.microsoft.com/en-us/security/ff852094.aspx).

## Related projects

These are some other repos for related projects:

* [Documentation](https://github.com/aspnet/Docs) - documentation sources for https://learn.microsoft.com/aspnet/core/
* [Entity Framework Core](https://github.com/dotnet/efcore) - data access technology
* [Runtime](https://github.com/dotnet/runtime) - cross-platform runtime for cloud, mobile, desktop, and IoT apps
* [Razor](https://github.com/dotnet/razor) - the Razor compiler and tooling for working with Razor syntax (.cshtml, .razor)

## Code of conduct

See [CODE-OF-CONDUCT](./CODE-OF-CONDUCT.md)

## Nightly builds

This table includes links to download the latest builds of the ASP.NET Core Shared Framework. Also included are links to download the Windows Hosting Bundle, which includes the ASP.NET Core Shared Framework, the .NET Runtime Shared Framework, and the IIS plugin (ASP.NET Core Module). You can download the latest .NET Runtime builds [here](https://github.com/dotnet/runtime/blob/main/docs/project/dogfooding.md#nightly-builds-table), and the latest .NET SDK builds [here](https://github.com/dotnet/installer#table). **If you're unsure what you need, then install the SDK; it has everything except the IIS plugin.**

| Platform | Shared Framework (Installer) | Shared Framework (Binaries) | Hosting Bundle (Installer) |
| :--------- | :----------: | :----------: | :----------: |
| **Windows x64** | [Installer](https://aka.ms/dotnet/9.0/daily/aspnetcore-runtime-win-x64.exe) | [Binaries](https://aka.ms/dotnet/9.0/daily/aspnetcore-runtime-win-x64.zip) | [Installer](https://aka.ms/dotnet/9.0/daily/dotnet-hosting-win.exe) |
| **Windows x86** | [Installer](https://aka.ms/dotnet/9.0/daily/aspnetcore-runtime-win-x86.exe) | [Binaries](https://aka.ms/dotnet/9.0/daily/aspnetcore-runtime-win-x86.zip) | [Installer](https://aka.ms/dotnet/9.0/daily/dotnet-hosting-win.exe) |
| **Windows arm64** | [Installer](https://aka.ms/dotnet/9.0/daily/aspnetcore-runtime-win-arm64.exe) | [Binaries](https://aka.ms/dotnet/9.0/daily/aspnetcore-runtime-win-arm64.zip) | [Installer](https://aka.ms/dotnet/9.0/daily/dotnet-hosting-win.exe) |
| **macOS x64** | **N/A** | [Binaries](https://aka.ms/dotnet/9.0/daily/aspnetcore-runtime-osx-x64.tar.gz) | **N/A** |
| **macOS arm64** | **N/A** | [Binaries](https://aka.ms/dotnet/9.0/daily/aspnetcore-runtime-osx-arm64.tar.gz) | **N/A** |
| **Linux x64** | [Deb Installer](https://aka.ms/dotnet/9.0/daily/aspnetcore-runtime-x64.deb) - [RPM Installer](https://aka.ms/dotnet/9.0/daily/aspnetcore-runtime-x64.rpm) | [Binaries](https://aka.ms/dotnet/9.0/daily/aspnetcore-runtime-linux-x64.tar.gz) | **N/A** |
| **Linux arm** | **N/A** | [Binaries](https://aka.ms/dotnet/9.0/daily/aspnetcore-runtime-linux-arm.tar.gz) | **N/A** |
| **Linux arm64** | [RPM Installer](https://aka.ms/dotnet/9.0/daily/aspnetcore-runtime-aarch64.rpm) | [Binaries](https://aka.ms/dotnet/9.0/daily/aspnetcore-runtime-linux-arm64.tar.gz) | **N/A** |
| **Linux-musl-x64** | **N/A** | [Binaries](https://aka.ms/dotnet/9.0/daily/aspnetcore-runtime-linux-musl-x64.tar.gz) | **N/A** |
| **Linux-musl-arm** | **N/A** | [Binaries](https://aka.ms/dotnet/9.0/daily/aspnetcore-runtime-linux-musl-arm.tar.gz) | **N/A** |
| **Linux-musl-arm64** | **N/A** | [Binaries](https://aka.ms/dotnet/9.0/daily/aspnetcore-runtime-linux-musl-arm64.tar.gz) | **N/A** |
