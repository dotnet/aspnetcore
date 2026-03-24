# MVC Support Analysis — Dual-Mode JS Library

This document analyzes what it would take for our JS validation library to support **both Blazor SSR and MVC**, replacing the legacy jquery-validation + jquery-validation-unobtrusive stack.

---

## 1. Architecture Comparison

### Legacy MVC Stack (3 Libraries)

```
jquery (88 KB min)
  └── jquery-validation (25 KB min)    ← Actual validation logic
        └── jquery-validation-unobtrusive (5 KB min)  ← Bridge: reads data-val-* → jquery-validate rules
```

**Total: ~118 KB minified, jQuery dependency required.**

jquery-validation-unobtrusive is purely a bridge — it has NO validation logic. Its job is:
1. Parse `data-val-*` attributes from the DOM
2. Translate them into jquery-validation rules/messages
3. Attach jquery-validation to the form
4. Wire error display into `data-valmsg-for` spans and `data-valmsg-summary` divs

### Our Library (Self-Contained)

```
aspnet-core-validation.js (~2.5 KB Brotli)
  ├── DirectiveParser     — Parses data-val-* attributes
  ├── ValidationEngine    — Provider registry
  ├── BuiltInProviders    — 12 validation providers
  ├── ValidationCoordinator — Validates, calls Constraint Validation API
  ├── ErrorDisplay        — CSS class manipulation + message rendering
  ├── EventManager        — submit/input/change listeners
  ├── DomScanner          — DOM discovery + fingerprinting
  └── BlazorWiring        — Blazor-specific initialization
```

**Total: ~2.5 KB Brotli, zero dependencies.**

Our library does what all three legacy libraries do, in one package:
- Parses `data-val-*` attributes (= jquery-validation-unobtrusive)
- Runs validation logic (= jquery-validation)
- Has no jQuery dependency

---

## 2. What's Already Shared

Our library already uses the **exact same HTML conventions** as MVC:

### Attribute Protocol (✅ Fully Compatible)

| Convention | MVC | Our Library | Match? |
|-----------|-----|-------------|--------|
| `data-val="true"` on inputs | ✅ | ✅ | ✅ |
| `data-val-{rule}="{message}"` | ✅ | ✅ | ✅ |
| `data-val-{rule}-{param}="{value}"` | ✅ | ✅ | ✅ |
| `data-valmsg-for="{fieldName}"` on message spans | ✅ | ✅ | ✅ |
| `data-valmsg-summary="true"` on summary div | ✅ | ✅ | ✅ |
| `*.PropertyName` convention for Compare | ✅ | ✅ | ✅ |

### CSS Classes (✅ Fully Compatible)

| Class | Purpose | MVC | Our Library |
|-------|---------|-----|-------------|
| `input-validation-error` | Invalid input | ✅ | ✅ |
| `input-validation-valid` | Valid input | ✅ | ✅ |
| `field-validation-error` | Error message span | ✅ | ✅ |
| `field-validation-valid` | Valid message span | ✅ | ✅ |
| `validation-summary-errors` | Summary with errors | ✅ | ✅ |
| `validation-summary-valid` | Summary without errors | ✅ | ✅ |

### Validation Rules (✅ Mostly Compatible)

All 12 of our providers map 1:1 to rules that MVC adapters emit. See `06-js-validators.md` for the full comparison.

---

## 3. What Differs and Needs Work

### 3a. `data-valmsg-replace` Attribute

**MVC behavior:**
```html
<span data-valmsg-for="Email" data-valmsg-replace="true" class="field-validation-valid"></span>
```

- `data-valmsg-replace="true"` (default) — jquery-validation-unobtrusive replaces the span content with the error message
- `data-valmsg-replace="false"` — the span's original content is preserved; error is hidden/shown by CSS class toggling only

**Our library:** Always replaces span content. Does not check `data-valmsg-replace`.

**Fix required:** Read `data-valmsg-replace` in `ErrorDisplay.ts`. When `false`, toggle CSS classes but don't change `textContent`.

**Effort:** Small — a few lines in `showFieldError` / `clearFieldError`.

### 3b. Initialization / Wiring

