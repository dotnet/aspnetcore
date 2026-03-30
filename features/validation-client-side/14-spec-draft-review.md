# Spec Review — Iteration 1

**Spec under review:** `features/validation-client-side/13-spec-draft.md`
**Review date:** 2026-03-26

---

## Summary

No issues found in this section.

The summary is clear, specific, and describes the three-layer solution at an appropriate level of detail. The motivation (Blazor SSR gap + MVC jQuery dependency) is well articulated, and the scope (both Blazor and MVC) is stated upfront.

---

## Goals

### Feedback 1.1 — Bundle size goal should be more precise

The goals reference "~3 KB Brotli" in passing but this is not stated as a measurable target. Given that size reduction (118 KB → ~3 KB) is a key selling point, the spec should state an explicit size budget (e.g., "the JS library must not exceed N KB Brotli-compressed, excluding the remote validation provider"). This makes the goal measurable and gives implementers a regression target.

**Severity:** Low — clarification, not a blocker.

### Feedback 1.2 — "Drop-in replacement" goal should qualify MVC version scope

The goal states the library is a "drop-in replacement" for jQuery unobtrusive validation in MVC. It should clarify which MVC versions are targeted. Specifically: does this target ASP.NET Core MVC only, or also legacy ASP.NET MVC 5 apps? The `data-val-*` protocol is the same across both, but the `remote` provider's URL format and anti-forgery token handling may differ. The spec should state explicitly that only ASP.NET Core MVC is in scope (if that is the intent).

**Severity:** Low — scope clarification.

---

## Non-goals

### Feedback 2.1 — "Source generator" non-goal should mention trimming/AOT implications

The non-goal about source generators mentions it's a "potential future optimization" but doesn't explain _why_ this matters. The `DefaultClientValidationService` uses `Type.GetProperty()` and `GetCustomAttributes<T>(inherit: true)` — both of which are problematic for Native AOT and aggressive trimming. Since .NET 10 is pushing AOT further, the spec should acknowledge that the reflection-based approach may need revisiting for AOT scenarios, and note whether the current design is intended to work under trimming or not.

**Severity:** Medium — affects future compat story. AOT/trimming is a key .NET 10 theme.

---

## Proposed Solution

### Feedback 3.1 — Missing scenario: Form reset clears validation state

**Gap identified.** The spec does not describe what happens when a form is reset (via `<button type="reset">` or `form.reset()`). Research confirms that the current implementation does **not** handle the `reset` event — the `EventManager` only attaches `submit`, `input`, and `change` handlers. After a form reset:
- Element values return to defaults (browser behavior)
- But CSS error classes (`input-validation-error`) remain applied
- Error message spans retain their content
- `hasBeenInvalid` flag remains set in the `WeakMap` state
- The validation summary is not cleared

This is a UX bug that users will encounter. The spec should either:
1. Add a scenario describing reset behavior (listen for `reset` event, clear all validation state for that form), or
2. Explicitly call this out as a known limitation in the first release.

jQuery unobtrusive validation has the same gap, so this would be an improvement opportunity.

**Severity:** Medium — observable UX issue.

### Feedback 3.2 — Missing scenario: Interaction with Blazor interactive render modes

The spec focuses on static SSR, but what happens when a form transitions from static SSR to interactive (Server or WebAssembly) render mode? For example:
- A page initially renders statically with `<ClientSideValidator />`
- After Blazor circuit connects, the form becomes interactive
- Blazor's own `EditContext` validation kicks in

Does the JS validation library conflict with Blazor's interactive validation? Are there duplicate error messages? Does the JS library need to detect when a circuit is active and stand down? The spec should describe this transition scenario or explicitly state that `<ClientSideValidator />` is only intended for use with static SSR forms (not interactive ones).

**Severity:** Medium — likely user confusion scenario.

### Feedback 3.3 — Missing scenario: `InputFile` and `InputRadio` components

Research confirms that `InputFile` and `InputRadio` do **not** extend `InputBase<T>` — they extend `ComponentBase` directly. This means:
- `InputFile` will not automatically emit `data-val-*` attributes. A `[FileExtensions]` annotation on the model property won't produce client-side validation when using `<InputFile>`. The `fileextensions` JS provider exists but has no C# counterpart that can reach `InputFile`.
- `InputRadio` relies on `InputRadioGroup<T>` (which does extend `InputBase<T>`) for validation attribute emission. The spec should clarify how radio group validation works end-to-end — does `InputRadioGroup` emit `data-val-*` on its container `<div>`, or do individual `<InputRadio>` elements need them?

