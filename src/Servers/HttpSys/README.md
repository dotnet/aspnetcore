# HttpSys

ASP.NET Core HttpSys Web Server is a web server that uses the [Windows Hypertext Transfer Protocol Stack](https://docs.microsoft.com/en-us/iis/get-started/introduction-to-iis/introduction-to-iis-architecture#hypertext-transfer-protocol-stack-httpsys).

Documentation for ASP.NET Core HttpSys can be found in the [ASP.NET Core HTTP.sys Docs](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/httpsys).

## Description

This folder contains all relevant code for the HttpSys Web Server implementation.

- `src/`: Contains all production code for the HttpSys Web Server.
- `src/NativeInterop`: Contains the native interop layer between managed and native code.
- `src/RequestProcessing`: Contains request and response processing code.
- `samples`: Contains samples showing how to use HTTP.sys.

## Development Setup

### Build

HTTP.sys can only be used on Windows.

To build this specific project from source, you can follow the instructions [on building a subset of the code](https://github.com/dotnet/aspnetcore/blob/main/docs/BuildFromSource.md#building-a-subset-of-the-code).

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
