---
name: port-components-platform-api
description: >-
  Port one standard non-DOM browser API family, such as Clipboard, Geolocation, or Permissions, to
  Microsoft.AspNetCore.Components.Platform as a typed projection reached from
  IBrowserPlatform.Window, the way Web Storage is exposed as Window.LocalStorage. USE FOR adding,
  extending, reviewing, or testing a browser API family in src/Components/Platform; designing its
  public API and API proposal; giving Blazor typed access to a standard browser API instead of
  string-based IJSRuntime calls; and the disposal, cancellation, trimming, and Helix E2E rules of
  that package. DO NOT USE FOR DOM manipulation, application-specific JavaScript, or migrating
  existing framework code such as ProtectedBrowserStorage to the package.
---

# Port a Components Platform API

Create one reviewable vertical slice, a typed projection of one browser API family with its tests.
Web Storage (`Storage.cs`, `WindowWebStorageExtensions.cs`, and `test/StorageTest.cs`) is the
reference implementation. Read it before writing code, and copy its shape unless a rule below says
otherwise.

## Scope

- Port one API family per change, so each family stays small and separately reviewable.
- Use Web IDL, browser compatibility data, and MDN to define the family and its explicit exclusions.
- Keep DOM manipulation and application-specific JavaScript out of this package.
- New public API needs an API proposal issue. Use the `api-review` skill, and keep the issue
  description in sync with `PublicAPI.Unshipped.txt` when the shape changes during review.

## Implement

- Put the projection in `src/Components/Platform/src`. Add one type for the family, such as
  `Clipboard.cs`, and one extension class, such as `WindowClipboardExtensions.cs`.
- Let the IL trimmer drop a family the application never touches. Attach each family to `Window` as
  extension members in an `extension(Window window)` block, not as instance properties that always
  hold it alive. Get the instance with `window.GetOrAddFeature(...)`, and pass a subkey when two
  properties share one type, as `LocalStorage` and `SessionStorage` do.
- Follow Web IDL names and behavior. Do not add convenience APIs, polyfills, or .NET substitutions.
  Map an operation `foo()` to `FooAsync()` and a readable attribute `foo` to `GetFooAsync()`. Map
  `unsigned long` to `uint` and a nullable `DOMString` to `string?`.
- Keep this one-to-one shape even when a more .NET-like API looks simpler. Review chose it on
  purpose. A developer who knows the Web Platform API should find the same API from `Window.`
  without learning Blazor-specific services. It also keeps unused families trimmable.
- Keep live browser access asynchronous, and return `ValueTask` or `ValueTask<T>` like the JS
  interop APIs do. Do not switch to `Task`. `ValueTask` leaves room for a synchronous
  `IJSInProcessRuntime` path on WebAssembly, and the return type cannot change after the API ships.
  Keep raw JS interop types and identifiers non-public. `PublicApiTest` enforces both.
- Do not add locks or other synchronization. Blazor runs on a single threaded synchronization
  context, so only one thread runs at a time.
- Use direct constructor, property, and method interop before adding a JavaScript module.
- Acquire a JS object reference lazily on first use and share the pending task between callers. Do
  not keep a failed or canceled acquisition. A timeout during a brief connection loss would
  otherwise break the family for the rest of the circuit, so acquire again on the next call.
- Wrap identity-bearing objects and preserve identity. `Window` owns the wrapper and hands the same
  one to every caller, so never implement `IAsyncDisposable` or a public `DisposeAsync`. Implement
  the internal `IInternalAsyncDisposal` explicitly, and let `Window` call it when the scope ends.
- Never let disposal throw. `Window` disposes features in access order, and an exception stops the
  rest. Do not wait for a pending acquisition either. It could hold up scope disposal until the JS
  interop timeout, which is one minute by default. Release the reference only when the acquisition
  has already completed successfully. After disposal, throw `ObjectDisposedException` from every
  member.
- Add cancellation only when it maps to `AbortSignal`, and do not add it to Web Storage style APIs.
  A `CancellationToken` overload can be added later without a breaking change, so leave it out
  when in doubt.
  `IJSRuntime` never forwards a token to JavaScript. Canceling only stops .NET from waiting and
  skips deserialization, and the result is still transferred. Real cancellation needs a JS-side
  `AbortController` that .NET aborts through interop. Also, any token turns off the default timeout
  that `JSRuntime` applies, so call the overload without a token when the caller passes none.

## Document

Every public member needs complete XML docs. The PR review bot flagged `Storage` when they were
missing. Describe consumer-observable behavior only. Do not describe interop, caching, or disposal
internals. Follow the majority phrasing in this repository and `ProtectedBrowserStorage`.

- Write a `<summary>` for every public member. Add a `<param>` for each parameter, a `<returns>`
  for every non-void method (including a bare `ValueTask`), and an `<exception>` for each argument
  check the member throws.
- Write a `<param>` as a noun phrase only, such as `The key of the value to set.` Put behavior,
  such as "replaces any existing value" or "if it exists", in the `<summary>`.
- For `ValueTask<T>`, describe the value itself, such as `The number of stored items.`
- For a bare `ValueTask`, write
  `A <see cref="ValueTask"/> representing the completion of the operation.`
- For a nullable result, write `..., or <see langword="null"/> if ...`. Use "if", not "when".
- For a null check, write
  `<exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>`.
- Do not document `JSException`, for example for an exceeded quota. Interop types stay out of the
  public contract.
- When unsure, search the repository for the phrasing and use the most common form.

## Adopt

Do not migrate existing framework code to the projection in the same change. The package ships on
its own, and an assembly in the shared framework, such as `Microsoft.AspNetCore.Components.Server`,
cannot reference it. Reviewers also decided that a wrapper around a few existing `IJSRuntime` calls
gives the framework no benefit. Adoption, if ever needed, is a separate decision.

Applications adopt the projection by calling `AddBrowserPlatform()`, injecting `IBrowserPlatform`, and
importing `Microsoft.AspNetCore.Components.Platform` for the extension members. Show exactly this in
the API proposal's usage example.

## Verify

- Add unit tests to `src/Components/Platform/test`, one file per family. Cover interop identifiers
  and arguments, lazy acquisition, a single shared acquisition, acquiring again after a failure,
  stable and distinct instances, disposal of the acquired reference, `ObjectDisposedException` after
  disposal, and disposal that finishes without waiting for a pending acquisition.
- Keep tests few and readable. Avoid tricks that depend on ordering. When ordering cannot be
  avoided, make it visible in the test and explain it in one comment.
- Add the new type to both `LiveBrowserObjectMethods_AreAsynchronous` and
  `SharedInstances_CannotBeDisposedByApplicationCode` in `PublicApiTest`.
- Exercise the public API in Interactive Server and WebAssembly through `PlatformTestApp` and
  `PlatformTestApp.E2E.Tests`. These E2E tests run on Helix in CI with a prebuilt app. A local run
  from the build output does not use that layout, so also run the tests from the output of
  `dotnet publish` on the E2E project.
- Publish a trimmed WebAssembly application and confirm the unused families are gone from the output.
- Update `PublicAPI.Unshipped.txt`, build, pack, and inspect the final diff. Use explicit
  `ProjectReference` and `PackageReference` items in project files, as
  `docs/ReferenceResolution.md` describes.
