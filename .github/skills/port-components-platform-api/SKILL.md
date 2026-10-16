---
name: port-components-platform-api
description: >-
  Port one standard non-DOM browser API family, such as Cookie Store, Screen, or Clipboard, to
  Microsoft.AspNetCore.Components.Platform as a typed .NET projection reached from
  IBrowserPlatform.Window, the way Web Storage is exposed as Window.LocalStorage. USE FOR wrapping,
  porting, or projecting a browser API (window.* or navigator.*) for Blazor, such as "wrap the
  Cookie Store API as a .NET API" or "add navigator.clipboard to Components.Platform"; designing the
  public API and API proposal of a family; mapping Web IDL types to .NET; extending or testing a
  family in src/Components/Platform; and the disposal, cancellation, trimming, and E2E rules of
  that package. DO NOT USE FOR DOM manipulation, application-specific JavaScript, migrating
  existing framework code such as ProtectedBrowserStorage, or filing the API review issue itself
  (use api-review).
---

# Port a Components Platform API

Create one reviewable vertical slice, a typed projection of one browser API family with its tests.
Web Storage (`Storage.cs`, `WindowWebStorageExtensions.cs`, and `test/StorageTest.cs`) is the
reference implementation. Read it before writing code, and copy its shape unless a rule below says
otherwise.

## Scope

- Port one API family per change, so each family stays small and separately reviewable.
- Keep DOM manipulation and application-specific JavaScript out of this package.
- Leave out events and other callbacks from JavaScript to .NET for now, such as the `onchange`
  event handler of `CookieStore`. Their design, including how to deliver them without a JavaScript
  module, is not decided yet. List them as deferred members, and raise them as an open question.
- Do not migrate existing framework code, such as `ProtectedBrowserStorage`, to the projection. The
  package ships on its own, and an assembly in the shared framework, such as
  `Microsoft.AspNetCore.Components.Server`, cannot reference it. Reviewers also found no benefit in
  wrapping a few existing `IJSRuntime` calls.

## Design first

Most of the time on a family goes to agreeing on its API shape, not to coding. So write a design
proposal before you write code, and get agreement from the person who asked for the port. The
examples below use the Cookie Store API.

- Read the Web IDL of the family from webref, at `ed/idl/<spec>.idl` in
  https://github.com/w3c/webref, not from memory. For example, `cookiestore.idl` shows that
  `CookieListItem` now has only `name` and `value`. Web IDL shows only signatures. Read the method
  steps in the specification for the behavior, such as which arguments the browser rejects, and
  use MDN as a guide.
- List every interface, dictionary, enum, typedef, and member that the family reaches from its
  entry point, such as `window.cookieStore`. Mark each one as implement, defer, or exclude, and give
  the reason. For example, the `get`, `getAll`, `set`, and `delete` operations are implemented,
  `onchange` is deferred as an event, and `CookieStoreManager` is excluded. It is exposed to
  `Window` too, but only through another entry point, `ServiceWorkerRegistration.cookies`, so it
  belongs to a separate slice. Check the `Exposed` attribute in the Web IDL before you give such a
  reason.
- Check the browser support of each member in the browser compatibility data on MDN. When a member
  is not Baseline, stop and raise it. Such a member would need `[Experimental]`, and the package
  has no diagnostic ID for it yet. A dictionary member that was added recently is a typical case.
- Write the planned public API as C# signatures, following the next section.
- Collect every open question that the rules in this skill tell you to raise into one list.
- Start the implementation only after the proposal is agreed. Then file the API proposal issue
  with the `api-review` skill. Its usage example calls `AddBrowserPlatform()`, injects
  `IBrowserPlatform`, and imports `Microsoft.AspNetCore.Components.Platform` for the extension
  members. Keep the issue in sync with `PublicAPI.Unshipped.txt` when the shape changes in review.

## Shape the public API

- Follow Web IDL names and behavior. Map an operation `foo()` to `FooAsync()` and a readable
  attribute `foo` to `GetFooAsync()`, except for the object properties described below. Do not add convenience APIs, polyfills, or .NET
  substitutions, even when a more .NET-like API looks simpler. Review chose this one-to-one shape
  on purpose. A developer who knows the Web Platform API should find the same API from `Window.`
  without learning Blazor-specific services.
- Map types by [references/type-mapping.md](references/type-mapping.md). It covers names, value
  types, dictionaries, overloads, enums, and trimming. A wrong mapping can send `null` where the
  browser expects a missing member, and the browser then stores the string `"null"`.
