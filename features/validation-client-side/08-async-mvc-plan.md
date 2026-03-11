# Implementation Plan: Async Support, Remote Provider, and MVC Wiring

## Overview

This plan adds three capabilities to the client-side validation library:

1. **Async provider support** — `ValidationProvider` returns `boolean | string | Promise<boolean | string>` (modeled after aspnet-client-validation). All validation is always-async internally; sync providers are transparently awaited.
2. **MVC wiring** — `MvcWiring.ts` with `parse(selector)` API, `data-valmsg-replace` support, and auto-detection entry point.
3. **Blazor RemoteAttribute guard** — C# throws `NotSupportedException` when `RemoteAttribute` is applied to a Blazor form, directing developers to a future solution.

### Always-Async Design

We use an **always-async** approach: `validateElement()`, `validateAndUpdate()`, and `validateForm()` always return `Promise`. The submit handler always calls `preventDefault()`, runs validation, and re-submits via `requestSubmit()` on success.

This eliminates ~80 lines of sync/async branching code that a dual-path approach would require (tracking which providers are async, split submit handler paths, `instanceof Promise` checks at every call site). The aspnet-client-validation library proves this pattern works in production. The only cost is one microtask tick on submit for sync-only forms — imperceptible to users.

> **Note — Dual-path alternative:** If profiling reveals measurable overhead from the always-async submit path, a dual-path optimization is possible. It would: (1) track async providers via `addAsyncProvider()` / `isAsync()` on `ValidationEngine`, (2) return `string | Promise<string>` from `validateElement` with `instanceof Promise` branching, (3) split the submit handler into sync fast-path (direct `preventDefault`) and async slow-path (`preventDefault` + `requestSubmit`). This adds ~80 lines but lets sync-only forms skip the `preventDefault` + `requestSubmit` dance entirely. We defer this unless needed.

---

## Phase 1: Expand Type System for Async

**Goal:** Make `ValidationProvider` accept `Promise` returns while keeping all existing sync providers unchanged.

### 1a. Update `Types.ts`

```typescript
// BEFORE
export type ValidationProvider = (
  value: string,
  element: ValidatableElement,
  params: Record<string, string>
) => boolean | string;

// AFTER
export type ValidationProviderResult = boolean | string;

export type ValidationProvider = (
  value: string,
  element: ValidatableElement,
  params: Record<string, string>
) => ValidationProviderResult | Promise<ValidationProviderResult>;
```

The `ValidationProviderResult` alias helps callers that need to work with the unwrapped result. Existing sync providers that return `boolean | string` are a valid subset of this type — no changes needed to any built-in provider.

### 1b. `ValidationEngine` — no changes needed

The `ValidationEngine` stores providers in a `Map<string, ValidationProvider>`. Since the type is widened, no API changes are needed. `addProvider()` and `getProvider()` work as before.

```typescript
// Unchanged — just stores/retrieves providers
export class ValidationEngine {
  private providers: Map<string, ValidationProvider> = new Map();
  addProvider(name: string, provider: ValidationProvider): void;
  setProvider(name: string, provider: ValidationProvider): void;
  getProvider(name: string): ValidationProvider | undefined;
  hasProvider(name: string): boolean;
}
```

### 1c. Tests for Phase 1

- Verify existing `addProvider` with sync functions still works identically.
- Verify `addProvider` with an async function (returns Promise) registers and retrieves correctly.
- Verify the `ValidationProviderResult` type is correctly exported.

**Files changed:** `Types.ts`
**Files added:** None
**Risk:** Low — type-only change, all existing providers are unaffected.

---

## Phase 2: Always-Async Validation Core

**Goal:** Make `validateElement`, `validateAndUpdate`, and `validateForm` always return `Promise`. Internally, each provider result is awaited — sync providers resolve immediately via `await` on a non-Promise value (which is a no-op microtask).

### 2a. `ValidationCoordinator.validateElement()` — always async