**MVC (jquery-validation-unobtrusive):**
```javascript
// Initializes on $(document).ready()
$(function () {
    $jQval.unobtrusive.parse(document);
});
```
- No enhanced navigation — full page reloads
- For AJAX/dynamic content: manually call `$.validator.unobtrusive.parse(selector)`
- Exposes API on `jQuery.validator.unobtrusive`

**Our library (Blazor mode):**
```typescript
// index.ts
document.addEventListener('DOMContentLoaded', () => initializeBlazorValidation());

// BlazorWiring.ts
Blazor.addEventListener('enhancedload', () => service.scan(document));
// Exposes API on window.__aspnetValidation
```

**Key differences:**
1. Blazor mode hooks into `enhancedload` for re-scanning after enhanced navigation
2. MVC mode doesn't need `enhancedload` — but needs a public `parse(selector)` method for AJAX scenarios
3. MVC typically uses jQuery's `$(document).ready()`, but `DOMContentLoaded` is equivalent

### 3c. Form Submission Handling

**MVC (jquery-validation):**
- Hooks into `$(form).submit()` via jQuery events
- Blocks submission by returning `false` from the handler
- Uses jQuery's event system — MVC's AJAX helpers and other code interact via jQuery events

**Our library:**
- Uses native `addEventListener('submit', ..., true)` in capture phase
- Calls `event.preventDefault()` to block submission
- Checks for `formnovalidate` on submit buttons
- Works with Blazor's enhanced navigation submit handling (which uses bubble phase)

**For MVC:** Our capture-phase approach should work fine — it runs before any jQuery handlers. The `formnovalidate` support is already correct. However, we should verify no conflicts with `jquery.unobtrusive-ajax.js` if that's also loaded.

### 3d. Remote Validation

**MVC:** `jquery-validation-unobtrusive` registers a `remote` adapter that sends AJAX requests to a server endpoint for validation.

**Our library:** Not supported (by design — no async validation in prototype).

**For MVC support:** This is a gap. Remote validation is used by Identity (e.g., checking if a username is taken). Options:
1. Support as an optional add-on provider (not in core)
2. Document that remote validation requires falling back to the old stack
3. Implement async provider support (significant design change)

**Recommendation:** Defer. Remote validation is a separate concern and can be added as a plugin provider later without changing the core architecture.

### 3e. Error Display Hidden `<li>` in Summary

**MVC:** The validation summary initially renders with a hidden `<li>`:
```html
<div class="validation-summary-valid" data-valmsg-summary="true">
    <ul><li style="display:none"></li></ul>
</div>
```

jquery-validation-unobtrusive empties the `<ul>` and repopulates on errors.

**Our library:** Finds the `<ul>` inside the summary div and replaces content. Should handle the hidden `<li>` correctly since we clear before adding errors.

**Verification needed:** Test that initial hidden `<li>` doesn't cause issues.

### 3f. Dynamic Validation (parse on demand)

**MVC pattern for AJAX partial views:**
```javascript
// After AJAX loads new form content:
$.validator.unobtrusive.parse('#newFormContainer');
```

**Our library equivalent:**
```javascript
// After AJAX loads new form content:
window.__aspnetValidation.scan(document.getElementById('newFormContainer'));
```

**Already supported:** Our `scan()` accepts a root element. The API just needs to be documented for MVC users.

---

## 4. Proposed Dual-Mode Architecture

### Shared Core (unchanged)

These modules contain no framework-specific code and work for both MVC and Blazor:

| Module | Purpose | Changes Needed |
|--------|---------|----------------|
| `ValidationEngine.ts` | Provider registry | None |
| `BuiltInProviders.ts` | 12 validation providers | None |
| `DirectiveParser.ts` | `data-val-*` parsing | None |
| `DomScanner.ts` | DOM discovery + fingerprinting | None |
| `ValidationCoordinator.ts` | Validation orchestration | None |
| `EventManager.ts` | Submit/input/change events | None |
| `ErrorDisplay.ts` | CSS classes + messages | Add `data-valmsg-replace` support |
| `Types.ts` | Type definitions | None |

