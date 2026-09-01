# Edit limits: supported vs rude edits

Cache invalidation only matters for edits the runtime can apply at all. Some edits are "rude edits" that force a restart no matter how correct your handler is. Design and test against the real capability set, and know that a rude edit under `dotnet watch` (with restart-on-rude-edit) restarts the app — which can make a stale cache look fixed, so validation must distinguish the two (see validation.md).

## Table of contents
- [Supported without restart](#supported-without-restart)
- [Rude edits (restart required)](#rude-edits-restart-required)
- [Added fields are not re-initialized](#added-fields-are-not-re-initialized)
- [CoreCLR vs Blazor WebAssembly](#coreclr-vs-blazor-webassembly)

## Supported without restart

Treat these as supported without restart when the target runtime reports the corresponding capabilities (`Baseline AddMethodToExistingType AddStaticFieldToExistingType AddInstanceFieldToExistingType NewTypeDefinition ChangeCustomAttributes UpdateParameters GenericUpdateMethod GenericAddMethodToExistingType GenericAddFieldToExistingType`):

- Edit an existing method body — `void F() { A(); }` -> `void F() { A(); B(); }`.
- Edit a lambda or local-function body (its signature and captured-variable set must not change).
- Edit Razor markup, an event-handler body, or a `@code` method body — these compile through the Razor source generator to method-body deltas.
- Add a method, a static field, an instance field, or a whole new type.
- Add a new Blazor component / `@page` route (a new type) or a new controller action (a method on an existing type).
- Add or modify custom attributes (not pseudo-custom attributes like `DllImport`/`StructLayout`).
- Add a property (compiles to accessor methods plus, for auto-props, a field).
- Modify generic code (methods and types) on .NET 8+ runtimes.

## Rude edits (restart required)

These are rejected as hot-reload deltas; the tooling restarts (or, in an IDE, prompts). Treat each limit against the target runtime and toolchain:

- Rename a field — rude.
- Delete a field or a type — rude.
- Change a field's type — rude.
- Change a method signature or return type — rude on runtimes without the parameter-update capability; supported on CoreCLR .NET 8+ (and WASM .NET 8+ as a capability, with exceptions below).
- Change a base type or implemented interface — rude.
- Edit around an active statement (e.g. wrap a currently-executing line in try/catch) — rude.
- Change a `const` value — rude; consts are inlined at every use site at compile time, so there is no runtime field to update.
- Edit startup: service registration, middleware, and route configuration run once at startup and do not re-run on hot reload; the source updates but the behavior does not, without a restart. An inline middleware delegate body is the exception (a normal body edit).

## Added fields are not re-initialized

Adding an instance or static field is supported, but the runtime does not resize or re-run initializers on objects that already exist. On CoreCLR an added instance field is tracked in a lazy side structure and reads as `default(T)` on pre-existing instances; the field initializer (`= 42`) runs only for instances created after the edit, via the updated constructor. Added static fields likewise start at `default(T)` if the static constructor already ran. Practical consequence: after adding `int _n = 42;`, an already-rendered object shows `0` for `_n`, while a freshly-created one shows `42`. Do not rely on an added field's initializer affecting live instances.

## CoreCLR vs Blazor WebAssembly

Blazor Server runs on CoreCLR; Blazor WebAssembly runs on Mono, whose capability set is narrower and set per target framework:

- WASM before .NET 8: no `AddInstanceFieldToExistingType`, no generic edits, no parameter updates — all rude. WASM .NET 8+ gains instance fields, generic method edits, and parameter updates.
- Even on WASM .NET 8+, adding a new `await` or `yield` (which changes a state machine's shape) is unsupported, and changing a method parameter's name is rude.
- WASM lacks some CoreCLR-only capabilities entirely (e.g. adding an explicit interface implementation).
- Delivery differs: for WASM, the patch is pushed over the browser WebSocket, so an active browser connection is required — a closed/disconnected tab means no delta is delivered. For Blazor Server and other server apps, the patch goes to the server process over a named pipe regardless of the browser; the browser connection is used only for the browser refresh.

When a feature or edit targets both, validate on both runtimes: an edit that hot-reloads on Server may be a rude edit on WebAssembly.