- For every .NET shape decision that Web IDL does not settle, follow the API conventions of the
  `review-public-api` skill. When a convention conflicts with the one-to-one rules of this skill, follow this skill and raise
  the conflict as an open question.
- Return `ValueTask` or `ValueTask<T>` from every live browser access, like the JS interop APIs do.
  Do not switch to `Task`. `ValueTask` leaves room for a synchronous `IJSInProcessRuntime` path on
  WebAssembly, and the return type cannot change after the API ships. Keep raw JS interop types
  and identifiers non-public. `PublicApiTest` enforces both.
- Put each family on the object that owns it in Web IDL, and reach that object from `Window`
  through synchronous properties. For example, `navigator.clipboard` becomes
  `Window.Navigator.Clipboard`, and its `writeText()` becomes `WriteTextAsync()`. Do not move a
  family to another owner, such as `Window.Clipboard`. A member of `WindowOrWorkerGlobalScope`,
  such as `indexedDB` or `caches`, belongs to the global object, which is `window` in a page, so
  put it on `Window`.
- Project a readable attribute as a synchronous property only when all of these are true. Its type
  is an interface. It returns the same object for the life of the page. Web IDL marks most such
  attributes `[SameObject]`, but not all, such as `localStorage`. Reading it has no side effect and
  needs no permission. The property then only moves between .NET objects and does not call the
  browser. Project any other readable attribute as `Get<X>Async()`, such as `navigator.language`
  as `GetLanguageAsync()`.
- Declare each synchronous property in an `extension` block of its owner, such as
  `extension(Window window)` in `WindowWebStorageExtensions`, not as an instance property. The
  trimmer can then drop a family the application never touches. When the owner type does not exist
  yet, such as `Navigator`, stop and raise it as an open question. Keep the property synchronous
  and non-nullable even when the browser object can be missing. "Implement" below says how to
  handle that.
- Leave value checks to the browser. For example, do not check in .NET that `CookieInit` sets both
  `expires` and `maxAge`. The browser already throws `TypeError` for that, and a copy in .NET can
  drift when the browser changes. Check only a `null` argument for a non-nullable parameter, and
  throw `ArgumentNullException`, as `Storage` does. Web IDL would turn that `null` into the string
  `"null"`. Check a dictionary argument itself but not its members, as `ResponseCookies.Append`
  does with `CookieOptions`. A caller who writes `Name = null!` takes responsibility for it.
- Add a `CancellationToken` only where Web IDL has an `AbortSignal`, and not to Web Storage style
  APIs. An overload with a token can be added later without a breaking change, so leave it out when
  in doubt. `IJSRuntime` never forwards a token to JavaScript. Canceling only stops .NET from
  waiting and skips deserialization, and the result is still transferred. Real cancellation needs a
  JS-side `AbortController` that .NET aborts through interop.

## Implement

- Put the projection in `src/Components/Platform/src`. Add one type for the family, such as
  `Clipboard.cs`, and one extension class on its owner, such as `NavigatorClipboardExtensions.cs`.
  Get the instance with `window.GetOrAddFeature(...)`, and pass a subkey when two properties share one type, as
  `LocalStorage` and `SessionStorage` do.
- Let the trimmer drop each unused member of a used family too. Do not reach family members through
  reflection or tables that reference every member. Use `[DynamicDependency]` only on a method that
  passes or receives a dictionary through JS interop, as the type mapping describes. Keep the build free of trim
  warnings.
- Do not add a JavaScript module. Use only the `IJSRuntime` and `IJSObjectReference` features of
  .NET 10 and later, such as `InvokeConstructorAsync`, `GetValueAsync`, and `SetValueAsync`. If a
  feature seems to need JavaScript of its own, stop and raise it as an open question.
- Do not add locks or other synchronization. Blazor runs on a single threaded synchronization
  context, so only one thread runs at a time.
- Acquire a JS object reference lazily on first use and share the pending task between callers. Do
  not keep a failed or canceled acquisition. A timeout during a brief connection loss would
  otherwise break the family for the rest of the circuit, so acquire again on the next call.
