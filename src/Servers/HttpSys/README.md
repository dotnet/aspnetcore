# HttpSys

ASP.NET Core HttpSys Web Server is a web server that uses the [Windows Hypertext Transfer Protocol Stack](https://learn.microsoft.com/iis/get-started/introduction-to-iis/introduction-to-iis-architecture#hypertext-transfer-protocol-stack-httpsys).

Documentation for ASP.NET Core HttpSys can be found in the [ASP.NET Core HTTP.sys Docs](https://learn.microsoft.com/aspnet/core/fundamentals/servers/httpsys).

## Description

This folder contains all relevant code for the HttpSys Web Server implementation.

- [src/](src/): Contains all production code for the HttpSys Web Server.
- [src/NativeInterop/](src/NativeInterop): Contains the native interop layer between managed and native code.
- [src/RequestProcessing/](src/RequestProcessing): Contains request and response processing code.
- [samples/](samples/): Contains samples showing how to use HTTP.sys.

## Development Setup

### Build

HTTP.sys can only be used on Windows.

To build this specific project from source, follow the instructions [on building the project](../../../docs/BuildFromSource.md#step-3-build-the-repo).

Or for the less detailed explanation, run the following command inside this directory.
```powershell
> ./build.cmd
```

### Test

To run the tests for this project, you can [run the tests on the command line](https://github.com/dotnet/aspnetcore/blob/main/docs/BuildFromSource.md#running-tests-on-command-line) in this directory.

Or for the less detailed explanation, run the following command inside this directory.
```powershell
> ./build.cmd -t
```

You can also run project specific tests by running `dotnet test` in the `tests` directory next to the `src` directory of the project.

## More Information

For more information, see the [ASP.NET Core README](../../../README.md).
