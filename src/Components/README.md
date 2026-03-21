# Blazor

Blazor is a framework for building modern, interactive web-based UIs with C# and Razor. For more information on using Blazor to build apps, check out [the official docs](https://blazor.net).

## Description

This folder contains the component model shared between the WebAssembly and Server hosting models for Blazor.

The following contains a description of each sub-directory in the `Components` directory.

- `Analyzers`: Contains a collection of Rosyln analyzers for Razor components
- `Authorization`: Contains source files associated with auth-related components and services in Blazor
- `Components`: Contains the implementation for Blazor's component model
- `Forms`: Contains source files for Form components in Blazor
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
- `WebView`: Contains the source files to support [Blazor Hybrid](https://github.com/dotnet/maui/tree/main/src/BlazorWebView) within [`dotnet/maui`](https://github.com/dotnet/maui). Changes in this project can be tested with `dotnet/maui` following [this guide](https://github.com/dotnet/maui/wiki/Blazor-Desktop#aspnet-core).

## Development Setup

**Note**: To build other specific projects from source, follow the instructions [on building the project](../../docs/BuildFromSource.md#step-3-build-the-repo).

### Building ASP.NET Core Components

1. You'll need to install [Node](https://nodejs.org) on your machine.

1. Ensure the repository is clean from any asset that could remain from previous version of the repository. This is recommended when switching branches, or after updating the working branch.

    ```powershell
    git clean -xdff
    ```

    You may need to kill some processes holding on files that are being deleted, like closing Visual Studio and other `msbuild` or `dotnet` processes. There may also be lingering headless 
    `chrome` processes, but they are not included in this command. The following command may help you but be aware that this could also stop other important tasks:

    ```powershell
    Get-Process dotnet, escape-node-job, msbuild, VBCSCompiler, node, vstest.console, Microsoft.CodeAnalysis.LanguageServer -ErrorAction Continue | Stop-Process;
    ```

1. Use NPM to restore the required JavaScript modules. This doesn't require an Internet connection since the sources are read from a sub-module.

    ```powershell
    npm ci --offline
    ```

1. You'll need to run the `restore` script locally to install the required dotnet dependencies and setup the repo. The `restore` script is located in the root of the repo.
    
    ```bash
    # Linux or Mac
    ./restore.sh
    ```

    ```powershell
    # Windows
    ./restore.cmd
    ```

1. Now you can build all the JavaScript assets required by the repository (including SignalR for instance) by running the following command:

     ```powershell
     npm run build
     ```

1. Build the Components:

     ```powershell
     ./src/Components/build.cmd
     ```

2. Optionally, open the Components in Visual Studio:

     ```powershell
     ./src/Components/startvs.cmd
     ```

### Test

This project contains a collection of unit tests implemented with XUnit and E2E tests implemented using Selenium. In order to run the E2E tests, you will need to have [Node v16](https://nodejs.org/en/) installed on your machine.

The E2E tests are located in the `tests/E2ETest` folder. The E2E test assets are located in the `test/testassets` directory, and it contains a top-level `TestServer` which instantiates different app servers for specific scenarios:

- Standalone Blazor WASM
- Hosted Blazor WASM
- Blazor Server
- Blazor Server with pre-rendering

Each app server mounts the same `BasicTestApp` application under each scenario (located at `tests/testassets/BasicTestApp`).

These tests are run in the CI as part of the [`aspnetcore-components-e2e`](https://dev.azure.com/dnceng/public/_build?definitionId=1026) pipeline.

#### How to run the E2E Tests

The E2E tests can be run and debugged directly from Visual Studio (as explained in the previous section). To run the tests from the command line,
follow the previous build steps and then these commands:

1. Activate the locally installed .NET by running the following command.

     ```bash
     # Linux or Mac
     source activate.sh
     ```

     ```powershell
     # Windows
     . ./activate.ps1
     ```

1. Start the tests.

     ```powershell
     dotnet test ./src/Components/test/E2ETest
     ```

Note, you may wish to filter tests using the `--filter` command (ie. `dotnet test --filter <TEST_NAME> ./src/Components/test/E2ETest`).

Please see the [`Build From Source`](https://github.com/dotnet/aspnetcore/blob/main/docs/BuildFromSource.md) docs for more information on building and testing from source.

##### WebAssembly Trimming

By default, WebAssembly E2E tests that run as part of the CI or when run in Release builds run with trimming enabled. It's possible that tests that successfully run locally might fail as part of the CI run due to errors introduced due to trimming. To test this scenario locally, either run the E2E tests in release build or with the `TestTrimmedOrMultithreadingApps` property set. For e.g.

```
dotnet test -c Release
```
or
```
dotnet build /p:TestTrimmedOrMultithreadingApps=true
dotnet test --no-build
```

## More Information

For more information, see the [ASP.NET Core README](https://github.com/dotnet/aspnetcore/blob/main/README.md).