- Handle a browser object that can be missing. For example, `window.cookieStore` does not exist on
  an insecure page or in an older browser. `GetValueAsync` then throws `JSException`, because the
  property is not defined. It returns a `null` reference only when the property exists and holds
  `null` or `undefined`. Code copied from Web Storage passes the `JSException` to the caller, or
  fails with `NullReferenceException` for a `null` reference. In both cases, fail the acquisition
  with `NotSupportedException`, so it is not kept, and set the `JSException` as its inner
  exception. Translate only `JSException`. A disconnect or a timeout must reach the caller as it
  is. `NotSupportedException` is only the default that the Cookie Store API proposal (#69844)
  proposes. Until API review approves it, raise this behavior as an open question in the API
  proposal of the family. Do not add an API that only reports whether the object exists. Web IDL has no such member,
  so it would be a convenience API.
- Wrap identity-bearing objects and preserve identity. `Window` owns a shared wrapper, such as
  `Storage`, and hands the same one to every caller. So never implement `IAsyncDisposable` or a
  public `DisposeAsync` on it. Implement the internal `IInternalAsyncDisposal` explicitly, and let
  `Window` call it when the scope ends. The review of the API proposal rejected two other designs.
  A public `IAsyncDisposable` let one caller break the wrapper for the rest of the scope. An
  explicit `IAsyncDisposable` implementation hid `DisposeAsync` from IntelliSense, but
  `await using` still compiled. With the internal interface, `await using` does not compile.
- This disposal rule covers only shared wrappers that `Window` hands out. Disposal of an object
  that a call creates and the caller owns is not designed yet. Examples are the `Cache` that
  `caches.open()` returns and a `Lock` from Web Locks. When a family returns such an object, stop
  and raise it as an open question.
- Never let disposal throw. `Window` disposes features in access order, and an exception stops the
  rest. Do not wait for a pending acquisition either. It could hold up scope disposal until the JS
  interop timeout, which is one minute by default. Release the reference only when the acquisition
  has already completed successfully. A reference that arrives after disposal is never released.
  This is accepted. It happens only when the scope ends during an acquisition, and it leaves at
  most one reference for each family. A circuit that waits a minute to close costs more. After
  disposal, throw `ObjectDisposedException` from every member.
- When the caller passes no `CancellationToken`, call the `IJSRuntime` overload without a token.
  Any token, even `CancellationToken.None`, turns off the default timeout of `JSRuntime`.
- Pass JS arguments in `params` form, such as `InvokeAsync<CookieListItem?>("get", name)`, as most
  of the repository does. Write the argument list as `[...]` only when an argument could be taken
  for something else. An array or `null` becomes the argument list itself, and a `TimeSpan` or
  `CancellationToken` binds to the timeout or cancellation overload.

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
- When the browser object can be missing, add `<exception cref="NotSupportedException">` to each
  member that reaches it.
- Do not document `JSException`, for example for an exceeded quota. Interop types stay out of the
  public contract.
- When unsure, search the repository for the phrasing and use the most common form.

## Verify

- Add unit tests to `src/Components/Platform/test`, one file per family. Cover interop identifiers
  and arguments, lazy acquisition, a single shared acquisition, acquiring again after a failure,
  stable and distinct instances, disposal of the acquired reference, `ObjectDisposedException` after
  disposal, and disposal that finishes without waiting for a pending acquisition. When the browser
  object can be missing, also cover a `JSException` from the acquisition and a `null` reference.
  Every member throws `NotSupportedException`, and disposal does not throw.
- When a method passes a dictionary or an enum, also assert the JSON that reaches the browser. A
  mock of `IJSRuntime`, like the one in `StorageTest`, records the .NET objects before
  serialization. So it cannot catch a missing `[JsonIgnore]` that sends `"path":null`, or an enum
  value with the wrong string. Use a test runtime that derives from `JSRuntime` and records the
  `ArgsJson` it receives in `BeginInvokeJS`. Cover each member with a default set to `null`, an
  empty options object, and each enum value. Trimming is a separate risk. Only the trimmed
  WebAssembly app below can show it.
- Keep tests few and readable. Avoid tricks that depend on ordering. When ordering cannot be
  avoided, make it visible in the test and explain it in one comment.
- Add the new type to both `LiveBrowserObjectMethods_AreAsynchronous` and
  `SharedInstances_CannotBeDisposedByApplicationCode` in `PublicApiTest`.
- Exercise the public API in Interactive Server and WebAssembly through `PlatformTestApp` and
  `PlatformTestApp.E2E.Tests`. These E2E tests run on Helix in CI with a prebuilt app. A local run
  from the build output does not use that layout, so also run the tests from the output of
  `dotnet publish` on the E2E project.
- Publish a trimmed WebAssembly application. Confirm that the unused families and the unused
  members of a used family are gone. When the family passes or returns a dictionary, also confirm
  that every dictionary member reaches the browser and comes back. Trimming can drop a member
  without a warning.
- Update `PublicAPI.Unshipped.txt`, build, pack, and inspect the final diff. Use explicit
  `ProjectReference` and `PackageReference` items in project files, as
  `docs/ReferenceResolution.md` describes.