### Framework-Specific Wiring (small layer)

| Module | Mode | Purpose |
|--------|------|---------|
| `BlazorWiring.ts` | Blazor | Hooks into `enhancedload`, exposes `__aspnetValidation` |
| `MvcWiring.ts` | MVC | **NEW** — DOMContentLoaded init, exposes `parse(selector)` API |
| `index.ts` | Entry point | Auto-detects mode or uses explicit configuration |

### Entry Point Strategy

```typescript
// index.ts — auto-detect mode
if (typeof Blazor !== 'undefined' && Blazor?.addEventListener) {
  // Blazor enhanced navigation is available
  initializeBlazorValidation();
} else {
  // Standard MVC — no enhanced nav
  initializeMvcValidation();
}
```

Or, build two entry points:
- `aspnet-core-validation.blazor.js` — includes BlazorWiring
- `aspnet-core-validation.mvc.js` — includes MvcWiring
- `aspnet-core-validation.js` — includes both, auto-detects

### MvcWiring.ts (New Module)

```typescript
export function initializeMvcValidation(): void {
  const service = createValidationService();
  service.scan(document);

  // Expose jQuery-compatible API for MVC users
  (window as any).__aspnetValidation = {
    ...service,
    // Compatibility method matching $.validator.unobtrusive.parse()
    parse: (selector: string | Element) => {
      const root = typeof selector === 'string'
        ? document.querySelector(selector)
        : selector;
      if (root) service.scan(root);
    }
  };
}
```

### Changes Required Summary

| Change | Scope | Effort |
|--------|-------|--------|
| Add `data-valmsg-replace` support to `ErrorDisplay.ts` | Small code change | Low |
| Create `MvcWiring.ts` with MVC-specific initialization | New small file | Low |
| Update `index.ts` for auto-detection or dual entry points | Small code change | Low |
| Build configuration for separate/combined bundles | Build tooling | Low |
| Test with MVC form HTML patterns | Testing | Medium |
| Remote validation provider (optional) | New provider | Medium (deferred) |

**Total effort: Low.** The core library is already framework-agnostic. Only the thin wiring layer needs a second variant.

---

## 5. E2E Test Analysis

### Existing MVC Functional Tests

**Location:** `src/Mvc/test/Mvc.FunctionalTests/`

| Test Class | What It Tests | Reusable? |
|-----------|---------------|-----------|
| `HtmlGenerationTest.cs` | Server-rendered HTML attributes match baseline files | **No** — tests C# attribute emission, not JS behavior |
| `RemoteAttributeValidationTest.cs` | Remote validation endpoints return correct JSON | **No** — tests server endpoints |
| `InputValidationTests.cs` | Valid/invalid form submissions return correct status codes | **No** — tests server-side validation |
| `ClientValidationOptionsTests.cs` | `DisableClientValidation` suppresses `data-val-*` attributes | **No** — tests C# options |

**Key finding:** MVC's existing functional tests verify **server-side HTML generation and model validation**. They do NOT test client-side JavaScript validation behavior in a browser. There are no Selenium/Playwright tests for jquery-validation-unobtrusive.

### Existing Blazor E2E Tests

| Test Class | What It Tests | Reusable? |
|-----------|---------------|-----------|
| `FormsTest.cs` | EditForm with DataAnnotationsValidator (Selenium) | **Partially** — tests server-side validation flow, not our JS |
| `AddValidationIntegrationTest.cs` | Nested validation in Blazor SSR | **Partially** — tests server-side validation |

**Key finding:** Blazor E2E tests also focus on server-side validation, not client-side JS.

### MVC Test Web Sites with Validation HTML

These sites generate correct `data-val-*` HTML and could be adapted for client-side testing:

| Test Site | Forms Available | Notes |
|-----------|----------------|-------|
| `HtmlGenerationWebSite` | Warehouse (minlength, range, required), Customer, Order | Has validation summary and message spans |
| `BasicWebSite` | RemoteAttribute forms | Remote validation only |