```typescript
// BEFORE (sync)
validateElement(element: ValidatableElement): string { ... }

// AFTER (always async)
async validateElement(element: ValidatableElement): Promise<string> {
  const state = this.getState(element);
  if (!state) return '';
  const value = this.getElementValue(element);

  for (const directive of state.directives) {
    const provider = this.engine.getProvider(directive.rule);
    if (!provider) continue;

    const result = await provider(value, element, directive.params);
    const error = this.resolveResult(result, directive);
    if (error) return error;
  }
  return '';
}

private resolveResult(result: ValidationProviderResult, directive: ValidationDirective): string {
  if (result === false) return directive.message;
  if (typeof result === 'string') return result;
  return ''; // true => valid
}
```

This is clean and linear — no branching, no `instanceof Promise` checks. `await` on a sync value (e.g., `true`) resolves immediately.

### 2b. `validateAndUpdate()` — always async

```typescript
async validateAndUpdate(element: ValidatableElement): Promise<boolean> {
  const error = await this.validateElement(element);
  if (error) {
    this.markInvalid(element, error);
    return false;
  }
  this.markValid(element);
  return true;
}
```

### 2c. `validateForm()` — parallel async

```typescript
async validateForm(form: HTMLFormElement): Promise<boolean> {
  const elements = this.getFormElements(form);
  const results = await Promise.all(
    elements.map(element => this.validateAndUpdate(element))
  );

  const allValid = results.every(v => v);
  const firstInvalidIndex = results.findIndex(v => !v);
  if (firstInvalidIndex >= 0) {
    elements[firstInvalidIndex].focus();
  }

  this.updateFormSummary(form);
  return allValid;
}
```

All fields validate in parallel (like aspnet-client-validation and jquery-validation). Within each field, directives run sequentially — a sync failure on `required` prevents the async `remote` call.

### 2d. Tests for Phase 2

- Test `validateElement` with all-sync providers: resolves to `''` (empty string = valid).
- Test `validateElement` with sync provider returning `false`: resolves to directive's error message.
- Test `validateElement` with async provider returning `Promise<true>`: resolves to `''`.
- Test `validateElement` with async provider returning `Promise<"custom error">`: resolves to `"custom error"`.
- Test `validateElement` with mixed sync + async: sync fail on earlier directive prevents async call.
- Test `validateForm`: parallel validation of multiple fields, returns `false` if any field fails.
- Test `validateForm`: focuses first invalid element.
- Test `validateAndUpdate`: updates CSS classes after resolution.

**Files changed:** `ValidationCoordinator.ts`
**Risk:** Medium — core validation flow changes from sync to async. All callers must `await`.

---

## Phase 3: Always-Async Submit Handler

**Goal:** The submit handler always calls `preventDefault()`, runs async validation, and re-submits via `requestSubmit()` on success.

### 3a. Update `EventManager` submit handler

```typescript
private resubmitting = false;

attachSubmitInterception(): void {
  this.submitHandler = (event: SubmitEvent) => {
    // Guard: let re-submitted events pass through
    if (this.resubmitting) return;

    const form = event.target as HTMLFormElement;
    if (!form.querySelector('[data-val="true"]')) return;

    // Check for formnovalidate
    const submitter = event.submitter;
    if (submitter?.hasAttribute('formnovalidate')) return;

    // Always prevent — validation is async
    event.preventDefault();
    event.stopPropagation();

    this.coordinator.validateForm(form).then(isValid => {
      if (isValid) {
        // Re-submit the form, bypassing our handler
        this.resubmitting = true;
        form.requestSubmit(submitter as HTMLElement | undefined);
        this.resubmitting = false;
      }
    });
  };

  document.addEventListener('submit', this.submitHandler, true);
}
```

This is significantly simpler than the dual-path alternative — no async tracking per form, no `formHasAsyncProviders()`, no `instanceof Promise` branching.

### 3b. Enhanced navigation interaction

When Blazor's enhanced nav is active, the re-submitted form event must still reach Blazor's bubble-phase handler. Since we use capture phase and `requestSubmit()` dispatches a new event, this works naturally — the new event bubbles and Blazor handles it.

**Verification needed:** Test that `requestSubmit()` with a submit button fires a new SubmitEvent that Blazor's enhanced nav intercepts correctly.

### 3c. Input/change event handlers — async-aware

Update the input/change handlers in `EventManager` to `await` the async `validateAndUpdate`:

