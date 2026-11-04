# Kestrel

Kestrel is our cross-platform web server that is included and enabled by default in ASP.NET Core.

Documentation for ASP.NET Core Kestrel can be found in the [ASP.NET Core Kestrel Docs](https://learn.microsoft.com/aspnet/core/fundamentals/servers/kestrel).

## Description

The following contains a description of the sub-directories.

- [Core/](Core/): Contains the main server implementation for Kestrel.
- [Kestrel/](Kestrel/): Contains the public API exposed to use Kestrel.
- [test/](test/): Contains End to End tests for Kestrel.
- [Transport.Sockets/](Transport.Sockets/):Contains the Sockets transport for connection management.
- [Transport.Quic/](Transport.Quic/): Contains the QUIC transport for connection management.

## Development Setup

### Build

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
