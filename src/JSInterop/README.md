# JSInterop

This directory contains sources for [`Microsoft.JSInterop`](https://www.nuget.org/packages/Microsoft.JSInterop), a package that provides abstractions and features for interop between .NET and JavaScript code.

The primary use case is for applications built with Blazor. For usage information, see the following documentation:

 * [Call JavaScript functions from .NET methods in ASP.NET Core Blazor](https://learn.microsoft.com/aspnet/core/blazor/call-javascript-from-dotnet)
 * [Call .NET methods from JavaScript functions in ASP.NET Core Blazor](https://learn.microsoft.com/aspnet/core/blazor/call-dotnet-from-javascript)

## Description

This section provides a brief overview of the architecture.

`Microsoft.JSInterop` is a .NET package with the following roles:

 * Defining abstractions to describe how .NET code can invoke JavaScript code and pass parameters. These abstractions include `IJSRuntime`, `IJSInProcessRuntime`, `DotNetObjectReference`, `IJSObjectReference`, and others.
 * Providing platform-independent abstract base class implementations of those abstractions, such as `JSRuntime` and `JSObjectReference`. These implement common logic around handling errors and asynchrony, even though they are independent of any particular runtime environment.
 * Providing extension methods on `IJSRuntime` that simplify making calls with differing numbers of parameters, cancellation tokens, and other characteristics.

For these types to become usable in a particular runtime environment, such as Blazor Server or Blazor WebAssembly, the runtime environment implements its own concrete subclasses that know how to dispatch calls to the actual JavaScript runtime that is available in that environment. For example, Blazor Server uses the SignalR-based circuit to send invocations to the end user's browser.

`Microsoft.JSInterop.JS` is the JavaScript-side counterpart to the preceding. It runs within a standard web browser environment, receives the invocations from .NET code, executes them, and sends back results in the format understood by the `JSRuntime` base class. This includes special handling for certain parameter types such as `ElementReference` and `DotNetObjectReference`. `Microsoft.JSInterop.JS` also exposes JavaScript functions that can be used to issue calls from JavaScript to .NET.

Since `Microsoft.JSInterop.JS` is platform-independent, runtime environments such as Blazor Server or Blazor WebAssembly must initialize it by registering environment-specific callbacks that know how to dispatch invocations across their own communication channels.

## Development Setup

### Build and test

To build the .NET code, you can:

 * Run `dotnet build` in the `Microsoft.JSInterop/src` directory. You can also read more [on building the project](../../docs/BuildFromSource.md#step-3-build-the-repo).
 * Run `dotnet build` or `dotnet test` in the `Microsoft.JSInterop/test` directory. You can also read more about how to [run the tests on the command line](../../docs/BuildFromSource.md#running-tests-on-command-line).

Alternatively, open `JSInterop.slnf` in Visual Studio.

To build the JavaScript code, execute the following commands in a command shell:

 * `cd Microsoft.JSInterop.JS/src`
 * `npm run preclean`
 * `npm run build`

**Warning:** Due to special requirements related to ASP.NET Core's CI process for Linux distributions, we store the compiled JavaScript artifacts for `Microsoft.JSInterop.JS` in source control in the `Microsoft.JSInterop.JS/src/dist` directory. If you edit and build JavaScript sources, your Git client should indicate that those outputs have changed. You will need to include changes to those `dist` files in any PRs that you submit. When this leads to merge conflicts, we have to resolve them manually by rebasing and rebuilding.

## More Information

For more information, see the [ASP.NET Core README](../../README.md).
