# JSInterop

This directory contains sources for [`Microsoft.JSInterop`](https://www.nuget.org/packages/Microsoft.JSInterop), a package that provides abstractions and features for interop between .NET and JavaScript code. The JavaScript implementation and npm package are under [`Web.JS`](../Web.JS).

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

The JavaScript-side counterpart runs within a standard web browser environment, receives the invocations from .NET code, executes them, and sends back results in the format understood by the `JSRuntime` base class. This includes special handling for certain parameter types such as `ElementReference` and `DotNetObjectReference`. It also exposes JavaScript functions that can be used to issue calls from JavaScript to .NET.

Since `Microsoft.JSInterop.JS` is platform-independent, runtime environments such as Blazor Server or Blazor WebAssembly must initialize it by registering environment-specific callbacks that know how to dispatch invocations across their own communication channels.

## Experimental JavaScript-to-.NET cancellation

The draft `invokeMethodAsyncWithCancellation` entry points accept an `AbortSignal`.
The invoked `[JSInvokable]` method declares a final `CancellationToken`, which interop
supplies in addition to the ordinary arguments:

```javascript
const controller = new AbortController();
const pending = dotNetObject.invokeMethodAsyncWithCancellation(
    "Load", controller.signal, query);
controller.abort();
await pending;
```

```csharp
[JSInvokable]
public Task<SearchResult> Load(string query, CancellationToken cancellationToken);
```

Static methods use
`DotNet.invokeMethodAsyncWithCancellation(assemblyName, methodIdentifier, signal, ...args)`.
Neither the signal nor its controller is serialized as an application argument.
An already-aborted signal rejects without calling .NET. Otherwise, abort requests
cancellation of that invocation's .NET token. After acknowledgement, the JavaScript
promise rejects with `signal.reason`; dispatch or cancellation-callback failures
instead reject through the existing interop error path.

Cancellation is cooperative. A handler that ignores its token continues running,
and interop retains its active invocation until it actually finishes. This does
not cancel component or application lifetimes, dispose services, or undo completed
work. The framework removes the signal listener when the caller settles and releases
the cancellation source when the handler completes or the runtime is disposed.
The eventual result of an aborted call is not delivered to the caller. Ownership of
object-reference or stream results produced by a non-cooperating handler is not
resolved by this experiment and requires a separate design decision before shipping.

Ordinary invocation APIs are unchanged. In the reverse direction, the existing
`IJSRuntime.InvokeAsync` overload's `CancellationToken` cancels the managed wait;
it does not abort the JavaScript function or supply a browser `AbortSignal`.

The names and final-token convention are experimental and require API review.
Server and WebAssembly have browser scenarios in `InteropCancellationTest`.
WebView, Native AOT/trimming, multithreaded WebAssembly, multiple attached runtimes,
Auto activation transitions, and circuit reconnect/resumption require separate
validation. Static SSR and prerendering do not support JavaScript interop.

## Development Setup

### Build and test

To build the .NET code, you can:

 * Run `dotnet build` in the `src` directory. You can also read more [on building the project](../../../docs/BuildFromSource.md#step-3-build-the-repo).
 * Run `dotnet build` or `dotnet test` in the `test` directory. You can also read more about how to [run the tests on the command line](../../../docs/BuildFromSource.md#running-tests-on-command-line).

Alternatively, open `Components.slnf` in Visual Studio.

To build the JavaScript code, execute the following commands in a command shell:

 * `npm run build --workspace @microsoft/dotnet-js-interop`

## More Information

For more information, see the [ASP.NET Core README](../../../README.md).