```typescript
// In attachInputListeners — change handler:
this.coordinator.validateAndUpdate(element).then(() => {
  this.coordinator.updateFormSummary(form);
});

// In attachInputListeners — input handler (only clear errors):
if (state.hasBeenInvalid && state.currentError) {
  this.coordinator.validateAndUpdate(element).then(() => {
    this.coordinator.updateFormSummary(form);
  });
}
```

### 3d. Tests for Phase 3

- Test submit with sync-only form: validates and re-submits on success.
- Test submit with invalid sync-only form: `preventDefault` called, no re-submit.
- Test submit with async providers: waits for resolution, re-submits on success.
- Test submit with async failure: no re-submit, errors displayed.
- Test re-entry guard: the re-submitted event passes through without validation.
- Test `formnovalidate`: skips validation entirely (existing behavior preserved).
- Test input event: only clears errors when `hasBeenInvalid` is true.
- Test change event: always validates.

**Files changed:** `EventManager.ts`
**Risk:** Medium — submit handler changes, but the logic is straightforward. Must test Blazor enhanced nav interaction.

---

## Phase 4: Remote Validation Provider (MVC Only)

**Goal:** Implement a `remote` provider that makes HTTP requests to validate fields, with per-element caching. This provider is MVC-only — Blazor will throw an error (see Phase 6).

### 4a. Remote provider implementation

Create new file `RemoteProvider.ts`:

```typescript
import { ValidationProvider, ValidatableElement, ValidationProviderResult, AsyncValidationCache } from './Types';

interface RemoteCache {
  data: string;
  result: ValidationProviderResult;
}

const remoteCache = new WeakMap<ValidatableElement, RemoteCache>();

export const remoteProvider: ValidationProvider = (
  value: string,
  element: ValidatableElement,
  params: Record<string, string>
): ValidationProviderResult | Promise<ValidationProviderResult> => {
  if (!value) return true; // Empty fields are valid (required handles emptiness)

  const url = params['url'];
  if (!url) return true;

  const method = (params['type'] || 'GET').toUpperCase();

  // Build request data
  const data = new URLSearchParams();
  data.set(element.name, value);

  // Collect additional fields
  const additionalFields = (params['additionalfields'] || '').split(',').filter(Boolean);
  const form = element.closest('form');

  for (const spec of additionalFields) {
    const fieldName = resolveFieldName(element.name, spec);
    if (fieldName === element.name) continue; // Skip self-reference
    const otherElement = form?.elements.namedItem(fieldName);
    if (otherElement && 'value' in otherElement) {
      data.set(fieldName, (otherElement as HTMLInputElement).value);
    }
  }

  // Check cache
  const cacheKey = data.toString();
  const cached = remoteCache.get(element);
  if (cached && cached.data === cacheKey) {
    return cached.result; // Sync return from cache
  }

  // Make HTTP request
  const requestUrl = method === 'GET' ? `${url}?${cacheKey}` : url;

  return fetch(requestUrl, {
    method,
    headers: method === 'POST'
      ? { 'Content-Type': 'application/x-www-form-urlencoded' }
      : {},
    body: method === 'POST' ? data.toString() : undefined,
  })
    .then(response => response.json())
    .then((serverResult: unknown): ValidationProviderResult => {
      let result: ValidationProviderResult;
      if (serverResult === true || serverResult === 'true') {
        result = true;
      } else if (typeof serverResult === 'string') {
        result = serverResult; // Custom error message from server
      } else {
        result = false; // Use default error message
      }

      // Cache the result
      remoteCache.set(element, { data: cacheKey, result });
      return result;
    })
    .catch(() => true); // Network error: don't block the user
};

function resolveFieldName(currentName: string, spec: string): string {
  // "*.PropertyName" → replace prefix with current element's prefix
  if (spec.startsWith('*.')) {
    const prefix = currentName.substring(0, currentName.lastIndexOf('.') + 1);
    return prefix + spec.substring(2);
  }
  return spec;
}
```

**Design decisions:**
- Uses `fetch` API (not XMLHttpRequest) — modern, Promise-native.
- `WeakMap` for cache — allows garbage collection when elements are removed from DOM.
- Cache keyed on serialized request data — automatically invalidates when any field value changes.
- Network errors resolve to `true` — remote validation is best-effort, never blocks the user on network failure.
- Response protocol matches MVC: `true`/`"true"` = valid, any string = error message, `false` = use default message.

