# Hosting

Hosting provides the entry point for configuring and running your application.

Documentation for Hosting can be found in the [ASP.NET Core Web Host](https://docs.microsoft.com/aspnet/core/fundamentals/host/web-host) and [.NET Generic Host in ASP.NET Core](https://docs.microsoft.com/aspnet/core/fundamentals/host/generic-host) docs.

## Description

The following contains a description of the sub-directories.

- `Abstractions`: Contains the main Hosting and Startup interfaces.
- `Hosting`: Contains the default implementations for GenericHost, WebHost, and Startup.
- `Server.Abstractions`: Contains the interfaces for Server implementations.
- `Server.IntegrationTesting`: Contains classes to help deploy servers for testing across the repo.
- `TestHost`: Contains a server implementation for in-memory testing against a server.
- `WindowsServices`: Contains methods to run an application as a Windows service.
- `samples`: Contains a few sample apps that show examples of using hosting.

## Development Setup

### Build

To build this specific project from source, you can follow the instructions [on building a subset of the code](../../docs/BuildFromSource.md#building-a-subset-of-the-code).

### Test

To run the tests for this project, [run the tests on the command line](../../docs/BuildFromSource.md#running-tests-on-command-line) in this directory.

## More Information

For more information, see the [ASP.NET Core README](../../README.md).
