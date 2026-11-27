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

`GeneratedInteropPrototype` and the `Microsoft.JSInterop.SourceGenerators` generator are an
experimental registration surface, not an approved API. Reflection-based dispatch remains the
default. To select generated dispatch exclusively, add this runtime feature switch to the consuming
application's project:

```xml
<ItemGroup>
  <RuntimeHostConfigurationOption Include="Microsoft.JSInterop.GeneratedOnly" Value="true" Trim="true" />
</ItemGroup>
```

### Generated registrations

The generator (`src/Components/JSInterop/gen`) finds every `[JSInvokable]` method in the compilation
and emits a module initializer that registers a strongly typed invoker for each identifier in
`GeneratedInteropPrototype.Shared`. Each invoker reads all arguments with the runtime-bound JSON
metadata before calling the method, rejects synchronous calls to asynchronous methods, and serializes
results the way reflection dispatch does. The module initializer returns immediately unless the
feature switch is enabled, so trimming removes the registrations when the switch is off.

Microsoft.AspNetCore.Components.Web, Microsoft.AspNetCore.Components.WebAssembly and
Microsoft.AspNetCore.Components.Platform run the generator, so framework callbacks such as
`WebRendererInteropMethods.DispatchEventAsync`, WebAssembly navigation callbacks and Platform's
`JSCallback<T>` are registered by the assemblies that own them. The generator is not packaged yet; an
application opts in with an analyzer reference to the generator project.

Instance methods on generic types (for example `Box<T>`) and on private or protected nested types are
registered lazily from the target instance through a generated implementation of
`GeneratedInteropPrototype.IInstanceRegistrationProvider`. The type and all its containing types
must be declared `partial`. No static constructor is added, existing static constructors are
untouched, and the generator no longer changes `beforefieldinit`.

The generator applies an assembly-local, non-generic attribute to each partial host. Its static
`Register<THost>` method owns the identifiers, argument counts and registration logic. The host
supplies `THost` and its strongly typed invokers through the interface bridge. This is an
attribute/interface hybrid: the runtime discovers the bridge using an interface test, not by
reading or instantiating attributes. The attribute application is descriptive metadata, not the
activation mechanism.

An attribute cannot use a host's type parameters, even when nested inside the generic host.
Passing closed invokers from inside the host avoids runtime `MakeGenericType`, reflective method
invocation and inaccessible-type references in the attribute. Generated non-sealed hosts also
expose a protected static `__JSInvokable_Register` bridge so generated derived hosts can compose
base registrations, including across assemblies. These bridges and the public provider interface
are experimental API surface, not an approved contract.

Each runtime's registry caches an isolated registration set per runtime target type, initialized
once even under concurrent calls. Registration failures surface on invocation as their original
exceptions and are cached, rather than becoming type-initialization failures. Explicit registrations
still take precedence at each type in the inheritance walk. Provider callbacks must use their target
argument instead of capturing the instance that initialized the type's registrations.

When an override does not redeclare `[JSInvokable]`, the generator registers a non-invokable marker
for the overriding type so that, like reflection, calls on that type fail instead of dispatching to
the override. Derived types compiled without the generator do not get these markers.

Unsupported methods are skipped with these warnings:

| ID | Meaning |
| --- | --- |
| `JSI0001` | Method is not public. |
| `JSI0002` | Method cannot be invoked from JavaScript by reflection or generated dispatch, such as a generic method or a static method on a generic type. |
| `JSI0003` | Method is declared on a file-local type. |
| `JSI0004` | Static method is declared on a type that is not public. |
| `JSI0005` | Signature or target is unsupported, such as `ref` parameters, abstract or interface methods, or instance methods on value types. |
| `JSI0006` | Identifier is duplicated. |
| `JSI0007` | Generic or inaccessible type, or one of its containing types, is not `partial`. |

`JSI0008` (static-constructor conflict) is retired by the lazy-host prototype; types with existing
static constructors are now supported.

Application payload types still need JSON metadata. Before creating any `JSRuntime`, set
`GeneratedInteropPrototype.ConfigureRuntime` to add the application's source-generated
`JsonSerializerContext`:

```csharp
GeneratedInteropPrototype.ConfigureRuntime = (runtime, options) =>
    options.TypeInfoResolverChain.Add(AppJsonContext.Default);
```

Libraries contribute metadata for their own payloads with
`GeneratedInteropPrototype.AddTypeInfoResolver`, which generated-only runtimes consult after the
built-in metadata. Components.Web uses it for `InputFile`'s `BrowserFile[]` payload. Add resolvers
before a runtime first serializes the affected types.

Handwritten registrations through `RegisterStatic` and `RegisterInstance<T>` on
`JSRuntime.GeneratedInterop` (per runtime) or `GeneratedInteropPrototype.Shared` remain available;
per-runtime registrations take precedence. Instance lookup walks the target's base types.
Unregistered methods and missing JSON metadata fail explicitly; there is no reflection fallback when
the switch is enabled.

### Known gaps

* The process-wide mutable startup hook and the registry shape are scaffolding, not a proposed
  production initialization model. The API proposal is still to be written.
* Module initializers run when their module loads. NativeAOT runs them at startup; on CoreCLR an
  assembly's registrations are not available until that assembly has been loaded.
* Generic methods and static methods on generic types are not invokable by reflection dispatch
  either, and are not generated. File-local types are not supported.
* Generic hosts such as `JSCallback<T>` read their arguments with the runtime's JSON metadata, so
  applications must provide metadata for each `T` they use.
* Existing instance-method rooting annotations remain, and four IL2072 suppressions in Components
  still need a real fix. This experiment does not establish support for complete Native AOT Blazor
  applications.

The WebView IPC envelope uses a separate source-generated context for its fixed transport types;
application payloads have already passed through the runtime's converters before this layer.

## More Information

For more information, see the [ASP.NET Core README](../../../README.md).
