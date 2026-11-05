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

Cancellation is a parameter value, not an invocation mode. Explicit references map
browser `AbortSignal` values to .NET `CancellationToken` parameters and .NET tokens
to native browser signals. Ordinary invocation APIs and their completion/error
semantics are unchanged.

### JavaScript signal to .NET token

```javascript
const controller = new AbortController();
const reference = await dotNetObject.createCancellationTokenReference(controller.signal);
try {
    const result = await dotNetObject.invokeMethodAsync("Load", reference, query);
} finally {
    await reference.disposeAsync();
}
```

```csharp
[JSInvokable]
public Task<SearchResult> Load(CancellationToken cancellationToken, string query);
```

The instance factory targets that object's runtime. For static interop with one
unambiguous default runtime, `DotNet.createCancellationTokenReference(signal)` is
also available. The token occupies an ordinary argument position; it need not be last. The
reference can be retained and reused after an invocation returns. Abort signals
cancel the projected token, not the interop promise: an ignoring handler remains
pending or returns normally, and a handler that catches cancellation can return a
normal result. An already-aborted signal still invokes the method with an already
canceled token. Browser abort reasons are not translated into .NET exceptions.

The factory wraps the browser subscription in a `JSObjectReference` and the managed
source in a `DotNetObjectReference`. Disposing the returned reference removes the
listener and releases both references without requesting cancellation. The owner
must keep it alive as long as .NET needs to observe the signal. Asynchronous abort
notification failures are reported to the browser console and through disposal.

### .NET token to JavaScript signal

```csharp
await using var reference =
    await DotNetCancellationTokenReference.CreateAsync(jsRuntime, cancellationToken);
await jsRuntime.InvokeVoidAsync("receiveSignal", reference);
```

```javascript
function receiveSignal(signal) {
    return fetch("/data", { signal });
}
```

JavaScript receives a native `AbortSignal`, not a JSON token or a promise wrapper.
The reference owns a managed token observer through `DotNetObjectReference` and the
native signal through `JSObjectReference`. A pre-canceled token produces an aborted
signal before the JavaScript function starts. A reference can span multiple calls;
dispose it only when JavaScript no longer needs the signal. Disposal ends observation
without aborting the signal or canceling the original token. Raw outgoing tokens
require this explicit lifetime owner instead of implicit serialization.

The `CancellationToken` controlling `IJSRuntime.InvokeAsync` is separate from these
arguments: it still cancels only the managed wait and does not become an AbortSignal
or stop the JavaScript function. No call-ID cancellation registry, argument envelope,
reserved cancel invocation, automatic promise rejection, or call-completion cleanup
is involved in these projections.

The public reference APIs are experimental and require API review.
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