**Baseline HTML files** are at `src/Mvc/test/Mvc.FunctionalTests/compiler/resources/`. Example:
```html
<!-- HtmlGenerationWebSite.HtmlGeneration_Home.EditWarehouse.html -->
<input type="text" data-val="true"
       data-val-minlength="The field City must be..." data-val-minlength-min="2"
       id="City" name="City" value="City_1" />
<span class="field-validation-valid" data-valmsg-for="Employee.Name"
      data-valmsg-replace="true"></span>
<div class="validation-summary-valid" data-valmsg-summary="true">
    <ul><li style="display:none"></li></ul>
</div>
```

### Recommended E2E Test Strategy

Since no existing tests cover client-side JS validation:

1. **Unit tests (Jest)** — Already comprehensive (69 tests for providers). Add more for:
   - `data-valmsg-replace="false"` behavior
   - MVC-style form HTML patterns (nested names like `Employee.Name`)
   - Summary display with hidden `<li>` initial state

2. **Integration tests (Jest + jsdom)** — Test full flow:
   - Create MVC-shaped DOM (from baseline HTML files)
   - Trigger form submit
   - Verify error messages appear in correct spans
   - Verify CSS class toggling
   - Verify summary population

3. **E2E tests (Playwright/Selenium)** — New browser tests:
   - Use the BlazorSSR sample app for Blazor mode
   - Create an MVC test page with identical validation rules
   - Verify both produce the same client-side behavior
   - Test AJAX partial view loading with `parse()` call

4. **Regression baseline approach:** Use the MVC baseline HTML files as DOM fixtures for Jest integration tests. This gives us MVC-compatible DOM structures without needing a running server.

---

## 6. Identity UI Consideration

The ASP.NET Identity UI scaffolded pages (`Register.cshtml`, `Login.cshtml`, etc.) currently include:

```html
@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

Where `_ValidationScriptsPartial.cshtml` loads:
```html
<script src="~/lib/jquery-validation/dist/jquery.validate.min.js"></script>
<script src="~/lib/jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js"></script>
```

**For migration to our library:**
- Replace both `<script>` tags with a single `<script src="~/lib/aspnet-core-validation.min.js"></script>`
- No changes to Razor views — all `asp-validation-for` / `asp-validation-summary` tag helpers generate the same HTML
- No changes to models — same DataAnnotations attributes
- Remove jQuery dependency (if no other jQuery usage)

**Size reduction:** ~118 KB (jQuery + jquery-validation + unobtrusive) → ~2.5 KB Brotli

---

## 7. MVC Compatibility Checklist

| Feature | Status | Notes |
|---------|--------|-------|
| Parse `data-val-*` attributes | ✅ Ready | Same protocol |
| Display errors in `data-valmsg-for` spans | ✅ Ready | Same protocol |
| Display errors in `data-valmsg-summary` div | ✅ Ready | Same protocol |
| CSS class toggling | ✅ Ready | Same class names |
| `data-valmsg-replace` attribute | ⚠️ Needs work | Small fix in ErrorDisplay |
| `DOMContentLoaded` initialization | ✅ Ready | Already does this |
| `parse(selector)` API for AJAX | ⚠️ Needs work | Expose `scan()` with selector support |
| Nested model naming (`Employee.Name`) | ✅ Ready | `equalto` already resolves `*.Property` |
| `formnovalidate` on submit buttons | ✅ Ready | Already supported |
| Remote validation | ⚠️ Needs async support | See Section 9 — feasible with Option B approach |
| No jQuery dependency | ✅ | Feature, not a limitation |

---

## 8. Summary (Non-Async Items)

**Our library is ~90% MVC-compatible already.** The attribute protocol, CSS classes, validation rules, and error display conventions are identical. The remaining work is:

1. **Small code changes** (~20 lines):
   - `data-valmsg-replace` support in ErrorDisplay
   - MvcWiring.ts with `parse()` API
   - Auto-detection or dual entry points

2. **Testing** (medium effort):
   - Jest integration tests with MVC-shaped DOM fixtures
   - Manual testing with Identity UI pages
   - Playwright E2E tests (new)

3. **Async / Remote Validation** (see Section 9):
   - Expand `ValidationProvider` type to support `Promise` returns (backward compatible)
   - Implement sync fast-path / async slow-path submit handler (Option B)
   - Add remote provider with fetch + result caching
   - ~200-300 lines of code, medium effort

The library's architecture is already framework-agnostic in its core. Only the thin initialization layer (`BlazorWiring.ts`) is Blazor-specific, and adding an equivalent `MvcWiring.ts` is straightforward. The biggest benefit for MVC users would be eliminating the jQuery dependency (118 KB → 2.5 KB Brotli).

---

## 9. Async Validation & Remote Attribute Support

### 9a. The RemoteAttribute — How It Works Today

The MVC `RemoteAttribute` enables server-side validation of individual fields via AJAX while the user fills out a form. A typical example is checking username availability.

**Server side:**
```csharp
public class RegisterModel
{
    [Remote(action: "IsUsernameAvailable", controller: "Validation")]
    public string Username { get; set; }
}

