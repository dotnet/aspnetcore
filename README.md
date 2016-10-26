# ASP.NET Core Module

The ASP.NET Core Module is an IIS Module which is responsible for process
management of ASP.NET Core http listeners and to proxy requests to the process
that it manages.

## Pre-requisites for building

### Windows 8.1+ or Windows Server 2012 R2+

### Visual C++ Build Tools

[Download](http://download.microsoft.com/download/D/2/3/D23F4D0F-BA2D-4600-8725-6CCECEA05196/vs_community_ENU.exe)
and install Visual Studio 2015. In Visual Studio 2015 C++ tooling is no longer
installed by default, you must chose "Custom" install and select Visual C++.

![Visual C++](https://cloud.githubusercontent.com/assets/4734691/18014419/b06e589a-6b77-11e6-9393-4eed32186ca3.png)

Optionally, if you don't want to install Visual Studio you can just install the
[Visual C++ build tools](http://landinghub.visualstudio.com/visual-cpp-build-tools).

### MSBuild

If you have installed Visual Studio, you should already have MSBuild. If you
installed the Visual C++ build tools, you will need to download and install
[Microsoft Build Tools 2015](https://www.microsoft.com/en-us/download/details.aspx?id=48159)

Once you have installed MSBuild, you can add it your path. The default location
for MSBuild is `%ProgramFiles(x86)%\MSBuild\14.0\Bin`

### Windows Software Development Kit for Windows 8.1

[Download](http://download.microsoft.com/download/B/0/C/B0C80BA3-8AD6-4958-810B-6882485230B5/standalonesdk/sdksetup.exe)
and install the Windows SDK for Windows 8.1. From the Feature list presented,
ensure you select *Windows Software Development Kit*.

If chose to install from the command prompt, you can run the following command.
````
.\sdksetup.exe /features OptionId.WindowsDesktopSoftwareDevelopmentKit
````

## How to build


```powershell

# Clean
.\build.cmd /target:clean

# Build
.\build.cmd

# Build 64-bit
.\build.cmd /property:platform=x64

# Build in Release Configuration
.\build.cmd /property:configuration=release
```

## Contributions

Check out the [contributing](https://github.com/aspnet/Home/blob/dev/CONTRIBUTING.md)
page to see the best places to log issues and start discussions.

This project has adopted the code of conduct defined by the [Contributor Covenant](http://contributor-covenant.org/)
to clarify expected behavior in our community.
For more information see the [.NET Foundation Code of Conduct](http://www.dotnetfoundation.org/code-of-conduct).

### .NET Foundation

This project is supported by the [.NET Foundation](http://www.dotnetfoundation.org).


