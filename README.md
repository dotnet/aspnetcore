# ASP.NET vNext Home

In the next version of ASP.NET we are working with multiple teams around Microsoft to create a lean, composable .NET stack that provides a familiar and modern framework for web and cloud scenarios.

The Home repository is the starting point for people to learn about ASP.NET vNext, it contains samples and [documentation](https://github.com/aspnet/Home/wiki) to help folks get started and learn more about what we are doing.

These products are actively developed by the ASP.NET team assigned to the Microsoft Open Tech Hub and in collaboration with a community of open source developers. Together we are dedicated to creating the best possible platform for web development.

The samples provided are designed to show some of the features of the new framework as well as setting up a sandbox for you to try out some of the new features. The NuGet.config file in the repo points to a MyGet feed (https://www.myget.org/F/aspnetvnext/) that has all the packages being developed. The feed is updated every time a full build succeeds.


# Minimum Requirements

These are the current minimum requirements, they do not necesarilly represent our RTM minimum.

### Windows
* Windows 7 or greater, though Core CLR will only work on Windows 8 today. If using Core CLR you will need to be on Windows 8 or above. At RTM the whole stack will support Windows 7+ and Windows Server 2008 R2+.
* .NET 4.5.1 for hosting in IIS

### OSX/Linux
 * Mono >= 3.4.1 - Currently this means compiling Mono from source from https://github.com/mono/mono
 * On Linux, you may need to run `mozroots --import --sync` after installing mono
 * bash or zsh and curl

#Getting Started

The first thing we need to do is setup the tools required to build and run an application. We will start out by getting the [K Version Manager (KVM)](https://github.com/aspnet/Home/wiki/version-manager)

The easiest way to get the KVM on Windows is to set the powershell execution policy 
```powershell
 Set-ExecutionPolicy RemoteSigned
```
and the run the following command:
```powershell
@powershell -NoProfile -ExecutionPolicy unrestricted -Command "iex ((new-object net.webclient).DownloadString('https://raw.githubusercontent.com/aspnet/Home/master/kvminstall.ps1'))"
```
or on OSX:
Mono and KVM (K Version manager) required to develop & run next generation ASP.NET applications on OSX is available as a Homebrew formula. To install the necessary development tools: 

 * Open a terminal on your MAC
 * Install [Homebrew](http://brew.sh) by running the following command. If you already have Homebrew installed skip this step. 
```ruby -e "$(curl -fsSL https://raw.github.com/Homebrew/homebrew/go/install)"```
 * Run command `brew tap aspnet/k` to tap the aspnet vnext related git repositories. 
 * Run command `brew install kvm` to install KVM. This also automatically install the latest KRE package from https://www.myget.org/f/aspnetvnext/api/v2 feed.

Now you are set for developing next general ASP.NET applications on OSX. A few things to note:
 * If you would like to use a different KRE version for your development, you can install the desired version using KVM. See [KVM page](https://github.com/aspnet/Home/wiki/version-manager) for more information. 
 * Run command `source kvm.sh` on your terminal if your terminal cannot understand kvm. 

or on Linux:
```
curl https://raw.githubusercontent.com/aspnet/Home/master/kvminstall.sh | sh && source ~/.kre/kvm/kvm.sh
```

This downloads KVM from this repository and puts it on your machine. Alternatively, you you could clone the repo and get it:
* Clone the repository
* On the command line execute
 * ```kvm setup``` on Windows or
 * ```sh kvminstall.sh && source ~/.kre/kvm/kvm.sh``` on OSX/Linux

This command will setup your environment, getting it ready to install a version of the runtime. It adds KVM to your path and puts it in your user profile. Once you have KVM then you need to get a version of the runtime:
* Execute ```kvm install 0.1-alpha-build-0446```. This command will download the named version of the KRE and put it on your user profile ready to use. You can get the latest version by running ```kvm upgrade``` but 0446 was the last version explicitly tested. see the [KVM page](https://github.com/aspnet/Home/wiki/version-manager) for more information on KVM.
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
kvm install 0.1-alpha-build-0446 -p

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
5. Run ```K web```
6. You should see build output and a message to show the site is now started
7. Navigate to "http://localhost:5001"
8. You should see the welcome page
9. Navigate to "http://localhost:5001/image.jpg"
10. You should see an image served with the static file middleware

If you can do all of the above then everything should be working. You can try out the WebFx sample now to see some more of the new stack. You should run ```kpm restore``` before using any sample for the first time.

# Switching to Core CLR


By default when running the applications you are running against Desktop CLR (4.5), you can change that using the KVM command.

1. Run ```kvm install 0.1-alpha-build-0446 -svrc50``` This command gets the latest Core CLR version of the k runtime and sets it as your default. The -svrc50 switch tells it to use Core CLR, you can use -svr50 to target desktop again.
2. Run ```K web```
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