// Controller
public class ValidationController : Controller
{
    [AcceptVerbs("Get", "Post")]
    public IActionResult IsUsernameAvailable(string username)
    {
        if (_db.Users.Any(u => u.Name == username))
            return Json($"Username '{username}' is already taken.");
        return Json(true);
    }
}
```

**Emitted HTML attributes:**
```html
<input data-val="true"
       data-val-remote="'Username' is invalid."
       data-val-remote-url="/Validation/IsUsernameAvailable"
       data-val-remote-type="Get"
       data-val-remote-additionalfields="*.Username"
       name="Username" />
```

**Additional fields:** The `AdditionalFields` property allows sending related field values alongside the primary field (e.g., `"UserId1, UserId2, UserId3"` becomes `*.UserId1,*.UserId2,*.UserId3` in the attribute).

**Server response protocol:**
- `true` or `"true"` → field is valid
- Any other value → treated as error message string
- `false` → uses the default error message from `data-val-remote`

### 9b. How jquery-validation Handles Async

The jquery-validation `remote` method has a sophisticated async model built into the core validator:

```javascript
// From jquery-validation/src/core.js — remote method
remote: function(value, element, param, method) {
    // 1. Cache check — if same data was already validated, return cached result
    if (previous.old === optionDataString) {
        return previous.valid;  // true, false, or null (if still pending)
    }

    // 2. Start async request
    this.startRequest(element);  // increments pendingRequest counter
    $.ajax({
        data: data,
        url: param.url,
        success: function(response) {
            var valid = response === true || response === "true";
            validator.stopRequest(element, valid);  // decrements counter
            // If form was submitted while waiting, re-trigger submit
            if (valid && pendingRequest === 0 && formSubmitted) {
                $(currentForm).trigger("submit");
            }
        }
    });

    // 3. Return "pending" immediately
    return "pending";
}
```

**Key mechanism — `pendingRequest` counter:**
- `startRequest()` increments a counter and adds `pending` CSS class
- `stopRequest()` decrements it, and if it hits 0 AND a form submit was deferred, re-triggers submit
- The form submit handler checks: `if (validator.pendingRequest) { validator.formSubmitted = true; return false; }`
- This means: submit is blocked while requests are pending, and auto-retried when they complete

**The `"pending"` return value in the check loop:**
```javascript
// core.js — check() method
result = $.validator.methods[method].call(this, val, element, rule.parameters);
if (result === "pending") {
    this.toHide = this.toHide.not(this.errorsFor(element));
    return;  // stops checking further rules, doesn't mark valid or invalid
}
```

This is an elegant but deeply coupled design — `"pending"` is a magic return value understood by the core `check()` loop, and the entire form submission is built around the `pendingRequest` counter.

### 9c. How aspnet-client-validation Handles Async

The aspnet-client-validation library takes a simpler approach — its `ValidationProvider` type already supports `Promise`:

```typescript
// aspnet-client-validation type
type ValidationProvider = (
    value: string,
    element: HTMLInputElement,
    params: StringKeyValuePair
) => boolean | string | Promise<boolean | string>;
```

Its remote provider returns a `Promise`:
```typescript
remote: ValidationProvider = (value, element, params) => {
    let url = params['url'];
    let fields = resolveAdditionalFields(element, params.additionalfields);
    return new Promise((resolve, reject) => {
        let request = new XMLHttpRequest();
        // ... build GET or POST request ...
        request.onload = () => {
            let data = JSON.parse(request.responseText);
            resolve(data);  // true, false, or error message string
        };
        request.onerror = () => reject(/* ... */);
    });
};
```

The validation loop then uses `await`:
```typescript
// Simplified validation loop
for (let directive of directives) {
    let result = provider(value, element, params);
    if (result instanceof Promise) {
        result = await result;
    }
    // ... process result ...
}
```

### 9d. Impact Analysis — Adding Async to Our Library

Our current architecture is fully synchronous. Here's what each module would need:

#### Types.ts

```typescript
// CURRENT
type ValidationProvider = (
    value: string, element: ValidatableElement, params: Record<string, string>
) => boolean | string;

