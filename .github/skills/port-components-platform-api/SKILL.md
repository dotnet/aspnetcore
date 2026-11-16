---
name: port-components-platform-api
description: >-
  Port one standard non-DOM browser API family, such as Cookie Store, Screen, or Clipboard, to
  Microsoft.AspNetCore.Components.Platform as a typed .NET projection reached from
  IBrowserPlatform.Window, the way Web Storage is exposed as Window.LocalStorage. USE FOR wrapping,
  porting, or projecting a browser API (window.* or navigator.*) for Blazor, such as "wrap the
  Cookie Store API as a .NET API" or "add navigator.clipboard to Components.Platform"; designing the
  public API and API proposal of a family; mapping Web IDL types to .NET; extending or testing a
  family in src/Components/Platform; and the disposal, cancellation, event, trimming, and E2E rules of
  that package. DO NOT USE FOR DOM manipulation, application-specific JavaScript, migrating
  existing framework code such as ProtectedBrowserStorage, or filing the API review issue itself
  (use api-review).
---

# Port a Components Platform API

Create one reviewable vertical slice, a typed projection of one browser API family with its tests.
Web Storage (`Storage.cs`, `WindowWebStorageExtensions.cs`, and `test/StorageTest.cs`) is the
reference implementation. Read it before writing code, and copy its shape unless a rule below says
otherwise. Cookie Store (`CookieStore.cs`, its dictionaries and enum, `test/CookieStoreTest.cs`,
and `CookieStoreTests` in the E2E tests) shows what Web Storage lacks. It has a browser object that
can be missing, dictionaries, a Web IDL enum, an `[Experimental]` member, an event, and JSON
assertions in unit tests. URL (`Url.cs`, `UrlSearchParams.cs`, and `WindowUrlExtensions.cs`) shows
objects that the application creates and owns, and an `iterable<>`.

## Scope

- Port one API family per change, so each family stays small and separately reviewable.
- Keep DOM manipulation and application-specific JavaScript out of this package.
- Port the events of the family with the shared event listener API, as "Add events" below
  describes. Leave out other callbacks from JavaScript to .NET, such as the callback that
  `navigator.locks.request()` calls while it holds the lock. Their design is not decided yet. List
  them as deferred members, and raise them as an open question.
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
  the `change` event is implemented as an event key instead of `onchange`, and
  `CookieStoreManager` is excluded. It is exposed to
  `Window` too, but only through another entry point, `ServiceWorkerRegistration.cookies`, so it
  belongs to a separate slice. Check the `Exposed` attribute in the Web IDL before you give such a
  reason.
- Check the browser support of each member in the browser compatibility data on MDN. Plan a member
  that is not Baseline as `[Experimental]`, as the next section describes. A dictionary member that
  was added recently is a typical case, such as `CookieInit.partitioned`.
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
- Map a Web IDL constructor to `CreateAsync` on a `<Type>Constructor` object, and reach that object
  through a synchronous property of the global object. For example, `new URL(url)` becomes
  `Window.Url.CreateAsync(url)`. Put static operations, such as `URL.parse()`, on the same object.
  The created object belongs to the caller. "Implement" below says how it is disposed.
- Mark each member that is not Baseline with
  `[Experimental("ASP0040", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]`. The whole package
  shares this ID, so never allocate a new one. Write its lines in `PublicAPI.Unshipped.txt` without
  an ID prefix. In tests and demo pages, suppress the warning with `#pragma warning disable ASP0040`
  around the use, not with `NoWarn`.
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
- Name the extension class `<Owner><Type>Extensions` after the type that the property returns, such
  as `WindowScreenExtensions` for `Screen`, and `NavigatorStorageManagerExtensions` for
  `navigator.storage`, which returns a `StorageManager`. Do not name it after the property, such as
  `NavigatorStorageExtensions`. That name is easy to confuse with Web Storage, a property name can
  be plural, such as `locks`, and the type name matches the MDN page. `WindowWebStorageExtensions`
  keeps the name of its specification, because two properties share one type there.
- Get the instance with `window.GetOrAddFeature(...)`, and pass a subkey when two properties share
  one type, as `LocalStorage` and `SessionStorage` do. The factory receives the internal
  `FeatureContext`, which holds what all projections of the scope share. So a new shared part does
  not change the signature of every factory. Pass the projection only what it uses, such as
  `context.JSRuntime`, and `context.CreateEventListenerCollection()` for a family with events.
