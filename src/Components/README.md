# Blazor

Blazor is a framework for building modern, interactive web-based UIs with C# and Razor. For more information on using Blazor to build apps, check out [the official docs](https://blazor.net).

## Description

This folder contains the component model shared between the WebAssembly and Server hosting models for Blazor.

The following contains a description of each sub-directory in the `Components` directory.

- `Analyzers`: Contains a collection of Rosyln analyzers for Blazor components
- `Authorization`: Contains source files associated with auth-related components and services in Blazor
- `Components`: Contains the implementation for the Blazor component model
- `Forms`: Contains source files for Form components in Blazor
- `Ignitor`: A library for testing Blazor Server apps
- `Samples`: Contains a collection of sample apps in Blazor
- `Server`: Contains the implementation for Blazor Server-specific components
- `Shared`: Contains a collection of shared constants and helper methods/classes
- `Web`: Contains source files for handling DOM events, forms, and other components
- `Web.JS`: Contains the source files for Blazor's client-side JavaScript
- `WebAssembly`: Contains the implementation for WebAssembly-specific components
  - `Authentication.Msal`: Contains the implementation for MSAL (Microsoft Authentication Library) auth in WASM applications
  - `DevServer`: Contains the implementation for the Blazor dev server
  - `JSInterop`: Contains the implementation for methods that allow invoking JS from .NET code
  - `Sdk`: Contains the MSBuild definitions for the Blazor WASM SDK
  - `Server`: Contains the implementation for WASM-specific extension methods and the launch logic for the debugging proxy
  - `WebAssembly`: Contains WebAssembly-specific implementations of the renderer, HostBuilder, etc.
  - `WebAssembly.Authentication`: Contains the WASM-specific implementations

## Development Setup

### Build

To build this specific project from source, follow the instructions [on building a subset of the code](../../docs/BuildFromSource.md#building-a-subset-of-the-code).

**Note:** You also need to run the preceding `build` command in the command line before building in VS to ensure that the Web.JS dependency is built.

### Test

This project contains a collection of unit tests implemented with XUnit and E2E tests implemented using Selenium. In order to run the E2E tests, you will need to have Selenium installed on your machine.

The E2E tests are located in the top-level `tests` folder in this directory. The E2E tests consists of a top-level `TestServer` which instantiates different app servers for specific scenarios:

- Standalone Blazor WASM
- Hosted Blazor WASM
- Blazor Server
- Blazor Server with pre-rendering

Each app server mounts the same `BasicTestApp` application under each scenario.

To run the tests for this project, [run the tests on the command line](../../docs/BuildFromSource.md#running-tests-on-command-line) in this directory.

By default, WebAssembly E2E tests that run as part of the CI or when run in Release builds run with trimming enabled. It's possible that tests that successfully run locally might fail as part of the CI run due to errors introduced due to trimming. To test this scenario locally, either run the E2E tests in release build or with the `TestTrimmedApps` property set. For e.g.

```
dotnet test -c Release
```
or
```
dotnet build /p:TestTrimmedApps=true
dotnet test --no-build
```

## More Information

For more information, see the [ASP.NET Core README](../../README.md).