// PROPOSED — support both sync and async
type ValidationProviderResult = boolean | string;
type ValidationProvider = (
    value: string, element: ValidatableElement, params: Record<string, string>
) => ValidationProviderResult | Promise<ValidationProviderResult>;
```

**Impact:** Low. Existing sync providers continue working — they just return `boolean | string` directly, which is a subset of the new type.

#### ValidationCoordinator.ts — `validateElement()`

```typescript
// CURRENT (sync)
validateElement(element): string {
    for (const directive of state.directives) {
        const provider = this.engine.getProvider(directive.rule);
        const result = provider(value, element, directive.params);
        if (result === false) return directive.message;
        if (typeof result === 'string') return result;
    }
    return '';
}

// PROPOSED (async-aware)
async validateElement(element): Promise<string> {
    for (const directive of state.directives) {
        const provider = this.engine.getProvider(directive.rule);
        let result = provider(value, element, directive.params);
        if (result instanceof Promise) {
            result = await result;
        }
        if (result === false) return directive.message;
        if (typeof result === 'string') return result;
    }
    return '';
}
```

**Impact:** Medium. The method becomes async, which cascades to all callers.

#### ValidationCoordinator.ts — `validateForm()`

```typescript
// CURRENT (sync)
validateForm(form): boolean {
    let allValid = true;
    for (const [element, state] of this.elementState) {
        if (element.closest('form') !== form) continue;
        const error = this.validateElement(element);
        // ... mark valid/invalid ...
        if (error) allValid = false;
    }
    return allValid;
}

// PROPOSED (async-aware)
async validateForm(form): Promise<boolean> {
    let allValid = true;
    const validations: Promise<void>[] = [];
    for (const [element, state] of this.elementState) {
        if (element.closest('form') !== form) continue;
        validations.push(
            this.validateElement(element).then(error => {
                // ... mark valid/invalid ...
                if (error) allValid = false;
            })
        );
    }
    await Promise.all(validations);  // run all field validations in parallel
    return allValid;
}
```

**Impact:** Medium. Parallel validation of fields is actually better for remote validation — multiple fields can validate concurrently.

#### EventManager.ts — Form Submit (THE HARD PART)

```typescript
// CURRENT (sync)
document.addEventListener('submit', (event) => {
    const isValid = this.coordinator.validateForm(form);  // sync!
    if (!isValid) {
        event.preventDefault();
        event.stopPropagation();
    }
}, true);
```

**The fundamental problem:** `event.preventDefault()` must be called **synchronously** within the event handler. You cannot `await` a Promise and then decide to prevent default — by then the event has already been dispatched.

**Three possible solutions:**

**Option A — Always prevent, re-submit if valid (jquery-validation approach):**
```typescript
document.addEventListener('submit', (event) => {
    event.preventDefault();    // Always prevent
    event.stopPropagation();

    this.coordinator.validateForm(form).then(isValid => {
        if (isValid) {
            // Re-submit the form programmatically
            // Must bypass our own handler (use a flag)
            this._submitting = true;
            form.requestSubmit(submitButton);
        }
    });
}, true);
```

**Pros:** Reliable, matches jquery-validation behavior.
**Cons:** Always blocks first submit attempt. May interfere with Blazor's enhanced navigation handler (which expects to receive the submit event normally when validation passes). Double-submit risk if flag management is wrong.

**Option B — Synchronous fast-path, async slow-path:**
```typescript
document.addEventListener('submit', (event) => {
    // Run sync providers immediately
    const syncResult = this.coordinator.validateFormSync(form);
    if (syncResult === 'invalid') {
        event.preventDefault();
        event.stopPropagation();
        return;
    }
    if (syncResult === 'pending') {
        event.preventDefault();
        event.stopPropagation();
        // Run async providers
        this.coordinator.validateFormAsync(form).then(isValid => {
            if (isValid) form.requestSubmit(submitButton);
        });
        return;
    }
    // syncResult === 'valid' → let submit proceed normally
}, true);
```

**Pros:** No overhead when all providers are sync (current behavior preserved). Only blocks when async providers exist.
**Cons:** More complex internal logic — need to distinguish sync from async providers. Two code paths to maintain.

**Option C — Pre-validation on blur/change, gate submit on pending state:**
```typescript
// Track pending validations per element
private pendingCount = 0;

