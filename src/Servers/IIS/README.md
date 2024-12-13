# IIS

ASP.NET Core IIS Web Server is a flexible secure managed Web Server to be hosted with IIS on Windows.

Documentation for ASP.NET Core IIS can be found in the [ASP.NET Core IIS Docs](https://learn.microsoft.com/aspnet/core/host-and-deploy/iis).

## Description

This folder contains all relevant code for the IIS Web Server implementation.

There are two modes for hosting application with IIS: in-process and out-of-process. In-process will run all managed code inside of the IIS worker process, while out-of-process will use IIS as a reverse-proxy to a dotnet process running Kestrel.

The following contains a description of the sub-directories.

- [AspNetCoreModuleV2/](AspNetCoreModuleV2/): Contains all native code that is part of the [ASP.NET Core Module/](https://learn.microsoft.com/aspnet/core/host-and-deploy/aspnet-core-module).
- [AspNetCoreModuleV2/AspNetCore/](AspNetCoreModuleV2/AspNetCore/): Contains the ASP.NET Core Module shim, a minimal layer for IIS to interact with the in-process and out-of-process modules.
- [AspNetCoreModuleV2/CommonLib/](AspNetCoreModuleV2/CommonLib/): Contains common code shared between all native components.
- [AspNetCoreModuleV2/CommonLibTests/](AspNetCoreModuleV2/CommonLibTests/): Contains native tests for the ASP.NET Core Module.
- [AspNetCoreModuleV2/IISLib/](AspNetCoreModuleV2/IISLib/): Contains common code for interactions with IIS.
- [AspNetCoreModuleV2/InProcessRequestHandler/](AspNetCoreModuleV2/InProcessRequestHandler/): Contains native code for in-process hosting.
- [AspNetCoreModuleV2/OutOfProcessRequestHandler/](AspNetCoreModuleV2/OutOfProcessRequestHandler/): Contains native code for out-of-process hosting.
- [AspNetCoreModuleV2/RequestHandlerLib/](AspNetCoreModuleV2/RequestHandlerLib/): Contains shared code between in-process and out-of-process hosting.
- [IIS/](IIS/): Contains managed code for hosting ASP.NET Core with in-process hosting.
- [IISIntegration/](IISIntegration/): Contains managed code for hosting ASP.NET Core with out-of-process hosting.
- [IntegrationTesting.IIS/](IntegrationTesting.IIS/): Contains testing infrastructure for starting IIS and IISExpress.

## Development Setup

### Build

IIS can only be used on Windows.

IIS requires VS C++ native components to build. VS C++ native components can be installed by following the [Build From Source instructions](https://github.com/dotnet/aspnetcore/blob/main/docs/BuildFromSource.md#on-windows).

To build this specific project from source, follow the instructions [on building the project](../../../docs/BuildFromSource.md#step-3-build-the-repo).

Or for the less detailed explanation, run the following command inside this directory.
```powershell
> ./build.cmd
```

### Test

To run the tests for this project, you can [run the tests on the command line](https://github.com/dotnet/aspnetcore/blob/main/docs/BuildFromSource.md#running-tests-on-command-line) in this directory.

Note: IIS.Tests require both IIS to be enabled, and the [ASP.NET Hosting bundle](https://learn.microsoft.com/aspnet/core/host-and-deploy/iis/hosting-bundle) which must be run after IIS has been enabled.

Or for the less detailed explanation, run the following command inside this directory.
```powershell
> ./build.cmd -t
```

You can also run project specific tests by running `dotnet test` in the `tests` directory next to the `src` directory of the project.

## More Information

For more information, see the [ASP.NET Core README](../../../README.md).
