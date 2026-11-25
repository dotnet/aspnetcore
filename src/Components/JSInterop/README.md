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

## Experimental cancellation arguments

A JavaScript `AbortSignal` passed to .NET becomes a `CancellationToken`, and a .NET `CancellationToken` passed to JavaScript becomes a native `AbortSignal`. Application code does not create, wrap, or dispose any interop object for this. The behavior is experimental and needs API review.

### JavaScript signal to .NET token

```javascript
const controller = new AbortController();
const text = await dotnetObject.invokeMethodAsync("DownloadAsync", url, controller.signal);

// Static methods work the same way.
const size = await DotNet.invokeMethodAsync("MyApp", "MeasureAsync", controller.signal);

// A JavaScript function can also return a signal to .NET.
export function createTimeoutSignal(milliseconds) {
    return AbortSignal.timeout(milliseconds);
}
```

```csharp
[JSInvokable]
public Task<string> DownloadAsync(string url, CancellationToken cancellationToken) { ... }

[JSInvokable]
public static Task<int> MeasureAsync(CancellationToken cancellationToken) { ... }

var token = await module.InvokeAsync<CancellationToken>("createTimeoutSignal", 5000);
```

**What the application sees**

- A signal can be any argument of `invokeMethodAsync` or `invokeMethod` (synchronous calls work in WebAssembly), can be part of a serialized object, and can be part of a value that a JavaScript function returns to .NET.
- Aborting the signal cancels the token. It does not reject the promise of the call. A method that ignores the token runs to the end, and a method that handles the cancellation can return a normal result.
- A signal that is already aborted still calls the method, with a canceled token.
- One signal gives one token in a runtime, also when it is passed again later or to several calls at once. .NET code can keep the token after the call returns.
- The abort reason is not passed to .NET.
- A signal wrapped with `DotNet.createJSObjectReference(signal)` stays a plain JavaScript object reference.

**How it works**

JavaScript gives each signal an id and writes it as `{"__abortSignal": <id>, "aborted": <bool>}`, so the call is sent at once, with no extra round trip. .NET creates the token's source for the id when it reads the message. On abort, JavaScript sends .NET an "aborted" message for the id and waits for its acknowledgment. Interactive Server can reorder inbound messages, so the "aborted" message can arrive before the call that carries the id. .NET then creates the token already canceled.

When the browser collects the signal, JavaScript sends a one-way "released" message, and .NET forgets the id without canceling the token. JavaScript counts the messages that carry each id and sends "released" only after .NET has answered every one of them and acknowledged the abort, so "released" always reaches .NET last. A call is answered by its reply. A value returned to .NET is answered by a one-way message that .NET sends after reading it. Errors from .NET cancellation callbacks during the abort are reported to the browser console.

### .NET token to JavaScript signal

```csharp
using var cts = new CancellationTokenSource();
var text = await module.InvokeAsync<string>("download", args: [url, cts.Token]);
```

```javascript
export async function download(url, signal) {
    const response = await fetch(url, { signal });
    return await response.text();
}
```

**What the application sees**

- JavaScript receives a native `AbortSignal`. Canceling the token aborts it, also after the call has returned, and also when JavaScript code did not keep the signal (for example after giving it to `fetch`).
- A token can be any argument, can be part of a serialized object, and can be part of a value that a `[JSInvokable]` method returns to JavaScript (also to synchronous `invokeMethod` in WebAssembly).
- One token gives the same `AbortSignal` until it is canceled or its source is reset with `CancellationTokenSource.TryReset()`. A canceled token gives a new aborted signal each time, and `CancellationToken.None` gives a signal that never aborts.
- After `TryReset()`, a later cancel aborts only the calls made after the reset, as .NET notifies only the registrations made after it.
- In WebAssembly, abort listeners run after `CancellationTokenSource.Cancel()` returns.
- The token in `args` is not the `CancellationToken` parameter of `InvokeAsync(string, CancellationToken, object[])`. That parameter still cancels only the .NET wait. It does not become a signal and does not stop the JavaScript function.

**How it works**

.NET gives each token one id from a counter shared by the whole process and writes it as `{"__dotNetCancellationToken": <id or null>, "isCancellationRequested": <bool>}`. JavaScript keeps the `AbortController` for the id until .NET releases the id, because a browser operation such as `fetch` does not keep its signal alive. .NET registers on the token after the message that carries the id is handed to the host. On cancel, .NET sends a one-way "canceled" message for the id. A WebView host or a synchronous result in WebAssembly can deliver it before the message that carries the id, so JavaScript then creates the controller already aborted. JavaScript keeps an aborted controller until "released", so a message that carries the id after the cancel still gives the aborted signal.

Only the token's registration keeps the .NET side alive. Once the source is canceled, reset, disposed, or collected, the next message that .NET sends to JavaScript after a garbage collection also queues a one-way "released" message, and JavaScript forgets the id without aborting. A message keeps its ids alive until it is handed to the host, so "released" is always handed over after every message that carried the id. It is sent from the thread pool, because the WPF and WinForms WebView hosts send at once on the UI thread but queue messages from other threads in order. Failures to send "canceled" and "released" are ignored. Disposing the `JSRuntime` unregisters the remaining tokens.

### Limits

- On Interactive Server, a message sent while the circuit is disconnected is lost and is not sent again after the reconnect. An abort or a cancel can then be lost, and ids can stay until the circuit ends.
- A canceled id stays on both sides until .NET sends its next message to JavaScript after a garbage collection.
- Both directions have Server and WebAssembly scenarios in `InteropCancellationTest`. The WebView ordering rests on the hosts' dispatchers and has no automated test. Native AOT and trimming, multithreaded WebAssembly, Auto render mode transitions, and circuit pause and resume are not validated yet. Static SSR and prerendering do not support JavaScript interop.

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