// On blur/change: trigger async validation eagerly
onFieldChange(element) {
    this.pendingCount++;
    this.coordinator.validateAndUpdate(element).then(() => {
        this.pendingCount--;
    });
}

// On submit: only block if async validations are pending
document.addEventListener('submit', (event) => {
    if (this.pendingCount > 0) {
        event.preventDefault();
        // Show "validation in progress" indicator
        // When pending hits 0, re-check and re-submit
        return;
    }
    // All async results are cached — sync check is fine
    const isValid = this.coordinator.validateFormCached(form);
    if (!isValid) {
        event.preventDefault();
        event.stopPropagation();
    }
}, true);
```

**Pros:** Best UX — user sees remote validation results as they type/blur, submit is instant if everything is already resolved.
**Cons:** Most complex. Requires result caching per element. Must handle edge case of value changing after async validation completed.

#### ErrorDisplay.ts

**Impact:** None. Display is always synchronous DOM manipulation. The coordinator calls `showFieldError`/`clearFieldError` after resolving the async result.

#### DomScanner.ts

**Impact:** None. Scanning is a synchronous DOM operation.

#### ElementState (Types.ts)

```typescript
// Add to ElementState
interface ElementState {
    // ... existing fields ...
    pendingValidation?: Promise<string>;  // if async validation is in flight
    lastValidatedValue?: string;          // for caching remote results
}
```

### 9e. Remote Provider Implementation

```typescript
// Proposed remote provider
engine.addProvider('remote', (value, element, params) => {
    if (!value) return true;

    const url = params['url'];
    if (!url) return true;

    const method = (params['type'] || 'GET').toUpperCase();
    const additionalFieldSpecs = (params['additionalfields'] || '').split(',').filter(Boolean);

    // Build field data
    const data = new URLSearchParams();
    data.set(element.name, value);

    for (const spec of additionalFieldSpecs) {
        if (spec === `*.${element.name.split('.').pop()}`) continue; // skip self
        const otherElement = resolveOtherElement(element, spec);
        if (otherElement) {
            const otherName = (otherElement as HTMLInputElement).name;
            data.set(otherName, (otherElement as HTMLInputElement).value);
        }
    }

    return fetch(url + (method === 'GET' ? '?' + data.toString() : ''), {
        method,
        headers: method === 'POST'
            ? { 'Content-Type': 'application/x-www-form-urlencoded' }
            : {},
        body: method === 'POST' ? data.toString() : undefined,
    })
    .then(response => response.json())
    .then(result => {
        if (result === true || result === 'true') return true;
        if (result === false || result === 'false') return false;
        if (typeof result === 'string') return result; // custom error message
        return false;
    })
    .catch(() => true); // On network error, don't block the user
});
```

**Key design decisions:**
- Uses `fetch` (not XMLHttpRequest) — modern, promise-based, lighter
- Returns `Promise<boolean | string>` — fits the proposed provider type
- Network errors resolve to `true` (valid) — matches aspnet-client-validation behavior; remote validation is best-effort
- Response protocol matches jquery-validation: `true`/"true" → valid, string → error message, `false` → use default message

### 9f. Caching Strategy

Both jquery-validation and aspnet-client-validation cache remote validation results:

```typescript
// Per-element cache
interface RemoteCache {
    previousData: string;      // serialized request params
    previousResult: boolean;   // cached validation outcome
}

