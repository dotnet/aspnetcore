# Blazor

Blazor is a framework for building modern, interactive web-based UIs with C# and Razor. For more information on using Blazor to build apps, check out [the official docs](https://blazor.net).

## Description

This folder contains the component model shared between the WebAssembly and Server hosting models for Blazor.

The following contains a description of each sub-directory in the `Components` directory.

- `Analyzers`: Contains a collection of Rosyln analyzers for Blazor components
- `Authorization`: Contains source files associated with auth-related components and services in Blazor
- `Components`: Contains the implementation for the Blazor component model
- `Forms`: Contains source files for Form components in Blazor
- `Ignitor`: Contains source files for inspecting Blazor apps
- `Samples`: Contains a collection of sample apps in Blazor
- `Server`: Contains the implementation for Blazor Server-specific components
- `Shared`: Contains a collection of shared constants and helper methods/classes
- `Web`: Contains source files for handling DOM events, forms, and other components
- `Web.JS`: Contains the source files for Blazor's client-side JavaScript
- `WebAssembly`: Contains the implementation for WebAssembly-specific components

## Development Setup

### Build

This project takes a dependency on MVC and SignalR packages. In order to build this project and its dependencies, run the following command inside this directory.

```powershell
> ./build.cmd
```

Or on MacOS or Linux:

```bash
$ ./build.sh
```

### Test

This project contains a collection of unit tests implemented with XUnit and E2E tests implemented using Selenium. In order to run the E2E tests, you will need to have Selenium installed on your machine.

The E2E tests are located in the top-level `tests` folder in this directory. The E2E tests consists of a top-level `TestServer` which instantiates different app servers for specific scenarios:

- Standalone Blazor WASM
- Hosted Blazor WASM
- Blazor Server
- Blazor Server with pre-rendering

Each app server mounts the same `BasicTestApp` application under each scenario.

Project-specific tests are located in each project under the `tests` directory and can be run with `dotnet test`.