The spec should document these component-specific behaviors, even if briefly.

**Severity:** Medium — users will try `[Required]` on `InputFile` and `InputRadio` bound properties and wonder why client validation doesn't work.

### Feedback 3.4 — Scenario 1 rendered HTML: `novalidate` timing clarification

In Scenario 1, the rendered HTML shows `novalidate` on the `<form>` element as if it's server-rendered. However, `novalidate` is actually set by the JS library's `DomScanner.scan()` at runtime, not by the Blazor server rendering. The spec should clarify this distinction — the server renders the form without `novalidate`, and the JS library adds it during initialization. This matters because there's a brief window between page load and script execution where the browser's native validation is active.

**Severity:** Low — accuracy of the example.

### Feedback 3.5 — Scenario 2 (MVC drop-in): Missing migration guidance for `$.validator.unobtrusive.parse()`

The scenario mentions that `$.validator.unobtrusive.parse()` can be replaced with `window.__aspnetValidation.parse()`, but doesn't address other jQuery unobtrusive APIs that MVC apps commonly use:
- `$.validator.unobtrusive.adapters.add()` / `$.validator.unobtrusive.adapters.addBool()` etc. — how do these map to `addProvider()`?
- `$.validator.addMethod()` — the jQuery validation plugin's custom method API
- Event hooks: `$.validator.defaults.showErrors`, `$.validator.defaults.invalidHandler`

The spec should either provide a migration table for common APIs or state that migration from custom jQuery validation extensions requires a rewrite to the `addProvider()` API.

**Severity:** Low — developer experience for MVC migration.

### Feedback 3.6 — Scenario 5 (Custom validators): `addProvider` not exposed on public API

Research document `09-custom-attributes-scenarios.md` identifies a gap: the `setProvider()` method (to override built-in providers) is not exposed on the public `window.__aspnetValidation` API in Blazor mode. Only `addProvider()` is exposed. This means a developer cannot override a built-in provider's behavior (e.g., replace the `email` regex) without importing internal modules. The spec's Scenario 5 shows `addProvider` for new providers but doesn't address overriding existing ones.

**Severity:** Low — extensibility gap, can be addressed in a follow-up.

### Feedback 3.7 — Scenario 8 (Remote validation): Network error behavior

The spec states that on network error, the remote provider "returns valid (does not block the user)." This is the jquery-validation convention, but it's worth calling out that this creates a security/UX consideration: if the server is unreachable, uniqueness checks (e.g., "is this email already taken?") silently pass. The spec should acknowledge this design choice and its implications, or provide guidance on how developers can customize this behavior.

**Severity:** Low — design rationale.

### Feedback 3.8 — Architecture: Script delivery for MVC apps

The architecture section states the JS library "ships as a static web asset in the `Microsoft.AspNetCore.Components.Web` package." For MVC apps that don't reference `Microsoft.AspNetCore.Components.Web`, how do they obtain the script? The research found the built bundle only in sample project `wwwroot/js/` directories, not in a distributable location. The spec should clarify the distribution mechanism for MVC:
- Is it published as a standalone npm package?
- Is it included in the `Microsoft.AspNetCore.Mvc` or shared framework package?
- Or do MVC developers copy the file manually?

This is critical for the "drop-in replacement" goal.

**Severity:** High — the MVC delivery story is under-specified and is a primary goal.

### Feedback 3.9 — Architecture: `data-valmsg-replace` behavior difference between Blazor and MVC

Open Question 5 raises this but it should be promoted to a scenario or at least a documented behavior difference. MVC's `asp-validation-for` tag helper emits `data-valmsg-replace="true"` by default. Blazor's `ValidationMessage<T>` does **not** emit this attribute. The JS library's `ErrorDisplay` module checks for `data-valmsg-replace="false"` to preserve existing content. Without the attribute, the default behavior is to replace — which happens to be correct for both, but the inconsistency means the behaviors are accidentally aligned rather than explicitly matched.

**Severity:** Low — works today by coincidence, but fragile.

---

## Assumptions

### Feedback 4.1 — Missing assumption: Script loading order and timing

The spec assumes the JS library will be loaded and initialized before the user interacts with the form, but doesn't state this explicitly. The `<ClientSideValidator />` renders the `<script>` tag inside the `<form>` element, which means the script loads after the form content. There's a timing window where:
1. The form is visible and interactive
2. The script hasn't loaded/executed yet
3. The user submits the form — no client validation runs, and the form submits to the server

This is the expected graceful degradation (server validation catches it), but it should be documented as an explicit assumption: "Client validation is a progressive enhancement. Forms are functional before the script loads; server validation handles any submissions that occur before JS initialization."

