# Getting Started

## Dependencies
In order to successfully build and test *SignalR* you'll need to ensure that your system has the following installed:
* [NodeJS](https://nodejs.org/) version 6.9 or later
* NPM *(typically bundled with NodeJS)*

## How To Build
Full instructions for how to build repositories within the [aspnet](https://github.com/aspnet) family of repositories can be found [in  the Home repository](https://github.com/aspnet/Home/wiki/Building-from-source).

The short-hand version of that is to simply run the  `build.cmd` or `build.sh` script that's in the root of the *SignalR* repository. The build script will automatically install the necessary .NET SDK version.

If, after running `build.cmd` or `build.sh`, you did not get a successful build, please refer to the [Troubleshooting](#troubleshooting) section below. If your problem isn't covered, please check that you've met all of the prerequisites. If you still can't get the solution to build successfully, please open an issue so that we might assist and please consider updating this documentation to help others in the future.

## Troubleshooting
Below are some tips for troubleshooting common issues.

### Project Load Failures in Visual Studio 
In order to property load several of the projects in the solution, it is necessary that `%USERPROFILE%\.dotnet\x64` be in your `PATH` variable. If you experience issues loading projects in Visual Studio, please ensure that your `PATH` is configured correctly.

### NPM Errors
Running `build.cmd` or `build.sh` immediately after installing NodeJS can cause an NPM error: `EPERM: operation not permitted, rename`. Executing the NPM command `npm cache clean` will fix this issue.
