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

## Development Setup

### Build and test

To build the .NET code, you can:

 * Run `dotnet build` in the `src` directory. You can also read more [on building the project](../../../docs/BuildFromSource.md#step-3-build-the-repo).
 * Run `dotnet build` or `dotnet test` in the `test` directory. You can also read more about how to [run the tests on the command line](../../../docs/BuildFromSource.md#running-tests-on-command-line).

Alternatively, open `Components.slnf` in Visual Studio.

To build the JavaScript code, execute the following commands in a command shell:

 * `npm run build --workspace @microsoft/dotnet-js-interop`

## WIP: generated-only Native AOT experiment

`GeneratedInteropPrototype` is an experimental registration surface, not a finished source
generator or an approved API. Reflection-based dispatch remains the default. To select the
prototype exclusively, add this runtime feature switch to the consuming application's project:

```xml
<ItemGroup>
  <RuntimeHostConfigurationOption Include="Microsoft.JSInterop.GeneratedOnly" Value="true" Trim="true" />
</ItemGroup>
```

Before creating any `JSRuntime`, set `GeneratedInteropPrototype.ConfigureRuntime` to install
application JSON metadata and register handwritten, strongly typed delegates. For example:

```csharp
GeneratedInteropPrototype.ConfigureRuntime = (runtime, options) =>
{
    options.TypeInfoResolverChain.Add(AppJsonContext.Default);
    runtime.GeneratedInterop.RegisterStatic("MyApp", "Double", 1, (r, _, args) =>
    {
        var value = args[0].Deserialize(GeneratedInteropPrototype.Metadata<int>(r));
        return ValueTask.FromResult<string?>(JsonSerializer.Serialize(
            value * 2, GeneratedInteropPrototype.Metadata<int>(r)));
    });
};
```

`AppJsonContext` is the application's source-generated `JsonSerializerContext`. Register every
application payload type, including value types. Use the runtime-bound `Metadata<T>` for both
arguments and results so byte arrays and object/stream references retain their runtime converters.
For each application `DotNetObjectReference<T>`, call `RegisterReferenceConverter<T>` during
configuration to root a closed converter without runtime generic construction.

Configure registrations once before dispatch begins. The process-wide mutable startup hook and
the registry shape are scaffolding for this experiment, not a proposed production initialization
model. Register asynchronous delegates with `isAsync: true`, deserialize all arguments before
their first await, and serialize results after awaiting `Task<T>` or `ValueTask<T>`. These delegate
contracts still require generator enforcement. Exact runtime types are used for instance lookup.
Unregistered methods and missing JSON metadata fail explicitly; there is no reflection fallback
when the switch is enabled.

**Known blocker:** generated-only mode does not register framework-owned callbacks such as
`WebRendererInteropMethods.DispatchEventAsync`. Blazor button clicks therefore fail in this mode.
Framework registrations must be supplied by the owning assembly, not by application code reaching
into private framework types. Component constructor preservation and remaining non-JSInterop
trimming/AOT warnings are also unresolved. Existing instance-method rooting annotations remain.
This experiment does not establish support for complete Native AOT Blazor applications.

The WebView IPC envelope uses a separate source-generated context for its fixed transport types;
application payloads have already passed through the runtime's converters before this layer.

## More Information

For more information, see the [ASP.NET Core README](../../../README.md).