**Severity:** Low — documents existing behavior.

### Feedback 4.2 — Missing assumption: Reflection and trimming compatibility

The `DefaultClientValidationService` uses `Type.GetProperty()`, `PropertyInfo.GetCustomAttributes<T>()`, and `PropertyInfo.GetCustomAttribute<T>()`. These are not trimming-safe by default. The spec should state whether the feature is expected to work under PublishTrimmed/PublishAot scenarios, and if not, add this as an explicit assumption or limitation.

**Severity:** Medium — .NET 10 AOT push makes this relevant.

### Feedback 4.3 — Questionable assumption: `RemoteAttribute` type name stability

The spec states "`[Remote]` attribute and its base class `RemoteAttributeBase` belong to the `Microsoft.AspNetCore.Mvc` namespace and are not expected to be used with Blazor models." The `DefaultClientValidationService` checks for `RemoteAttributeBase` by string comparison against `"Microsoft.AspNetCore.Mvc.RemoteAttributeBase"` (walking the type hierarchy). This avoids an assembly dependency on MVC but is inherently fragile — if the type is ever renamed or moved, the guard silently fails. The spec should document this as a known trade-off rather than presenting the guard as robust.

**Severity:** Low — known trade-off, low probability of breakage.

---

## References

### Feedback 5.1 — GitHub issue reference should link directly to the proposal comment

The references section lists the GitHub issue (`#51040`) and "Original proposal (javiercn)" separately, but the direct URL to the proposal comment would be more useful: `https://github.com/dotnet/aspnetcore/issues/51040#issuecomment-3706000376`. Currently the reference text says "Comment on #51040" but doesn't include the full URL.

**Severity:** Low — convenience.

### Feedback 5.2 — Missing reference: Phil Haack's aspnet-client-validation specific version/commit

The reference to `aspnet-client-validation` points to the repository root but doesn't specify which version or commit was analyzed. The library has evolved over time, and specific claims (e.g., "~4 KB gzip", provider pattern, validation timing) may not hold for all versions. Pin to a specific commit or release tag.

**Severity:** Low — traceability.

### Feedback 5.3 — Missing reference: Constraint Validation API browser compatibility data

The spec assumes "all modern browsers" support the Constraint Validation API but doesn't link to compatibility data. Adding a reference to [caniuse.com/constraint-validation](https://caniuse.com/constraint-validation) or the MDN browser compat tables would strengthen the assumption.

**Severity:** Low — supporting evidence.

---

## Overall Assessment

The specification is **comprehensive and well-structured**. It covers the three-layer architecture clearly, provides concrete code examples for key scenarios, and identifies the right open questions. The core design decisions (Constraint Validation API, `data-val-*` protocol compatibility, `WeakMap` state tracking, capture-phase submit interception) are sound and well-researched.

### Issues requiring resolution before implementation:

| # | Feedback | Severity | Section |
|---|----------|----------|---------|
| 3.8 | MVC script delivery mechanism is under-specified | **High** | Proposed Solution |
| 2.1 | AOT/trimming implications of reflection-based design | Medium | Non-goals |
| 3.1 | Form reset does not clear validation state | Medium | Proposed Solution |
| 3.2 | Interaction with Blazor interactive render modes undefined | Medium | Proposed Solution |
| 3.3 | InputFile and InputRadio don't get automatic client validation | Medium | Proposed Solution |
| 4.2 | Trimming/AOT compatibility not stated | Medium | Assumptions |
| 1.1 | Bundle size should be a measurable target | Low | Goals |
| 1.2 | MVC version scope (Core only vs. MVC 5) not clarified | Low | Goals |
| 3.4 | `novalidate` is JS-applied, not server-rendered | Low | Proposed Solution |
| 3.5 | MVC migration guidance for jQuery APIs incomplete | Low | Proposed Solution |
| 3.6 | `setProvider` not on public API for overriding built-ins | Low | Proposed Solution |
| 3.7 | Remote provider network-error-returns-valid rationale | Low | Proposed Solution |
| 3.9 | `data-valmsg-replace` Blazor/MVC inconsistency | Low | Proposed Solution |
| 4.1 | Script loading timing / progressive enhancement | Low | Assumptions |
| 4.3 | RemoteAttribute string check fragility | Low | Assumptions |
| 5.1 | GitHub comment URL not fully specified | Low | References |
| 5.2 | aspnet-client-validation version not pinned | Low | References |
| 5.3 | Browser compat reference missing | Low | References |
