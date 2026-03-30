# Async Blazor Form Validation — Specification (v3)

## Summary

{Describe what the feature does and the motivation for the feature.

This document proposes adding [brief description of the feature] to [product or service] to address [problem or opportunity].}

## Goals

{List of problems/opportunities that we want to address with this feature. Should reflect the problems users currently face and describe the existing approaches available and their trade-offs.

It should explain what we want to make possible and how it will improve the user experience or solve a problem.}

- Support `System.ComponentModel.DataAnnotations` APIs via `DataAnnotationsValidator`.
- Provide enough extensibility for creating validator components that integrate other async validation libraries (e.g., `FluentValidation`).

## Non-goals

{Should explicitly call out problems we aren't trying to solve and include why. This section is optional and doesn't have to contain anything unless the goals can be ambiguous. When there aren't any non-goals just write N/A in this section.}

- Implement async validators themselves, do validator discovery, etc.
- Incremental error reporting for form submit
- Debouncing (atm)

## Proposed solution

{Describe the proposed solution at a high level, include a list of scenarios that the feature will support with samll code-snippets that showcase what code developers will write and example outcomes where appropriate.

When changing an existing feature, we expect to see a before and after comparison of the code and the outcomes.}

### 1. Async form validation with `DataAnnotationsValidator`

As a Blazor application developer, I can include async validations (e.g., async validation attributes) for my form models and get correct validation without other code changes.

**Example:**

**Expected outcome:**

**Before:**

**After:**

### 2. Async form validation of complex models with `DataAnnotationsValidator`

**Example:**

**Expected outcome:**

**Before:**

**After:**

- Per-field async validation
- Pending validation indication
- Crashed/timeouted validation indication
- Automatic cancellation after changes
- Request per-field re-validation
- CSS classes
- Backward compatibility, throwing in sync path
- SSR compatibility (streaming rendering)

### N. Integrating third-party validation library

**Example:**

**Expected outcome:**

**Before:**

**After:**

{### [Name of scenario 1]

Short description in one or two sentences.

Snippets showcasing the user experience when using the feature to achieve the scenario. Include expected outcomes where appropriate.

Snippet explanations should be at most one or two sentences.}

## Assumptions

{List any assumptions that are being made as part of the proposal. This can include assumptions about user behavior, technical constraints, third party dependencies, and so on.}

- Existence of `Validator.TryValidateObjectAsync`, `Validator.TryValidatePropertyAsync`

## References

- [dotnet/aspnetcore #7680](https://github.com/dotnet/aspnetcore/issues/7680) — Original design sketch for async support in Blazor form validation
- [dotnet/aspnetcore #40244](https://github.com/dotnet/aspnetcore/issues/40244#issuecomment-1044298329) — Workaround sketch for doing async form validation with current Blazor API
- [Blazored #31](https://github.com/Blazored/FluentValidation/issues/31) — Discussion about issues and workarounds while using async FluentValidation validators with current Blazor API
- [Blazored #38](https://github.com/Blazored/FluentValidation/issues/38) — Anopther discussion about issues and workarounds while using async FluentValidation validators with current Blazor API,
- [Blazilla](https://github.com/loresoft/Blazilla/) — Form validator package that works around the missing async features in the current Blazor API
- [dotnet/designs #363](https://github.com/dotnet/designs/pull/363)  — Design draft for adding async validation support in the BCL

## Notes

- Use Blazilla as benchmark of making async integration easier/better
- Validation states: valid/invalid/pending/cancelled/initial?
  - per field
  - per form? as events? (currently: `OnValidSubmit`, `OnInvalidSubmit`)