// In the remote provider:
const cacheKey = data.toString();
const cached = element.__remoteCache;
if (cached && cached.previousData === cacheKey) {
    return cached.previousResult;  // return sync from cache
}
// ... make fetch request ...
// On success:
element.__remoteCache = { previousData: cacheKey, previousResult: valid };
```

This is critical because:
1. On form submit, validation runs for ALL fields — cached results avoid redundant requests
2. After user types and blurs, validation runs — then on submit, same value should not re-validate
3. When the value changes, the cache is automatically invalidated (different `cacheKey`)

### 9g. Recommended Approach — Option B (Sync Fast-Path)

**Recommendation: Option B** with caching gives the best balance of complexity and behavior:

1. **All existing sync providers continue to work identically** — zero regression risk
2. **Async providers only invoked when present** — no overhead for forms without remote validation
3. **Caching makes submit instant** after initial async validation on blur/change
4. **Form submit logic:**
   - Run all sync providers first — if any fail, block immediately (sync `preventDefault`)
   - If sync providers all pass but async providers exist, check cache
   - If cache is warm for all fields, use cached results (sync path)
   - If cache is cold, block submit, run async, re-submit on success

**Implementation phasing:**
1. **Phase 1:** Expand `ValidationProvider` type to allow `Promise` return
2. **Phase 2:** Make `validateElement` async-aware (await if Promise)
3. **Phase 3:** Split submit handler into sync/async paths
4. **Phase 4:** Implement remote provider with caching
5. **Phase 5:** Add pending UI state (CSS class on element, optional loading indicator)

### 9h. Blazor Considerations for Remote Validation

Remote validation in Blazor SSR has unique aspects:

1. **No `RemoteAttribute`** — Blazor doesn't use MVC's `RemoteAttribute`. A new Blazor-specific attribute or adapter pattern would be needed to emit `data-val-remote-*` attributes.

2. **Anti-forgery tokens** — Blazor SSR forms include anti-forgery tokens. Remote validation requests may need to include these for POST requests.

3. **Enhanced navigation** — After enhanced nav patches the DOM, remote validation caches should be cleared for changed elements (the fingerprint system in `DomScanner` already detects attribute changes).

4. **Blazor interop** — An alternative to HTTP-based remote validation is using Blazor's JS interop to call .NET methods directly. This would be a Blazor-specific enhancement, not applicable to MVC.

### 9i. Complexity & Risk Assessment

| Component | Change Scope | Risk | Notes |
|-----------|-------------|------|-------|
| `ValidationProvider` type | Type expansion | **Low** | Backward compatible — sync providers are a subset |
| `validateElement()` | Async wrapper | **Low** | `await` on non-Promise is a no-op |
| `validateForm()` | Async + parallel | **Medium** | Must handle mixed sync/async correctly |
| Submit handler | Sync/async split | **High** | Most complex change; Blazor enhanced nav interaction |
| Remote provider | New provider | **Low** | Self-contained, well-understood protocol |
| Result caching | New infrastructure | **Medium** | Per-element cache, invalidation on value change |
| Pending UI state | CSS class + display | **Low** | Optional enhancement |

### 9j. Summary

| Aspect | Assessment |
|--------|-----------|
| **Feasibility** | High — the architecture supports it cleanly with Option B |
| **Effort** | Medium — ~200-300 lines of code across 3-4 files |
| **Risk** | Low for sync path (no change), Medium for async path (new code) |
| **Breaking changes** | None — `ValidationProvider` type expansion is backward compatible |
| **Bundle size impact** | Minimal — ~0.3-0.5 KB Brotli for remote provider + async support |
| **Blazor applicability** | Partial — JS provider works, but needs C# adapter for emitting attributes |
| **MVC applicability** | Full — drop-in replacement for jquery-validation's remote method |
