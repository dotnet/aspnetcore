# ASP.NET vNext Home
Latest dev version: ![dev version](http://img.shields.io/myget/aspnetvnext/v/KRE-svr50-x64.svg?style=flat)  
Latest master version: ![master version](http://img.shields.io/myget/aspnetmaster/v/KRE-svr50-x64.svg?style=flat)  

The Home repository is the starting point for people to learn about ASP.NET vNext. This repo contains samples and [documentation](https://github.com/aspnet/Home/wiki) to help folks get started and learn more about what's coming in ASP..NET vNext.

ASP.NET vNext is being actively developed by the ASP.NET team assigned to the Microsoft Open Tech Hub and in collaboration with a community of open source developers. Together we are dedicated to creating the best possible platform for web development.

The samples provided in this repo are designed to show some of the features of the new framework and to provide a starting point for further exploration. The NuGet.config file in the repo points to a MyGet feed (https://www.myget.org/F/aspnetmaster/) that has all the packages being developed. This feed is updated with each preview release. To try out the latest bits under development use the dev feed instead (https://www.myget.org/F/aspnetvnext).

## Minimum Requirements

These are the current minimum requirements for the latest preview release. They do not necessarily represent what the final minimum requirements will be.

#### Windows
* Windows 7 or Windows Server 2008 R2.
* .NET 4.5.1 for hosting in IIS

#### OS X/Linux
 * Mono 3.4.1 or later (Note: On OS X use the Homebrew formula specified below to install the required version of Mono)
 * bash or zsh and curl

## Getting Started

The easiest way to get started with ASP.NET vNext is to try out the latest preview of Visual Studio "14". You can find installation instructions and getting started documentation at http://www.asp.net/vnext.

That said, you can also try out ASP.NET vNext with just a command-prompt and a text editor. The following instructions will walk you through getting your dev environment setup.

### Install the K Version Manager (KVM)

The first thing we need to do is setup the tools required to build and run an application. We will start out by getting the [K Version Manager (KVM)](https://github.com/aspnet/Home/wiki/version-manager). You use the K Version Manager to install different versions of the ASP.NET vNext runtime and switch between them.

#### Windows
To install KVM on Windows run the following command, which will download and run a script that installs KVM for the current user (requires admin privileges):
```powershell
@powershell -NoProfile -ExecutionPolicy unrestricted -Command "iex ((new-object net.webclient).DownloadString('https://raw.githubusercontent.com/aspnet/Home/master/kvminstall.ps1'))"
```
After the script has run open a new command prompt to start using KVM.

#### OS X:

To install KVM and the correct version of Mono on OS X using [Homebrew](http://brew.sh) follow the following steps: 

 * Install [Homebrew](http://brew.sh) if it is not already installed.
 * Run command `brew tap aspnet/k` to tap the ASP.NET vNext related git repositories. 
 * Run command `brew install kvm` to install KVM. This also automatically install the latest KRE package from https://www.myget.org/f/aspnetmaster/api/v2 feed.
 * Run command `source kvm.sh` on your terminal if your terminal cannot understand kvm. 

#### Linux:

To install KVM on Linux run the following command:

```
curl https://raw.githubusercontent.com/aspnet/Home/master/kvminstall.sh | sh && source ~/.kre/kvm/kvm.sh
```

Note that on Linux you need to also install [Mono](http://mono-project.com) 3.4.1 or later.

### Install the K Runtime Environment (KRE)

Now that you have KVM setup you can install the latest version of the runtime by running the following command: ```kvm install 1.0.0-alpha3```
 
This command will download the specified version of the K Runtime Environment (KRE), and put it on your user profile ready to use. You are now ready to start using ASP.NET vNext!

# Samples

The samples in this repo are basic starting points for you to experiment with.

+ [ConsoleApp](https://github.com/aspnet/Home/tree/master/samples/ConsoleApp). This is just basic console app if you want to use it as a starting point.
+ [HelloWeb](https://github.com/aspnet/Home/tree/master/samples/HelloWeb). This is a minimal startup class that shows welcome page and static file middleware. This is mostly for you to run through the steps in the readme and make sure you have everything setup and working correctly.
+ [HelloMvc](https://github.com/aspnet/Home/tree/master/samples/HelloMvc). This sample is a basic MVC app. It is not designed to show all the functionality of the new web stack, but to give you a starting point to play with features.
+ [MVC Music Store](https://github.com/aspnet/MusicStore) and [BugTracker](https://github.com/aspnet/BugTracker) are application samples that are both being ported to ASP.NET vNext. Each of these samples have their own separate repositories that you can look at.

**NOTE: The samples are pinned to a specific version of the packages. If you want to try the latest builds then update the project.json and replace the last part of the version with a '\*', so '1.0.0-alpha3' becomes '1.0.0-alpha3-\*', and then run ```kpm restore``` to pull down the latest packages**

### Running the samples

1. Clone the Home repository
2. Change directory to the folder of the sample you want to run
3. Run ```kpm restore``` to restore the packages required by that sample.
4. You should see a bunch of output as all the dependencies of the app are downloaded from MyGet. 
5. Run the sample using the appropriate K command:
    - For the console app run  ```k run```.
    - For the web apps run ```k web``` on Windows or ```k kestrel``` on Mono.
6. You should see the output of the console app or a message that says the site is now started.
7. You can navigate to the web apps in a browser by going to "http://localhost:5001" or "http://localhost:5004" if running on Mono.

### Switching to Core CLR

By default when running ASP.NET vNext applications on the Windows platform you are running on the full .NET Framework. You can switch to use the new Cloud Optimized runtime, or Core CLR, using the KVM command.

1. Run ```kvm install 1.0.0-alpha3 -svrc50``` This command gets the latest Core CLR version of the k runtime and sets it as your default. The -svrc50 switch tells it to use Core CLR. You can use -svr50 to target desktop again.
2. Run ```k web``` to run on WebListener. 
3. The first line of your output should say "Loaded Module: klr.core45.dll" instead of "Loaded Module: klr.net45.dll"
4. The HelloWeb app should work the same as when running on the full desktop .NET Framework but now as a fully self-contained app with true side-by-side versioning support.

**NOTE: There are many APIs from the .NET Framework that are not yet available when running on Core CLR. This set should get smaller and smaller as time goes on.**

**NOTE: There is no Core CLR currently available on OSX/Linux. There is only a single platform (mono45) and a single architecture (x86).**

# Feedback

Check out the [contributing](https://github.com/aspnet/Home/blob/master/CONTRIBUTING.md) page to see the best places to log issues and start discussions.
