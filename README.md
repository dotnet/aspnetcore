# ASP.NET vNext Home

The Home repository is the starting point for people to learn about ASP.NET vNext. This repo contains samples and [documentation](https://github.com/aspnet/Home/wiki) to help folks get started and learn more about what's coming in ASP..NET vNext.

ASP.NET vNext is being actively developed by the ASP.NET team assigned to the Microsoft Open Tech Hub and in collaboration with a community of open source developers. Together we are dedicated to creating the best possible platform for web development.

The samples provided in this repo are designed to show some of the features of the new framework and to provide a starting point for further exploration. The NuGet.config file in the repo points to a MyGet feed (https://www.myget.org/F/aspnetmaster/) that has all the packages being developed. This feed is updated with each preview release. To try out the latest bits under development use the dev feed instead (https://www.myget.org/F/aspnetvnext).

## Minimum Requirements

These are the current minimum requirements for the latest preview release. They do not necessarily represent what the final minimum requirements will be.

### Windows
* Windows 7 or Windows Server 2008 R2.
* .NET 4.5.1 for hosting in IIS

### OS X/Linux
 * Mono >= 3.4.1 - Currently this means compiling Mono from source from https://github.com/mono/mono (Note: See instructions below to install the Homebrew formula that can install the required mono)
 * On Linux, you may need to run `mozroots --import --sync` after installing mono
 * bash or zsh and curl

## Getting Started

The first thing we need to do is setup the tools required to build and run an application. We will start out by getting the [K Version Manager (KVM)](https://github.com/aspnet/Home/wiki/version-manager). You use the K Version Manager to install different versions of the ASP.NET vNext runtime and switch between them.

### Windows
To install KVM on Windows run the following command, which will download and run a script that installs KVM for the current user:
```powershell
@powershell -NoProfile -ExecutionPolicy unrestricted -Command "iex ((new-object net.webclient).DownloadString('https://raw.githubusercontent.com/aspnet/Home/master/kvminstall.ps1'))"
```
### OS X:

To install KVM and the correct version of Mono on OS X using [Homebrew](http://brew.sh) follow the following steps: 

 * Install [Homebrew](http://brew.sh) if it is not already installed.
 * Run command `brew tap aspnet/k` to tap the ASP.NET vNext related git repositories. 
 * Run command `brew install kvm` to install KVM. This also automatically install the latest KRE package from https://www.myget.org/f/aspnetmaster/api/v2 feed.
 * Run command `source kvm.sh` on your terminal if your terminal cannot understand kvm. 
 * To make sure you get the KRE packages from the right myget feed execute `export KRE_FEED=https://www.myget.org/f/aspnetmaster/api/v2`.

### Linux:
```
curl https://raw.githubusercontent.com/aspnet/Home/master/kvminstall.sh | sh && source ~/.kre/kvm/kvm.sh
```

This downloads KVM from this repository and puts it on your machine. Alternatively, you you could clone the repo and get it:
* Clone the repository
* On the command line execute
 * ```kvm setup``` on Windows or
 * ```sh kvminstall.sh && source ~/.kre/kvm/kvm.sh``` on OSX/Linux

This command will setup your environment, getting it ready to install a version of the runtime. It adds KVM to your path and puts it in your user profile. Once you have KVM then you need to get a version of the runtime:
* Execute ```kvm install 1.0.0-alpha3```. This command will download the named version of the KRE and put it on your user profile ready to use. You can get the latest version by running ```kvm upgrade``` but 0446 was the last version explicitly tested. see the [KVM page](https://github.com/aspnet/Home/wiki/version-manager) for more information on KVM.
* Navigate to samples\ConsoleApp
* Run ```kpm restore```. This downloads the System.Console package so the app can do Console.WriteLine
* Run ```k run```
* You should see a message saying "Hello World"
* Type
 * ```SET KRE_TRACE=1``` on Windows or
 * ```export KRE_TRACE=1``` on OSX/Linux
* Run ```k run```
* You should now see compiler output as well as the "Hello World" message

```
:: getting started
git clone https://github.com/aspnet/Home.git
cd Home
kvm setup
kvm install 1.0.0-alpha3 -p

cd samples\ConsoleApp
kpm restore
k run

SET KRE_TRACE=1
k run
```

# Samples

## Sandbox Samples

These samples, in this repo, are just basic starting points for you to experiment with features. Since there is no File->New Project we thought some simple samples to take the place of scaffolding would be convenient.

+ [ConsoleApp](https://github.com/aspnet/Home/tree/master/samples/ConsoleApp). This is just basic console app if you want to use it as a starting point.
+ [HelloWeb](https://github.com/aspnet/Home/tree/master/samples/HelloWeb). This is a minimal startup class that shows welcome page and static file middleware. This is mostly for you to run through the steps in the readme and make sure you have everything setup and working correctly.
+ [HelloMvc](https://github.com/aspnet/Home/tree/master/samples/HelloMvc). This sample is a basic MVC app. It is not designed to show all the functionality of the new web stack, but to give you a starting point to play with features.

**NOTE: The samples are pinned to a specific version of the packages. If you want to try the latest builds then update the project.json and replace the last part of the version with a '\*', so '0.1-alpha-build-267' becomes '0.1-alpha-\*', and then run ```kpm restore``` to pull down the latest packages**

## Feature Samples
The [Entropy repo](https://github.com/aspnet/Entropy) contains samples of specific features in isolation. Each directory contains just enough code to show an aspect of a feature.

## Application Samples
[MVC Music Store](https://github.com/aspnet/MusicStore) and [BugTracker](https://github.com/aspnet/BugTracker) application are both being ported. Each of these have their own repository that you can look at.

# Running the samples

## Running HelloWeb

1. Clone the repository
2. Change directory to Samples\HelloWeb
3. Run ```kpm restore```
4. You should see a bunch of output as all the dependencies of the app are downloaded from MyGet. The K commands all operate on the app that is in the current directory.
5. Run ```K web``` to run on WebListener. Or run ```K kestrel``` to run on Mono. 
6. You should see build output and a message to show the site is now started
7. Navigate to "http://localhost:5001" or "http://localhost:5004" in case of Mono
8. You should see the welcome page
9. Navigate to "http://localhost:5001/image.jpg" or "http://localhost:5004/image.img" in case of Mono. 
10. You should see an image served with the static file middleware

If you can do all of the above then everything should be working. You can try out the WebFx sample now to see some more of the new stack. You should run ```kpm restore``` before using any sample for the first time.

# Switching to Core CLR


By default when running the applications you are running against Desktop CLR (4.5), you can change that using the KVM command.

1. Run ```kvm install 1.0.0-alpha3 -svrc50``` This command gets the latest Core CLR version of the k runtime and sets it as your default. The -svrc50 switch tells it to use Core CLR, you can use -svr50 to target desktop again.
2. Run ```K web``` to run on WebListener. Or run ```K kestrel``` to run on Mono. 
3. The first line of your output should say "Loaded Module: klr.core45.dll" instead of "Loaded Module: klr.net45.dll"
4. The HelloWeb app should work the same as when running on Desktop CLR.

**NOTE: There are going to be parts of the stack that work on Desktop but do not work on Core CLR. This set should get smaller and smaller as time goes on, but it is entirely likely as you use Core CLR you will hit errors that can't be worked around as the Core CLR surface area just does not exist yet.**

**NOTE: There is no Core CLR currently on OSX/Linux. There is only a single platform (mono45) and a single architecture (x86).**

#Core CLR Packages

Currently the BCL is split into some fairly fine grained packages, which was one of the goals of this effort. However, the packages that exist today do not necessarily represent the list of packages that we will end up with. We are still experimenting with what makes sense to be a package and what the experience should be.

# Known Issues

* Core CLR doesn't currently work on Windows OSes earlier than Windows 8

# Feedback

Check out the [contributing](https://github.com/aspnet/Home/blob/master/CONTRIBUTING.md) page to see the best places to log issues and start discussions.
