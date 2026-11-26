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

## Timeout behavior and compatibility

This change proposes a **behavioral breaking change** to timeouts owned by `JSRuntime` or the timeout extension methods. Public signatures and timeout durations are unchanged, but exception handling and task completion behavior change:

| Timeout source | Previous behavior | Proposed behavior |
| --- | --- | --- |
| `JSRuntime.DefaultAsyncTimeout`, when configured and no cancellation token is supplied | Canceled task; awaiting throws `TaskCanceledException` | Faulted task; awaiting throws a descriptive `JSException` with the original cancellation exception as its `InnerException` |
| An explicit `TimeSpan` timeout | Canceled task; awaiting throws `TaskCanceledException` if the timer remains active | Faulted task; awaiting throws a descriptive `JSException` with the original cancellation exception as its `InnerException` |
| A caller-supplied `CancellationToken`, including one scheduled with `CancelAfter` | Cancellation | Unchanged: cancellation, not a timeout `JSException` |

The explicit-timeout change covers all ten `TimeSpan` overloads: `InvokeAsync<TValue>`, `InvokeVoidAsync`, `InvokeConstructorAsync`, `GetValueAsync<TValue>`, and `SetValueAsync<TValue>` on both `IJSRuntime` and `IJSObjectReference`. Applying the same classification to both default and explicit timeouts is intentional; it is broader than changing only the default timeout. `Timeout.InfiniteTimeSpan` does not expire. Supplying a cancellation token overrides the runtime's default timeout, even when the caller uses that token to implement a timeout.

There is also a timer-lifetime correction: the constructor, property-get, and property-set `TimeSpan` overloads now retain their timeout sources until the asynchronous operation completes. Previously, those sources were disposed when the pending operation was returned, so their timeouts might never fire. Calls that previously remained pending can therefore now time out. This is a separate observable change from the exception type.

### Application migration

- Code that catches `OperationCanceledException` or `TaskCanceledException` for default or `TimeSpan` timeouts must now handle `JSException`. A timeout fault retains an `OperationCanceledException` as its inner exception, allowing it to be distinguished from a JavaScript error without inspecting message text.
- Code that inspects task status must account for timeout tasks being faulted rather than canceled.
- `ComponentBase` ignores ordinary cancellation, but does not ignore timeout faults. An unhandled timeout from a component lifecycle method or event handler now reaches the renderer's error handling and can terminate an Interactive Server circuit. Handle recoverable timeouts in the component or with an error boundary.
- Caller-token cancellation remains unchanged and can still be ignored by `ComponentBase`. This change does not alter `ComponentBase` or treat every `OperationCanceledException` as an error. Applications that need to retain cancellation behavior can use the cancellation-token overloads, with a caller-owned timeout source if needed.
- Browser-platform acquisition must preserve timeout faults instead of reporting that the browser API is unavailable. Ordinary missing-property JavaScript errors continue to be translated to `NotSupportedException` by the Cookie Store and Screen Orientation wrappers.

### Compatibility review status

Maintainer acceptance of **this patch's full scope**, including the ten explicit-timeout overloads and timer-lifetime correction, is still required. The discussion and approval on [dotnet/aspnetcore#67013](https://github.com/dotnet/aspnetcore/pull/67013) acknowledge a related behavioral break but do not approve this implementation. This document records the proposed changes and migration path; it is not a compatibility approval. Release documentation must reflect the accepted scope before this behavior ships.

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
