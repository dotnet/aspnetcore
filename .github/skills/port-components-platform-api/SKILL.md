---
name: port-components-platform-api
description: >-
  Port one standard non-DOM browser API family to Microsoft.AspNetCore.Components.Platform
  and migrate an existing Components IJSRuntime consumer to it. Use for browser API
  projections, scattered JS interop cleanup, or Platform package additions.
---

# Port a Components Platform API

Create one reviewable vertical slice: a typed projection and a real adoption.

## Scope

- Choose one standard browser API that can replace an existing production `IJSRuntime` call site.
- Use Web IDL, browser compatibility data, and MDN to define the family and explicit exclusions.
- Keep DOM manipulation and application-specific JavaScript out of this package.

## Implement

- Put the projection in `Microsoft.AspNetCore.Components.Platform`.
- Follow Web IDL names and behavior. Do not add convenience APIs, polyfills, or .NET substitutions.
- Keep live browser access asynchronous and raw JS interop types and identifiers non-public.
- Use direct constructor, property, and method interop before adding a JavaScript module.
- Wrap identity-bearing objects, preserve identity, and dispose wrappers asynchronously.
- Add cancellation only when it maps to `AbortSignal`.

## Adopt

Replace the selected consumer's direct interop with the projection. Preserve existing public API
compatibility; if a legacy path must remain, make the framework's default path use the projection.
Remove old JavaScript only when no caller remains.

## Verify

- Test interop identifiers, arguments, lazy acquisition, failure, and disposal.
- Exercise the public API in Interactive Server and WebAssembly.
- Update `PublicAPI.Unshipped.txt`, build, pack, and inspect the final diff.
