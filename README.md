# Getting Started with ASP.NET Core

[![Join the chat at https://gitter.im/aspnet/Home](https://badges.gitter.im/aspnet/Home.svg)](https://gitter.im/aspnet/Home?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

This guide is designed to get you started building applications with the latest development versions ASP.NET Core. This means nightly builds and potentially broken or unstable packages.

If you want a more stable, released, experience then you should go to https://www.asp.net/vnext.

## What you need

The key part of working with development feeds is getting your environment set up so that you can acquire and switch to new builds of the DNX. Once you have that then it is just a matter of pulling the latest packages from the development MyGet feed.

In order to be able to get new builds of the DNX, and switch between them, you need to get the .NET Version Manager (DNVM) command line tool.

## Getting Started on Windows

The easiest way to get started on Windows is to grab the latest version of Visual Studio 2015, which can be found [here](https://www.visualstudio.com/en-us/downloads/download-visual-studio-vs.aspx).

Visual Studio will install DNVM for you, so if you open a developer command prompt and type `dnvm` you should get some help text.

### Upgrading DNVM or running without Visual Studio

If you don't want to install Visual Studio or want to upgrade DNVM to the latest version then you need to run the following command:

####CMD
```
@powershell -NoProfile -ExecutionPolicy unrestricted -Command "&{$Branch='dev';$wc=New-Object System.Net.WebClient;$wc.Proxy=[System.Net.WebRequest]::DefaultWebProxy;$wc.Proxy.Credentials=[System.Net.CredentialCache]::DefaultNetworkCredentials;Invoke-Expression ($wc.DownloadString('https://raw.githubusercontent.com/aspnet/Home/dev/dnvminstall.ps1'))}"
```

####Powershell
```
&{$Branch='dev';$wc=New-Object System.Net.WebClient;$wc.Proxy=[System.Net.WebRequest]::DefaultWebProxy;$wc.Proxy.Credentials=[System.Net.CredentialCache]::DefaultNetworkCredentials;Invoke-Expression ($wc.DownloadString('https://raw.githubusercontent.com/aspnet/Home/dev/dnvminstall.ps1'))}
```

This will download the DNVM script and put it in your user profile. You can check the location of DNVM by running the following in a cmd prompt:

```
where dnvm
```

> If the output of `where dnvm` shows a program files location before the user profile, or doesn't show an entry in user profile, then the install has either failed or your PATH is incorrect. After installing dnvm you should have the dnvm script in `%USERPROFILE%\.dnx\bin` and that path needs to be on your PATH.

## OS X

See the instructions on the ASP.NET Core Documentation site: [Installing ASP.NET Core on Mac OS X](https://docs.asp.net/en/latest/getting-started/installing-on-mac.html)

## Linux

See the instructions on the ASP.NET Core Documentation site: [Installing ASP.NET Core on Linux](https://docs.asp.net/en/latest/getting-started/installing-on-linux.html)

# Running an application

Now that you have DNVM, you need to use it to download a DNX to run your applications with:

```
dnvm upgrade
```

> DNVM has the concept of a stable and unstable feed. Stable defaults to NuGet.org while unstable defaults to our dev MyGet feed. So if you add `-u` or `-unstable` to any of the install or upgrade commands you will get our latest CI build of the DNX instead of the one last released on NuGet.

DNVM works by manipulating your path. When you install a runtime it downloads it and adds the path to the dnx binary to your `PATH`. After doing upgrade you should be able to run `dnvm list` and see an active runtime in the list.

You should also be able to run `dnx` and see the help text of the `dnx` command.

## Running the samples

1. Clone the ASP.NET Core Home repository: https://github.com/aspnet/home
2. Change directory to the folder of the sample you want to run
3. Run ```dnu restore``` to restore the packages required by that sample.
4. You should see a bunch of output as all the dependencies of the app are downloaded from MyGet.
5. Run the sample using the appropriate DNX command:
    - For the console app run  `dnx run`.
    - For the web apps run `dnx kestrel`.
6. You should see the output of the console app or a message that says the site is now started.
7. You can navigate to the web apps in a browser by navigating to `http://localhost:5004`

# Documentation and Further Learning

## [Community Standup](https://www.youtube.com/playlist?list=PL0M0zPgJ3HSftTAAHttA3JQU4vOjXFquF)
The community standup is held every week and streamed live to YouTube. You can view past standups in the linked playlist.

If you have questions you can also jump online during the next standup and have them answered live.

## [Wiki Documentation](https://github.com/aspnet/Home/wiki)
We have some useful documentation on the wiki of this Repo. This wiki is a central spot for docs from any part of the stack.

If you see errors, or want some extra content, then feel free to create an issue or send a pull request (see feedback section below).

## [ASP.NET/vNext](https://www.asp.net/vnext)
The vNext page on the ASP.NET site has links to some TechEd videos and articles with some good information about ASP.NET Core (formerly known as ASP.NET 5).

## [Roadmap] (https://github.com/aspnet/Home/wiki/Roadmap)
The schedule and milestone themes for ASP.NET Core.

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

# Feedback

Check out the [contributing](CONTRIBUTING.md) page to see the best places to log issues and start discussions.