- Write the factory as a `static` lambda, such as
  `window.GetOrAddFeature(static context => new Screen(context.JSRuntime))`. It captures nothing,
  so the compiler caches one delegate, and the property allocates nothing on each access. A lambda
  that captures a variable creates a new delegate every time the property is read.
- Let the trimmer drop each unused member of a used family too. Do not reach family members through
  reflection or tables that reference every member. Use `[DynamicDependency]` only on a method that
  passes or receives a dictionary through JS interop, as the type mapping describes. Keep the build free of trim
  warnings.
- Do not add JavaScript for a family. Use the `IJSRuntime` and `IJSObjectReference` features of
  .NET 10 and later, such as `InvokeConstructorAsync`, `GetValueAsync`, and `SetValueAsync`. The
  package has one shared JavaScript bundle, `js/Platform.ts`, for events and callbacks from
  JavaScript to .NET. Reach it only through the event listener API, and do not add a function for
  one family to it. If a feature seems to need JavaScript of its own, stop and raise it as an open
  question.
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
  exception, as `CookieStore` does. Translate only `JSException`. A disconnect or a timeout must
  reach the caller as it is. Do not add an API that only reports whether the object exists. Web IDL has no such member,
  so it would be a convenience API.
- Wrap identity-bearing objects and preserve identity. `Window` owns a shared wrapper, such as
  `Storage`, and hands the same one to every caller. So never implement `IAsyncDisposable` or a
  public `DisposeAsync` on it. Implement the internal `IInternalAsyncDisposal` explicitly, and let
  `Window` call it when the scope ends. The review of the API proposal rejected two other designs.
  A public `IAsyncDisposable` let one caller break the wrapper for the rest of the scope. An
  explicit `IAsyncDisposable` implementation hid `DisposeAsync` from IntelliSense, but
  `await using` still compiled. With the internal interface, `await using` does not compile.
- This disposal rule covers only shared wrappers that `Window` hands out. An object that a call
  creates and the caller owns, such as the `Url` from `Window.Url.CreateAsync()` or the `Cache`
  from `caches.open()`, implements public `IAsyncDisposable` itself. Do not add an owner type or a
  wrapper around it. When another object also returns the same type, such as `url.SearchParams`,
  keep one sealed type, and say in its XML docs that its owner disposes it and callers must not, as
  `UrlSearchParams` does. Review accepted the risk that a caller disposes such an instance anyway,
  as with a `Stream`.
- A caller-owned object that a component keeps for a long time, such as a `WebSocket`, leaks when
  the component is disposed before `CreateAsync` completes. Nobody receives the object. When a
  family creates such an object, raise this as an open question.
- Never let disposal throw. `Window` disposes features in access order, and an exception stops the
  rest. Do not wait for a pending acquisition either. It could hold up scope disposal until the JS
  interop timeout, which is one minute by default. Release the reference only when the acquisition
  has already completed successfully. A reference that arrives after disposal is never released.
  This is accepted. It happens only when the scope ends during an acquisition, and it leaves at
  most one reference for each family. A circuit that waits a minute to close costs more. After
  disposal, throw `ObjectDisposedException` from every member.
- Do not add handling for a race or a failure that can happen only while the scope is disposed. In
  Interactive Server, `CircuitHost` calls `MarkPermanentlyDisconnected()` on the JS runtime before
  it disposes the scope. So every JS call during disposal fails at once with
  `JSDisconnectedException`, and `JSObjectReference.DisposeAsync` ignores that exception. In
  WebAssembly, the scope is not disposed while the page is open. Make an exception only for a fix of
  one or two lines that stops a real leak in .NET, such as a callback that stays registered on a
  token the caller keeps for a long time.
- When the caller passes no `CancellationToken`, call the `IJSRuntime` overload without a token.
  Any token, even `CancellationToken.None`, turns off the default timeout of `JSRuntime`.
- When a member takes a `CancellationToken` but cannot get the browser object, throw that failure,
  such as `NotSupportedException` for a missing object, even if the token was canceled meanwhile.
  Nothing was done, so the failure tells the caller more. Once the operation itself can go ahead,
  the token decides. A failure of the operation while the token is canceled becomes
  `OperationCanceledException` for that token.
- Pass JS arguments in `params` form, such as `InvokeAsync<CookieListItem?>("get", name)`, as most
  of the repository does. Write the argument list as `[...]` only when an argument could be taken
  for something else. An array or `null` becomes the argument list itself, and a `TimeSpan` or
  `CancellationToken` binds to the timeout or cancellation overload.

## Add events

Events use one API for every family. `CookieStore.ChangeEvent` and `Window.StorageEvent` are the
references.

