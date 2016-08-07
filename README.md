# ASP.NET 5 Home
Latest dev version: [![dev version](http://img.shields.io/myget/aspnetvnext/v/dnx-clr-win-x86.svg?style=flat)](https://www.myget.org/gallery/aspnetvnext)<br>
Latest master version: [![master version](http://img.shields.io/myget/aspnetmaster/v/kre-clr-win-x86.svg?style=flat)](https://www.myget.org/gallery/aspnetmaster)

The Home repository is the starting point for people to learn about ASP.NET 5. This repo contains samples and [documentation](https://github.com/aspnet/Home/wiki) to help folks get started and learn more about what's coming in ASP.NET 5.

ASP.NET 5 is being actively developed by the ASP.NET team assigned to the Microsoft Open Tech Hub and in collaboration with a community of open source developers. Together we are dedicated to creating the best possible platform for web development.

The samples provided in this repo are designed to show some of the features of the new framework and to provide a starting point for further exploration. All the component packages are available on NuGet. To try out the latest bits under development switch to the dev branch of the Home repo and use the dev feed in NuGet.config (https://www.myget.org/F/aspnetvnext).


## Contents

* [Minimum Requirements](#minimum-requirements)
* [Getting Started](#getting-started)
* [Samples](#samples)
* [Documentation and Further Learning](#documentation-and-further-learning)
* [Repos and Projects](#repos-and-projects)
* [Feedback](#feedback)

## Minimum Requirements

These are the current minimum requirements for the latest preview release. They do not necessarily represent what the final minimum requirements will be.

### Windows
* Windows 7 or Windows Server 2008 R2.
* .NET 4.5.1 for hosting in IIS

### OS X/Linux
 * Mono 3.4.1 or later (Note: On OS X use the Homebrew formula specified below to install the required version of Mono)
 * bash or zsh and curl


## Getting Started

The easiest way to get started with ASP.NET 5 is to try out the latest preview of Visual Studio 2015 Preview. You can find installation instructions and getting started documentation at http://www.asp.net/vnext.

That said, you can also try out ASP.NET 5 with just a command-prompt and a text editor. The following instructions will walk you through getting your dev environment setup.

### Install the .NET Version Manager (DNVM)

Note: DNVM was formerly called KVM.

The first thing we need to do is setup the tools required to build and run an application. We will start out by getting the [.NET Version Manager (DNVM)](https://github.com/aspnet/Home/wiki/version-manager). We use the .NET Version Manager to install different versions of the .NET Execution Environment (DNX) (used by the ASP.NET 5 runtime) and switch between them.

#### Windows
To install DNVM on Windows run the following command, which will download and run a script that installs DNVM for the current user (requires admin privileges for Powershell).

##### Stable(ish) (KVM)
This will use the currently released version of `kvm` (from the `master` branch of this repo). (KVM was renamed to DNVM after the current public release.)

```
@powershell -NoProfile -ExecutionPolicy unrestricted -Command "iex ((new-object net.webclient).DownloadString('https://raw.githubusercontent.com/aspnet/Home/master/kvminstall.ps1'))"
```

##### Optimistic (DNVM)
If you want to run on the bleeding edge and install the latest development version of DNVM (formerly called KVM), run the following command:

```
@powershell -NoProfile -ExecutionPolicy unrestricted -Command "&{$Branch='dev';iex ((new-object net.webclient).DownloadString('https://raw.githubusercontent.com/aspnet/Home/dev/dnvminstall.ps1'))}"
```

After the script has run open a new command prompt to start using DNVM.

#### OS X:

To install KVM/DNVM and the correct version of Mono on OS X using [Homebrew](http://brew.sh) follow the following steps: 

 * Install [Homebrew](http://brew.sh) if it is not already installed.
 * Run command `brew tap aspnet/k` to tap the ASP.NET 5 related git repositories. If you had already tapped the repo for previous releases, run `brew untap aspnet/k` to delete the old commands and tap again to get the updated brew scripts.

##### Stable(ish) (KVM)

 * Run command `brew install kvm` to install KVM. This also automatically install the latest KRE package from https://www.nuget.org/api/v2 feed.
 * Run command `source kvm.sh` on your terminal if your terminal cannot understand kvm. 
 
##### Optimistic (DNVM)

 * Run command `brew install dnvm` to install dnvm. This also automatically install the latest DNX package from https://www.nuget.org/api/v2 feed.
 * Run command `source dnvm.sh` on your terminal if your terminal cannot understand dnvm. 

#### Linux:

Note that on Linux you need to also install [Mono](http://mono-project.com) 3.4.1 or later.

##### Stable(ish) (KVM)
Installing KVM requires `curl`. Do verify if that is installed on the machine. Next install KVM on Linux run the following command:

```
curl -sSL https://raw.githubusercontent.com/aspnet/Home/master/kvminstall.sh | sh && source ~/.k/kvm/kvm.sh
```

##### Optimistic (DNVM)
If you want to run on the bleeding edge and install the latest development version of DNVM (formerly called KVM), run the following command:

```
curl -sSL https://raw.githubusercontent.com/aspnet/Home/dev/dnvminstall.sh | DNX_BRANCH=dev sh && source ~/.dnx/dnvm/dnvm.sh
```


### Install the .NET Execution Environment (DNX)

##### Stable(ish) (KRE)

Now that you have KVM set up you can install the latest version of the runtime by running the following command: ```kvm upgrade```
 
This command will download the specified version of the KRE, and put it on your user profile ready to use. You are now ready to start using ASP.NET 5 with the KRE!

##### Optimistic (DNX)

Note: DNX was formerly called KRE.

Now that you have DNVM set up you can install the latest version of the runtime by running the following command: ```dnvm upgrade```
 
This command will download the specified version of the DNX, and put it on your user profile ready to use. You are now ready to start using ASP.NET 5 with the DNX!


## Samples

The samples in this repo are basic starting points for you to experiment with.

+ [ConsoleApp](https://github.com/aspnet/Home/tree/master/samples/ConsoleApp). This is just basic console app if you want to use it as a starting point.
+ [HelloWeb](https://github.com/aspnet/Home/tree/master/samples/HelloWeb). This is a minimal startup class that shows welcome page and static file middleware. This is mostly for you to run through the steps in the readme and make sure you have everything setup and working correctly.
+ [HelloMvc](https://github.com/aspnet/Home/tree/master/samples/HelloMvc). This sample is a basic MVC app. It is not designed to show all the functionality of the new web stack, but to give you a starting point to play with features.
+ [MVC Music Store](https://github.com/aspnet/MusicStore) and [BugTracker](https://github.com/aspnet/BugTracker) are application samples that are both being ported to ASP.NET 5. Each of these samples have their own separate repositories that you can look at.

### Running the samples

1. Clone the Home repository
2. Change directory to the folder of the sample you want to run
3. Run ```kpm restore``` or ```dnu restore``` to restore the packages required by that sample.
4. You should see a bunch of output as all the dependencies of the app are downloaded from MyGet. 
5. Run the sample using the appropriate `dnx` command:
    - For the console app run  ```dnx . run```.
    - For the web apps run ```dnx . web``` on Windows or ```dnx . kestrel``` on Mono.
6. You should see the output of the console app or a message that says the site is now started.
7. You can navigate to the web apps in a browser by going to "http://localhost:5001" or "http://localhost:5004" if running on Mono.

### Switching to Core CLR

By default when running ASP.NET 5 applications on the Windows platform you are running on the full .NET Framework. You can switch to use the new Cloud Optimized runtime, or Core CLR, using the DNVM command.

1. Run ```dnvm upgrade -runtime CoreCLR``` This command gets the latest Core CLR version of the DNX and sets it as your default. The `-runtime CoreCLR` switch tells it to use Core CLR. You can use `-r CLR` to target desktop again.
2. Run ```dnx . web``` to run on WebListener. 
3. The first line of your output should say "Loaded Module: dnx.core45.dll" instead of "Loaded Module: dnx.net45.dll"
4. The HelloWeb app should work the same as when running on the full desktop .NET Framework but now as a fully self-contained app with true side-by-side versioning support.

**NOTE: There are many APIs from the .NET Framework that are not yet available when running on Core CLR. This set should get smaller and smaller as time goes on.**

**NOTE: There is no Core CLR currently available on OSX/Linux. There is only a single platform (mono45) and a single architecture (x86).**

## Documentation and Further Learning

### [Community Standup](http://www.youtube.com/playlist?list=PL0M0zPgJ3HSftTAAHttA3JQU4vOjXFquF)
The community standup is held every week and streamed live to YouTube. You can view past standups in the linked playlist. 

If you have questions you can also jump online during the next standup and have them answered live.

### [Official Documentation](http://aspnet.readthedocs.org/)
Official documentation is available on http://aspnet.readthedocs.org/.

If you see errors, or want some extra content, please log an issue on https://github.com/aspnet/Docs/issues.

### [Wiki Documentation](https://github.com/aspnet/Home/wiki)
We have some useful documentation on the wiki of this Repo. This wiki is a central spot for docs from any part of the stack.

If you see errors, or want some extra content, then feel free to create an issue or send a pull request (see feedback section below).

### [ASP.NET/vNext](http://www.asp.net/vnext)
The vNext page on the ASP.NET site has links to some TechEd videos and articles with some good information about ASP.NET 5.

## Repos and Projects

These are some of the most common repos:

* [DependencyInjection](https://github.com/aspnet/DependencyInjection) - basic dependency injection infrastructure and default implementation
* [EntityFramework](https://github.com/aspnet/EntityFramework) - data access technology
* [Identity](https://github.com/aspnet/Identity) - users and membership system
* [DNX](https://github.com/aspnet/DNX) - core runtime, project system, loader
* [MVC](https://github.com/aspnet/Mvc) - MVC framework for web apps and services
* [SignalR-Server](https://github.com/aspnet/SignalR-Server) - real-time 

A description of all the repos is [here](https://github.com/aspnet/Home/wiki/Repo-List).

## Feedback

Check out the [contributing](https://github.com/aspnet/Home/blob/master/CONTRIBUTING.md) page to see the best places to log issues and start discussions.
