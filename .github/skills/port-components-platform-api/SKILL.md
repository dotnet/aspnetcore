---
name: port-components-platform-api
description: >-
  Port a browser API family into Microsoft.AspNetCore.Components.Platform or refactor direct
  IJSRuntime usage to an existing typed projection. Use for browser-owned singletons, JavaScript
  constructors, identity-bearing wrappers, explicit options conversion, disposal, or browser E2E
  tests with Microsoft.AspNetCore.Components.Testing. Do not use for DOM manipulation or
  application-specific JavaScript.
---

# Port a Components Platform API

Add one complete vertical slice to the existing `Microsoft.AspNetCore.Components.Platform` package.

Use Web Storage, URL, and Fetch in `src/Components/Platform` as implementation-pattern examples.
They are not a substitute for the complete declaration inventory required by the feature
specification.

## 1. Define the boundary

Pin the TypeScript declaration, Web Platform DX, and MDN browser-compat-data revisions. List every
member in the selected Web IDL family and classify it as included or explicitly excluded.

Stable Baseline Newly and Widely declarations ship normally. Apply `[Experimental]` with the
package diagnostic ID to included limited or experimental declarations. Exclude DOM-bound,
deprecated, obsolete, withdrawn, origin-trial-only, and prefixed declarations.

Classify each browser value:

| Value | Representation |
| --- | --- |
| Scalar or immutable snapshot | Copied .NET value |
| Browser-owned singleton | Stable facade with lazy reference acquisition |
| Constructed or identity-bearing object | Sealed wrapper over an internal `IJSObjectReference` |
| Options dictionary | Explicit trimming-safe conversion |
| Live child object | Stable wrapper owned by its parent |

Do not implement until ownership and disposal are explicit.

## 2. Implement the public projection

- Add the family to the existing package.
- Use the Web IDL type and member names with .NET casing.
- Keep `IJSRuntime` and `IJSObjectReference` internal.
- Use `InvokeConstructorAsync`, `GetValueAsync`, and `SetValueAsync` before adding JavaScript.
- Keep live browser operations asynchronous.
- Add `CancellationToken` only as a projection of an `AbortSignal` parameter, and verify that it
  aborts the browser operation.
- Preserve browser failures, including `DOMException` name and data.
- Make disposal asynchronous and idempotent.
- Reject operations after disposal.
- Recursively unwrap projected references in dictionaries, unions, and sequences.
- Enforce parent, callback, and transfer ownership lifetimes where applicable.

## 3. Add focused unit tests

Use fake interop objects to verify:

- exact constructor, property, and method identifiers
- exact arguments and option keys
- no interop during facade construction
- lazy acquisition and stable identity
- `CancellationToken` to `AbortSignal` behavior where supported
- recursive projected-reference unwrapping
- projected browser exception data
- ownership and disposal
- rejection after disposal

## 4. Add browser tests

Use `Microsoft.AspNetCore.Components.Testing`.

This is the experimental .NET 11 package tracked by
`https://github.com/dotnet/aspnetcore/issues/66394` and initially implemented by
`https://github.com/dotnet/aspnetcore/pull/65958`. Follow the current source implementation because
the test-runner shape has evolved since the original proposal.

1. Add a focused consumer page for each cohesive interface or behavior.
2. Cover Interactive Server, Interactive WebAssembly, and defined static SSR behavior.
3. Exercise only public Platform APIs.
4. Derive the E2E class from `BrowserTest` and apply `[UITest]`.
5. Start the app through `ServerFactory`.
6. Route Playwright through the server.
7. Call `WaitForInteractiveAsync` before interaction.
8. Share behavior assertions between render modes.
9. Verify construction or access, pass-through, failure, and lifetime behavior.
10. Cover Chromium, Firefox, and WebKit according to the repository test matrix.

The E2E project uses Playwright with MSTest on Microsoft Testing Platform. Do not replace it with the
older xUnit shape from the original API proposal.

## 5. Refactor a consumer

When replacing direct JS interop:

1. Identify the browser API and every JavaScript member used.
2. Confirm the typed Platform family covers the same behavior.
3. Replace `IJSRuntime` injection with `IBrowserPlatform`.
4. Preserve cancellation, errors, and disposal.
5. Remove JavaScript only when no other caller uses it.
6. Run the consumer test in Server and WebAssembly when both are supported.

Do not move DOM or application-specific interop into the Platform package.

## 6. Finish

- Update XML documentation and `PublicAPI.Unshipped.txt`.
- Update the pinned declaration inventory and explicit exclusion report.
- Run public API guards and asynchronous-surface checks.
- Run focused unit tests.
- Run targeted Components.Testing E2E tests.
- Build, trim, and pack the shipping project.
- Inspect the package contents.
- Run `git diff --check`.
