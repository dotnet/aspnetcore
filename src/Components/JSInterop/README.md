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

## Experimental cancellation parameter projections

Cancellation is a parameter value, not an invocation mode. The runtimes automatically
project raw browser `AbortSignal` arguments to .NET `CancellationToken` parameters
and raw .NET token arguments to native browser signals. Application code does not
create, wrap, or dispose interop cancellation references.

### JavaScript signal to .NET token

```javascript
const controller = new AbortController();
const result = await dotnetObject.invokeMethodAsync("Yolo", controller.signal);

// Static methods need no application DotNetObjectReference.
const staticResult = await DotNet.invokeMethodAsync("MyApp", "YoloStatic", controller.signal);
```

```csharp
[JSInvokable]
public Task<string> Yolo(CancellationToken cancellationToken);

[JSInvokable]
public static Task<string> YoloStatic(CancellationToken cancellationToken);
```

The instance call selects that object's runtime. Static calls retain their existing
default-dispatcher requirements. Tokens occupy ordinary argument positions and can
also occur in serialized objects. The token can be retained and the signal reused
after an invocation returns. Abort signals cancel the token, not the interop promise: an ignoring handler remains
pending or returns normally, and a handler that catches cancellation can return a
normal result. An already-aborted signal still invokes the method with an already
canceled token. Browser abort reasons are not translated into .NET exceptions.

The async invocation pipeline establishes any needed reference bridge before
dispatching the application call. Repeated and concurrent uses of one signal share
its projection in that runtime. Application getters and `toJSON` run once; byte-array
snapshots are transmitted only after asynchronous preparation, immediately before
the application invocation. Explicit generic `createJSObjectReference(signal)` values
remain generic references, rather than implicitly changing their target type.

### .NET token to JavaScript signal

```csharp
using var cts = new CancellationTokenSource();
var pending = jsRuntime.InvokeAsync<string>("yolo", args: [cts.Token]);
cts.Cancel();
var result = await pending;
```

```javascript
globalThis.yolo = async function (signal) {
    const response = await fetch("/data", { signal });
    return await response.text();
}
```

JavaScript receives a native `AbortSignal`, not a JSON token or a promise wrapper.
A pre-canceled token produces an aborted signal before the JavaScript function
starts. A signal can be retained after the call returns. `CancellationToken.None`
and already-canceled tokens need no live bridge. Each live serialized token occurrence
has its own observer, so an earlier signal's cleanup cannot revoke a later in-flight
projection of the same token.

The runtimes own cleanup. Incoming links are released after abort notification and
outstanding argument uses; outgoing observers are released after notification.
`WeakRef` and `FinalizationRegistry` also release links when the browser signal is
collected without cancellation. Collection is nondeterministic and does not request
cancellation. Live signals can retain their registrations and interop references.
Managed runtime disposal clears remaining automatic references. Browser cleanup after
transport loss or runtime teardown needs separate host/lifecycle validation.

The `CancellationToken` controlling `IJSRuntime.InvokeAsync` is separate from these
arguments: `args: [cts.Token]` is intentional. The separate invocation-control token
still cancels only the managed wait and does not become an AbortSignal or stop the
JavaScript function. There is no operation-cancellation mode, forced promise rejection,
or application-owned reference lifetime. Async bridge notification/cleanup failures
are reported to the browser console; setup failures reject the invocation normally.

The marshalling convention and framework callback surface are experimental and require API review.
Both directions have Server and WebAssembly browser scenarios in `InteropCancellationTest`.
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