### 4b. Register as a standard provider

```typescript
// In MvcWiring.ts (not BlazorWiring.ts)
engine.addProvider('remote', remoteProvider);
```

No special `addAsyncProvider` needed — the always-async validation core handles Promise returns from any provider transparently.

### 4c. Tests for Phase 4

Testing remote provider requires mocking `fetch`. Jest provides `jest.fn()` and `global.fetch` mocking:

- Test: empty value returns `true` synchronously.
- Test: cached result returns synchronously (no fetch call).
- Test: server returns `true` → provider resolves `true`, result cached.
- Test: server returns `"Username is taken"` → provider resolves with error string.
- Test: server returns `false` → provider resolves `false` (default message used).
- Test: network error → provider resolves `true` (don't block user).
- Test: additional fields are collected from form and sent.
- Test: cache invalidated when field value changes (different cacheKey).
- Test: `*.PropertyName` resolution works for nested model names.
- Test: POST vs GET method selection.

**Files added:** `RemoteProvider.ts`
**Files changed:** None (registration happens in MvcWiring.ts in Phase 5)
**Risk:** Low — self-contained provider, well-understood protocol.

---

## Phase 5: MVC Support

**Goal:** Create MVC wiring module, add `data-valmsg-replace` support, and build dual-mode entry point.

### 5a. `data-valmsg-replace` support in `ErrorDisplay.ts`

```typescript
// In showFieldError():
for (const el of messageElements) {
  const shouldReplace = el.getAttribute('data-valmsg-replace') !== 'false';
  if (shouldReplace) {
    el.textContent = message;
  }
  // Always toggle CSS classes regardless of replace setting
  el.classList.remove(this.css.messageValid);
  el.classList.add(this.css.messageError);
}

// In clearFieldError():
for (const el of messageElements) {
  const shouldReplace = el.getAttribute('data-valmsg-replace') !== 'false';
  if (shouldReplace) {
    el.textContent = '';
  }
  el.classList.remove(this.css.messageError);
  el.classList.add(this.css.messageValid);
}
```

### 5b. Create `MvcWiring.ts`

```typescript
import { ValidationEngine } from './ValidationEngine';
import { ValidationCoordinator } from './ValidationCoordinator';
import { EventManager } from './EventManager';
import { DomScanner } from './DomScanner';
import { ErrorDisplay } from './ErrorDisplay';
import { registerBuiltInProviders } from './BuiltInProviders';
import { remoteProvider } from './RemoteProvider';
import { CssClassConfig, defaultCssClasses, ValidationProvider, ValidatableElement } from './Types';

export interface MvcValidationApi {
  /** Register a custom validation provider (sync or async) */
  addProvider(name: string, provider: ValidationProvider): void;
  /** Scan a DOM subtree for new validatable elements (replaces $.validator.unobtrusive.parse) */
  parse(selectorOrElement?: string | Element | ParentNode): void;
  /** Validate an entire form */
  validateForm(form: HTMLFormElement): Promise<boolean>;
  /** Validate a single field */
  validateField(input: ValidatableElement): Promise<boolean>;
}

export function initializeMvcValidation(cssOverrides?: Partial<CssClassConfig>): void {
  const css = { ...defaultCssClasses, ...cssOverrides };
  const engine = new ValidationEngine();
  const display = new ErrorDisplay(css);
  const coordinator = new ValidationCoordinator(engine, display);
  const eventManager = new EventManager(coordinator);
  const scanner = new DomScanner(coordinator, eventManager);

  // Register all built-in sync providers
  registerBuiltInProviders(engine);

  // Register remote provider (MVC-only)
  engine.addProvider('remote', remoteProvider);

  // Attach submit interception
  eventManager.attachSubmitInterception();

  // Initial scan
  scanner.scan(document);

  // Expose MVC-compatible API
  const api: MvcValidationApi = {
    addProvider: (name, provider) => engine.addProvider(name, provider),
    parse: (selectorOrElement?) => {
      if (!selectorOrElement) {
        scanner.scan(document);
      } else if (typeof selectorOrElement === 'string') {
        const root = document.querySelector(selectorOrElement);
        if (root) scanner.scan(root);
      } else {
        scanner.scan(selectorOrElement as ParentNode);
      }
    },
    validateForm: (form) => coordinator.validateForm(form),
    validateField: (input) => coordinator.validateAndUpdate(input),
  };

  (window as any).__aspnetValidation = api;
}
```

**`parse()` method:** Drop-in replacement for `$.validator.unobtrusive.parse()`. Accepts a CSS selector string, an Element, or a ParentNode. Calls `scanner.scan()` on the resolved root.

### 5c. Update `BlazorWiring.ts`

Update the API to reflect async return types:

```typescript
export interface ValidationServiceApi {
  addProvider(name: string, provider: ValidationProvider): void;
  scan(root?: ParentNode): void;
  validateForm(form: HTMLFormElement): Promise<boolean>;
  validateField(input: ValidatableElement): Promise<boolean>;
}
```

Note: `remote` provider is NOT registered in BlazorWiring — see Phase 6.

### 5d. Update `index.ts` — auto-detection entry point

```typescript
import { initializeBlazorValidation } from './BlazorWiring';
import { initializeMvcValidation } from './MvcWiring';

function initialize(): void {
  // Auto-detect: if Blazor's enhanced navigation is available, use Blazor mode
  const blazor = (window as any).Blazor;
  if (blazor && typeof blazor.addEventListener === 'function') {
    initializeBlazorValidation();
  } else {
    initializeMvcValidation();
  }
}

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', initialize);
} else {
  initialize();
}
```

### 5e. Tests for Phase 5

- Test `data-valmsg-replace="false"`: CSS classes toggle but content unchanged.
- Test `data-valmsg-replace="true"` (and absent): content replaced (existing behavior).
- Test `parse()` with CSS selector string.
- Test `parse()` with Element.
- Test `parse()` with no argument (scans document).
- Test auto-detection: Blazor object present → initializeBlazorValidation called.
- Test auto-detection: no Blazor → initializeMvcValidation called.
- Test MVC API exposed on `window.__aspnetValidation`.

**Files added:** `MvcWiring.ts`
**Files changed:** `ErrorDisplay.ts`, `BlazorWiring.ts`, `index.ts`
**Risk:** Low — additive changes, existing Blazor path untouched.

---

## Phase 6: Blazor RemoteAttribute Guard (C#)

**Goal:** When `RemoteAttribute` (or `RemoteAttributeBase`) is encountered during client validation attribute emission for a Blazor form, throw a clear error message. This prevents silent failure and directs developers to the future Blazor-compatible solution.

### 6a. Detection approach

`RemoteAttributeBase` lives in `Microsoft.AspNetCore.Mvc` namespace and inherits from `ValidationAttribute`. Our `DefaultClientValidationService.ComputeAttributes()` iterates all `ValidationAttribute` instances on a property. We add a check there.

**Important:** `RemoteAttribute` is in `Microsoft.AspNetCore.Mvc.ViewFeatures` assembly. The `Components.Forms` project should NOT take a dependency on that assembly. Instead, detect by checking the type name or base type name.

```csharp
// In DefaultClientValidationService.ComputeAttributes()
foreach (var attribute in attributes)
{
    // Guard: RemoteAttribute is not supported in Blazor
    if (IsRemoteAttribute(attribute))
    {
        throw new NotSupportedException(
            $"The '{attribute.GetType().Name}' attribute on property '{fieldName}' of type " +
            $"'{modelType.Name}' is not supported for client-side validation in Blazor. " +
            $"RemoteAttribute requires server-side AJAX endpoints which are an MVC pattern. " +
            $"Consider using a custom validation approach for Blazor forms.");
    }

    var adapter = _registry.GetAdapter(attribute);
    if (adapter is not null)
    {
        adapter.AddClientValidation(context, errorMessage);
    }
}

private static bool IsRemoteAttribute(ValidationAttribute attribute)
{
    // Check by type hierarchy name to avoid assembly dependency on Mvc.ViewFeatures
    var type = attribute.GetType();
    while (type is not null)
    {
        if (type.FullName == "Microsoft.AspNetCore.Mvc.RemoteAttributeBase")
        {
            return true;
        }
        type = type.BaseType;
    }
    return false;
}
```

### 6b. Tests for Phase 6

- Test with a mock attribute whose type hierarchy includes `RemoteAttributeBase` → throws `NotSupportedException`.
- Test with standard attributes (Required, Range, etc.) → no exception.
- Test error message contains the property name and model type.

**Files changed:** `DefaultClientValidationService.cs`
**Risk:** Low — simple type-name check, no new dependencies.

---

## Phase Summary

| Phase | Description | Files Changed | Files Added | Risk |
|-------|-------------|---------------|-------------|------|
| 1 | Type system for async | Types.ts | — | Low |
| 2 | Always-async validation core | ValidationCoordinator.ts | — | Medium |
| 3 | Always-async submit handler | EventManager.ts | — | Medium |
| 4 | Remote validation provider | — | RemoteProvider.ts | Low |
| 5 | MVC wiring + ErrorDisplay fix | ErrorDisplay.ts, BlazorWiring.ts, index.ts | MvcWiring.ts | Low |
| 6 | Blazor RemoteAttribute guard | DefaultClientValidationService.cs | — | Low |

### Implementation Order

Phases 1 → 2 → 3 are strictly sequential (each depends on the previous).

Phase 4 depends on Phase 1 (async provider type) but can be implemented in parallel with Phases 2-3 since it's a self-contained provider.

Phase 5 is largely independent — `ErrorDisplay` and `MvcWiring` can be built at any time. The `remote` registration in `MvcWiring` depends on Phase 4.

Phase 6 is fully independent (C# only).

**Recommended order:** 1 → 2 → 3 → 4 → 5 → 6, with Phase 6 done at any time.

### Test Strategy

Each phase has its own tests. All tests are Jest-based (JavaScript) except Phase 6 (xUnit/C#).

Existing tests (69 JS provider tests, 129 C# Forms tests) must continue to pass at each phase — they validate that built-in providers still work correctly with the async wrapper.

### Bundle Size Impact

- Phases 1-3 (async support): ~+0.2 KB Brotli (async/await, Promise.all — simpler than dual-path)
- Phase 4 (remote provider): ~+0.2 KB Brotli (fetch, caching, field resolution)
- Phase 5 (MVC wiring): ~+0.2 KB Brotli (MvcWiring module, ErrorDisplay change is negligible)
- **Total estimate:** ~2.5 KB → ~3.1 KB Brotli

---

## Design Decisions Log

### Why always-async instead of sync fast-path?

A dual-path approach would return `string` from `validateElement` when all providers are sync, and `Promise<string>` only when an async provider is encountered. This requires:
- Tracking which providers are async via `addAsyncProvider()` / `isAsync()` (~15 lines)
- `instanceof Promise` checks at every call site in coordinator (~20 lines)
- Split submit handler with sync fast-path and async slow-path (~30 lines)
- Per-form async tracking in `DomScanner` (~15 lines)

Total: ~80 extra lines of branching for zero perceptible UX benefit. The always-async approach uses `await` uniformly — `await` on a sync value resolves in one microtask (~0.01ms). The aspnet-client-validation library uses this pattern successfully in production.

The submit handler's `preventDefault` + `requestSubmit` dance adds one microtask of latency before the form actually submits. For sync-only forms this is the only cost. If profiling shows this matters, the dual-path optimization can be added later (see note at top of document).

### Why throw for RemoteAttribute in Blazor?

RemoteAttribute depends on MVC conventions:
- It generates URLs using MVC routing (`IUrlHelper`)
- It expects controller/action endpoints
- Its `AddValidation` method uses `ClientModelValidationContext` (MVC's context, not ours)

A Blazor-compatible remote validation solution would need a different design (e.g., Blazor circuit calls, Minimal API endpoints, or custom JS interop). Throwing now prevents silent failure and gives us a clean extension point for a future solution.

### Why WeakMap for remote cache?

When Blazor's enhanced navigation replaces DOM elements, the old elements are garbage collected. Using `WeakMap<ValidatableElement, RemoteCache>` ensures cache entries are cleaned up automatically — no manual cache invalidation needed for removed elements.

### Why `parse()` instead of `scan()` for MVC?

MVC developers are familiar with `$.validator.unobtrusive.parse(selector)`. Our `scan()` method already does the same thing but accepts a `ParentNode`. The `parse()` wrapper adds CSS selector string support and matches the expected MVC API name, reducing migration friction.