- Do not project an event handler attribute, such as `onchange`, as a property or as
  `SetOnChangeAsync()`. A projection is shared in the scope, so one handler slot would let one caller
  replace or remove the handler of another, and the handler that the page's own JavaScript sets.
- Give each event one key, a static `EventType<TTarget, TEvent>` property named `<Name>Event`, on
  the target type, such as `CookieStore.ChangeEvent` for the `change` event. For an event on
  `Window`, declare the key as a static member in an `extension(Window)` block of the family's
  extension class, as `WindowWebStorageExtensions` does for `Window.StorageEvent`. Keys belong to
  one target even when two targets fire events with the same name, because the event object can
  differ. A key of another target does not compile.
- When the target is not `Window`, give it two `AddEventListenerAsync<TEvent>` overloads, one with a
  `CancellationToken` and one without, and copy them from `CookieStore` with their XML docs.
  `Window` already has them. The result is a plain `IAsyncDisposable` that removes the listener.
  Do not add `RemoveEventListenerAsync()` or the `once`, `capture`, `passive`, and `signal` options.
  Canceling the token removes the listener, as disposing the result does.
- Create the key with `EventTypes.Create<TTarget, TEvent>(name, shapesJson)`. The shape JSON is an
  `ArgumentShape` list (see `js/Platform.ts`) that names the properties to copy from the event
  object. `null` copies a plain value, a dictionary, or a sequence of them as it is. When a property
  holds a shared object that the package already projects, such as `storageArea`, use
  `{"$known":[...]}` and the `EventTypes.Create` overload that converts a payload into the event
  object, as `StorageEventPayload` does. The bridge never creates a JS object reference for an
  event. When an event carries any other browser object, stop and raise it as an open question.
- Map the event interface to a sealed class with `{ get; init; }` properties, like a dictionary that
  the browser returns. Keep the Web IDL name, such as `CookieChangeEvent`, and copy only the members
  that the event interface itself declares. When an event is a plain `Event` with no members of
  its own, stop and raise the .NET event type as an open question.
- Take `context.CreateEventListenerCollection()` in the factory. In disposal, dispose the listeners
  first and the reference of the target after them, because a listener needs that reference to
  remove itself. When the event object contains a dictionary, put `[DynamicDependency]` for that
  dictionary type on `AddEventListenerAsync`, as `CookieStore` does for `CookieListItem`.
- Unit tests check the shape JSON and the event name that reach the browser, the event object that
  a listener receives, and that two listeners stay independent. Copy the event tests in
  `CookieStoreTest`. The E2E test fires the event in the browser, also from code that is not .NET,
  and checks that a removed listener receives nothing, as `CookieStoreTests` does.

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
  `ArgsJson` it receives in `BeginInvokeJS`, such as `SerializingJSRuntime` in `CookieStoreTest`. Cover each member with a default set to `null`, an
  empty options object, and each enum value. Trimming is a separate risk. Only the trimmed
  WebAssembly app below can show it.
- Keep tests few and readable. Avoid tricks that depend on ordering. When ordering cannot be
  avoided, make it visible in the test and explain it in one comment.
- Add the new type to `LiveBrowserObjectMethods_AreAsynchronous` in `PublicApiTest`. Add a shared
  type to `SharedInstances_CannotBeDisposedByApplicationCode`. Check that a caller-owned type can
  be disposed, as `CallerOwnedUrlSearchParams_CanBeDisposedByApplicationCode` does.
- Exercise the public API in Interactive Server and WebAssembly through `PlatformTestApp` and
  `PlatformTestApp.E2E.Tests`. These E2E tests run on Helix in CI with a prebuilt app. A local run
  from the build output does not use that layout, so also run the tests from the output of
  `dotnet publish` on the E2E project.
- Keep each E2E step from passing on the text of the previous step. Show each result as
  `<operation>: <result>` in one status element with a `data-run` counter that goes up when a run
  completes. Read the counter before the click, and wait for it to go up by one before checking the
  text, as `CookieStoreDemo` and `CookieStoreTests` do.
- Publish a trimmed WebAssembly application. Confirm that the unused families and the unused
  members of a used family are gone. When the family passes or returns a dictionary, also confirm
  that every dictionary member reaches the browser and comes back. Trimming can drop a member
  without a warning.
- Update `PublicAPI.Unshipped.txt`, build, pack, and inspect the final diff. Use explicit
  `ProjectReference` and `PackageReference` items in project files, as
  `docs/ReferenceResolution.md` describes.
