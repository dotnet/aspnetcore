# Prior Art Analysis: aspnet-client-validation

**Library:** [haacked/aspnet-client-validation](https://github.com/haacked/aspnet-client-validation)
**Version reviewed:** Latest main branch (commit `d90831e`)
**Language:** TypeScript (single file: `src/index.ts`, ~1,565 lines)
**Bundle size:** 10.6 KB raw, ~4 KB gzipped
**License:** MIT
**Origin:** Fork of [ryanelian/aspnet-core-validation](https://github.com/ryanelian/aspnet-core-validation)

---

## 1. What It Does

aspnet-client-validation is a jQuery-free drop-in replacement for `jquery.validate.unobtrusive.js`. It reads the same `data-val-*` attribute protocol emitted by ASP.NET Core MVC tag helpers, and provides client-side validation without requiring jQuery (~112 KB savings).

**Core flow:**
1. On bootstrap, scans the DOM for `input|select|textarea` elements with `data-val="true"`
2. Parses `data-val-{rule}` (error message) and `data-val-{rule}-{param}` (rule parameters) attributes
3. Registers event listeners on each input (debounced `input`/`change` events)
4. On form submit, validates all tracked inputs; prevents submission if invalid
5. Updates `data-valmsg-for` spans with error messages and toggles CSS classes
6. Updates `data-valmsg-summary="true"` containers with a `<ul>` of all errors

---

## 2. Architecture

### 2.1 Key Types

| Type | Purpose |
|------|---------|
| `ValidationService` | Central orchestrator — scans DOM, manages inputs/forms, runs validators, renders errors |
| `MvcValidationProviders` | Container class with one `ValidationProvider` method per MVC validation rule |
| `ValidationProvider` | Function signature: `(value, element, params) => boolean \| string \| Promise<boolean \| string>` |
| `ValidationDirective` | Parsed representation of `data-val-*` attributes for one input element |
| `ValidatableElement` | Union type: `HTMLInputElement \| HTMLSelectElement \| HTMLTextAreaElement` |

### 2.2 Class Diagram (Conceptual)

```
ValidationService
├── providers: { [name]: ValidationProvider }       ← pluggable rule registry
├── messageFor: { [formUID]: { [name]: Element[] } } ← validation message spans
├── formInputs: { [formUID]: string[] }             ← input UIDs per form
├── validators: { [inputUID]: Validator }            ← per-input validation factories
├── formEvents: { [formUID]: callback }             ← submit handlers
├── inputEvents: { [inputUID]: callback }           ← input/change handlers
├── summary: { [inputUID]: string }                 ← current error messages
│
├── bootstrap(options?)                             ← entry point
├── scan(root?)                                     ← attach validation to DOM subtree
├── remove(root?)                                   ← detach validation from DOM subtree
├── watch(root?)                                    ← MutationObserver for dynamic DOM
├── addProvider(name, callback)                     ← register custom validator
├── addInput(input) / removeInput(input)            ← manage individual elements
├── addError(input, message) / removeError(input)   ← update UI
├── validateForm(form) / validateField(field)       ← programmatic validation
├── isValid(form) / isFieldValid(field)             ← query validity
├── highlight(input, ...) / unhighlight(input, ...) ← overridable CSS class toggling
└── createValidator(input, directives)              ← builds async validation pipeline
```

### 2.3 Element Tracking

The library assigns GUIDs to every managed DOM element (forms, inputs, validation spans) via a linear-scan `elementUIDs` array plus a `elementByUID` reverse map. This is necessary because DOM elements don't have stable unique identifiers (elements can lack `id` attributes, and `name` attributes are not unique across forms).

**Observation for our feature:** This GUID-based tracking is heavyweight. For Blazor SSR, we could use `WeakMap` or `WeakRef` for element-to-state mapping, avoiding memory leaks and O(n) lookups.

---

## 3. Built-in Validation Providers

| Provider Name | MVC Attribute | `data-val-*` Attributes Read | Validation Logic |
|---------------|--------------|------------------------------|------------------|
| `required` | `[Required]` | `data-val-required` | Trims value, checks non-empty; special handling for checkbox/radio groups |
| `length` | `[StringLength]` | `data-val-length`, `-min`, `-max` | `value.length` checks against min/max |
| `maxlength` | `[MaxLength]` | `data-val-maxlength`, `-max` | Same as `length` (reuses `stringLength` provider) |
| `minlength` | `[MinLength]` | `data-val-minlength`, `-min` | Same as `length` (reuses `stringLength` provider) |
| `range` | `[Range]` | `data-val-range`, `-min`, `-max` | `parseFloat` then compare |
| `regex` | `[RegularExpression]` | `data-val-regex`, `-pattern` | `new RegExp(pattern).test(value)` |
| `equalto` | `[Compare]` | `data-val-equalto`, `-other` | Resolves other field via `*.FieldName` selector protocol |
| `email` | `[EmailAddress]` | `data-val-email` | RFC822 regex |
| `url` | `[Url]` | `data-val-url` | Checks for `http://`, `https://`, or `ftp://` prefix |
| `phone` | `[Phone]` | `data-val-phone` | Regex: `^\+?[0-9\-\s]+$` |
| `creditcard` | `[CreditCard]` | `data-val-creditcard` | Luhn algorithm |
| `remote` | `[Remote]` | `data-val-remote`, `-url`, `-type`, `-additionalfields` | XHR GET/POST to server endpoint, expects JSON boolean |

**All providers skip validation when value is empty** — they return `true` to let `[Required]` handle presence validation. This is the correct behavior: an optional field with `[EmailAddress]` should not flag an empty value as invalid.

---

## 4. Directive Parsing Algorithm

The `parseDirectives` method implements a clean two-pass parse over an element's attributes:

```
Pass 1: Collect all attributes starting with "data-val-" into a flat dictionary
         e.g. { "required": "Name is required", "length": "...", "length-max": "100", "length-min": "2" }

Pass 2: For each key without a hyphen (= rule name):
         - Collect all keys that start with "{rule}-" (= parameters)
         - Build a directive: { error: "...", params: { max: "100", min: "2" } }
```

**Strength:** This is simple, generic, and protocol-compatible. It doesn't need to know the specific rules at parse time — it just parses the naming convention. Any `data-val-{rule}` attribute becomes a rule; any `data-val-{rule}-{param}` becomes a parameter.

**Takeaway for our feature:** This parsing algorithm is elegant and should be adopted. It decouples the attribute protocol from the validator implementations, enabling extensibility.

---

## 5. Validation Lifecycle & Timing

### 5.1 Input Events

```
Default event binding:
  - <input>, <textarea>: "input change"  (fires on every keystroke AND on blur/commit)
  - <select>:            "change"        (fires when selection changes)

Override: data-val-event="blur"  (per-field customization)

Debounce: 300ms (configurable via v.debounce = N)
```

### 5.2 Smart Invalidation (Input Event Gating)

The library implements a subtle UX refinement: on `input` events, validation **only clears** an existing error (turns a field back to valid). It does **not** mark a pristine field as invalid on `input`. Only `change` events (field commit / blur) can transition a field from valid to invalid.

```typescript
if (!input.dataset.valEvent && event.type === 'input'
    && !input.classList.contains(this.ValidationInputCssClassName)) {
    // "input" event only takes it back to valid. "Change" event can make it invalid.
    return true;
}
```

This is important UX: users should not see red errors while they are still typing. They see errors only when they move away from a field (or submit), and errors clear immediately as they type a correction.

**Takeaway for our feature:** This is a good pattern. The design doc proposes similar behavior: validate on blur initially, then on input after a field has been shown invalid. The library's approach is slightly different (it uses CSS class presence as the "has been invalid" signal) but achieves the same effect.

### 5.3 Form Submit

```
submit event on <form>
  → preValidate(e)              // calls preventDefault() + stopImmediatePropagation()
  → getFormValidationTask()     // runs ALL input validators in parallel via Promise.all
  → handleValidated(form, success, e)
      → success: re-dispatches submit event (bypasses validation) → form.submit()
      → failure: focusFirstInvalid(form)
```

**The re-dispatch pattern is notable:** After validation succeeds, the library creates a new `SubmitEvent` and dispatches it on the form. If no handler calls `preventDefault()`, it then calls `form.submit()`. It also manually handles `formaction` and submitter `name/value` pairs, since `form.submit()` doesn't propagate submitter info.

**Critical concern for Blazor SSR:** This `form.submit()` approach bypasses Blazor's enhanced navigation. Enhanced nav intercepts `submit` events to send `fetch()` requests. The library's approach of calling `form.submit()` after validation would **skip enhanced navigation entirely**, resulting in a full page reload. Our implementation must integrate with the enhanced navigation pipeline, not around it.

---

## 6. DOM Manipulation for Error Display

### 6.1 Per-Field Messages (`data-valmsg-for`)

The library finds `<span data-valmsg-for="FieldName">` elements and:
- **On error:** Sets `innerHTML = errorMessage`, swaps CSS class from `field-validation-valid` → `field-validation-error`
- **On valid:** Sets `innerHTML = ''`, swaps CSS class from `field-validation-error` → `field-validation-valid`

The `data-valmsg-for` attribute value must match the input's `name` attribute.

### 6.2 Validation Summary (`data-valmsg-summary`)

The library finds `<div data-valmsg-summary="true">` elements and:
- Builds a `<ul><li>...</li></ul>` from all current error messages
- Replaces existing `<ul>` children
- Swaps between `validation-summary-valid` and `validation-summary-errors` CSS classes
- Deduplicates messages (important for checkbox/radio groups)
- Short-circuits rendering if the summary hasn't changed (JSON comparison)

### 6.3 CSS Classes (All Configurable)

| Property | Default Value | MVC Equivalent |
|----------|--------------|----------------|
| `ValidationInputCssClassName` | `input-validation-error` | Same |
| `ValidationInputValidCssClassName` | `input-validation-valid` | Same |
| `ValidationMessageCssClassName` | `field-validation-error` | Same |
| `ValidationMessageValidCssClassName` | `field-validation-valid` | Same |
| `ValidationSummaryCssClassName` | `validation-summary-errors` | Same |
| `ValidationSummaryValidCssClassName` | `validation-summary-valid` | Same |

**Takeaway:** The CSS class names match MVC defaults. For Blazor, we should consider whether to use MVC class names (for cross-framework compatibility) or Blazor's existing class names (`invalid`, `modified`, `validation-message`, `validation-errors`), or make them configurable.

---

## 7. Dynamic DOM Support (MutationObserver)

The library supports watching the DOM for changes via `MutationObserver`:

```typescript
watch(root) {
    this.observer = new MutationObserver(mutations => {
        mutations.forEach(mutation => this.observed(mutation));
    });
    this.observer.observe(root, {
        attributes: true,
        childList: true,
        subtree: true
    });
}
```

It handles:
- **Added nodes:** Scans for new `data-val="true"` inputs and `data-valmsg-for` spans
- **Removed nodes:** Detaches event listeners and cleans up tracking state
- **Attribute changes:** Re-scans if an attribute value changed (e.g., dynamic `data-val-*` modification)
- **Disabled state:** Special handling to reset field validation when disabled

**Relevance to Blazor SSR:** Blazor's enhanced navigation replaces DOM subtrees. The library's MutationObserver approach would work, but the `enhancedload` event is a better fit — it fires once per navigation, after DOM patching is complete, making it more efficient than reacting to every individual mutation.

---

## 8. Extensibility

### 8.1 Custom Providers

```typescript
v.addProvider('classicmovie', (value, element, params) => {
    let year = parseInt(params.year);
    // Custom validation logic...
    return true;
});
```

Providers are registered by name. The first registered provider for a name wins (first-come-first-served), allowing developers to override built-in providers by registering before `bootstrap()`.

### 8.2 Async Providers

Providers can return `Promise<boolean | string>`:
```typescript
v.addProvider('remote', async (value, element, params) => {
    let result = await fetch(params.url);
    return result.ok;
});
```

### 8.3 Overridable Hooks

| Hook | Default Behavior |
|------|-----------------|
| `preValidate(e)` | `preventDefault()` + `stopImmediatePropagation()` |
| `handleValidated(form, success, e)` | Submit if valid, focus first invalid if not |
| `submitValidForm(form, e)` | Re-dispatch submit event, then `form.submit()` |
| `focusFirstInvalid(form)` | Focus first invalid input |
| `highlight(input, ...)` / `unhighlight(input, ...)` | Swap CSS classes |

**Takeaway:** Good extensibility model. The provider pattern is clean and should be adopted. However, the async support adds complexity (Promises, debouncing) that may not be needed for our initial scope (no remote validation). The hook overrides are a good pattern for allowing CSS framework integration.

---

## 9. Mapping to Our Requirements

| Requirement | Library Status | Gap / Applicability |
|-------------|---------------|---------------------|
| **FR-1: Emit `data-val-*` attributes** | N/A (client-only library) | Library *consumes* these attributes; our C# code must *emit* them. The library confirms the protocol is well-defined and stable. |
| **FR-2: Client-side validation before submit** | ✅ Full support | Core functionality. Submit interception, parallel validation, prevent submission on failure. |
| **FR-3: Validation message display** | ✅ `data-valmsg-for` spans + summary | Requires MVC-style placeholder markup (`<span data-valmsg-for="...">`) to exist in the DOM. Blazor's `ValidationMessage<T>` doesn't emit this today. |
| **FR-4: Enhanced navigation compat** | ⚠️ MutationObserver only | MutationObserver would partially work, but `form.submit()` bypasses enhanced nav. The `enhancedload` event pattern would be simpler and more correct. |
| **FR-5: Server validation authoritative** | ✅ Client-only, no server coupling | Clean separation. |
| **FR-6: Opt-in behavior** | ✅ Explicit `bootstrap()` call | Good pattern — no global side effects until initialized. |
| **FR-7: Validation timing** | ✅ Debounced input/change + per-field `data-val-event` | Smart "only clear on input, invalidate on change" behavior is a good UX pattern. |
| **FR-8: No jQuery dependency** | ✅ Zero dependencies | Core selling point. |
| **NFR-1: Payload size** | ✅ ~4 KB gzipped | Excellent reference point. |
| **NFR-3: Accessibility** | ❌ No ARIA support | Library does not set `aria-invalid`, `aria-describedby`, or `aria-live`. This is a significant gap. |
| **NFR-4: Extensibility** | ✅ `addProvider()` + hook overrides | Clean plugin architecture. |
| **NFR-5: MVC convergence** | ✅ Fully MVC-compatible protocol | Uses identical `data-val-*` / `data-valmsg-*` attribute protocol. |

---

## 10. Strengths to Adopt

### 10.1 Protocol Parsing
The two-pass `parseDirectives` algorithm is elegant and protocol-agnostic. Adopt this approach for parsing `data-val-*` attributes.

### 10.2 Provider Pattern
The `(value, element, params) => boolean | string` signature is clean and extensible. Users can return `true`/`false` for pass/fail with the default error message, or return a `string` to override the error message. Adopt this pattern.

### 10.3 Smart Validation Timing
The "input event only clears errors, change event can set errors" behavior is good UX. Adopt this approach.

### 10.4 Null-value Skip Convention
All providers return `true` for empty values, deferring to `required` for presence validation. This is the correct compositional behavior. Adopt this convention.

### 10.5 Configurable CSS Classes
Making CSS class names configurable enables integration with Bootstrap, Tailwind, and other CSS frameworks. Consider this for extensibility.

### 10.6 `formnovalidate` Support
The `shouldValidate` check respects the `formnovalidate` attribute on submit buttons. This is correct behavior per the HTML spec. Adopt this.

### 10.7 Checkbox/Radio Group Handling
The `required` provider correctly handles checkbox and radio groups by checking all elements with the same `name`. Adopt this behavior.

---

## 11. Weaknesses to Avoid

### 11.1 No Constraint Validation API Integration
The library does **not** use `setCustomValidity()`, `checkValidity()`, `ValidityState`, or the `invalid` event. All validation state is managed in the library's own data structures (the `summary` dictionary). This means:
- Third-party libraries can't query element validity via standard APIs
- The `:invalid` / `:valid` CSS pseudo-classes don't reflect validation state
- Screen readers don't get automatic validity announcements

**Our approach:** Use `setCustomValidity()` as the primary validity state mechanism, as proposed in the design doc. This integrates with the browser's built-in validation infrastructure.

### 11.2 No ARIA Support
The library does not set any ARIA attributes. This is a significant accessibility gap:
- No `aria-invalid="true"` on invalid inputs
- No `aria-describedby` linking inputs to their error messages
- No `aria-live` on error message containers

**Our approach:** Must include ARIA support from day one.

### 11.3 innerHTML for Error Messages
The library uses `innerHTML` to set error messages:
```typescript
spans[i].innerHTML = message;
```
This is an XSS risk if error messages contain unsanitized user input. While DataAnnotation messages are typically developer-controlled, this is still a bad practice.

**Our approach:** Use `textContent` instead of `innerHTML`.

### 11.4 GUID-Based Element Tracking
The library generates UUIDs for every DOM element and maintains parallel arrays/dictionaries for lookup. This is:
- Memory-intensive (retains references to removed elements until explicitly cleaned up)
- O(n) for element-to-UID lookup (`elementUIDs.filter(...)`)
- Leak-prone if DOM elements are removed without calling `removeInput`

**Our approach:** Use `WeakMap<Element, State>` for element-to-state mapping. This is O(1) lookup and automatically garbage-collects when elements are removed from the DOM.

### 11.5 `form.submit()` Bypasses Enhanced Navigation
After successful validation, the library calls `form.submit()`, which triggers a native form submission. In Blazor SSR with `data-enhance`, this would bypass the enhanced navigation pipeline entirely, causing a full page reload instead of a fetch-and-patch cycle.

**Our approach:** Must integrate with the enhanced navigation submit interception. Validation should run *before* the enhanced navigation fetch, not as a replacement for it. The correct integration point is to intercept the `submit` event before enhanced navigation does, validate, and only call `preventDefault()` if validation fails.

### 11.6 Promise-Based Async Model
The library's entire validation pipeline is Promise-based, even for synchronous validators. This adds overhead for the common case (synchronous validation). The async support is there for the `remote` provider, which is out of scope for our feature.

**Our approach:** Keep the core synchronous. The design doc does not include remote validation. A synchronous validation loop is simpler, faster, and easier to reason about.

### 11.7 Re-Dispatch Submit Pattern
After validation succeeds, the library creates a new `SubmitEvent` and dispatches it, then falls through to `form.submit()`. This is fragile:
- The new event may be caught by other handlers
- The submitter/formaction handling is manual and error-prone
- It doesn't work with frameworks that intercept submit events (like Blazor enhanced nav)

**Our approach:** Don't re-dispatch. If validation passes, let the original event propagate normally.

---

## 12. Design Implications for Our Feature

### 12.1 What We Should Mirror
1. **`data-val-*` attribute protocol** — Same naming convention as MVC, for ecosystem compatibility
2. **Provider/plugin architecture** — `addProvider(name, callback)` for extensibility
3. **Directive parsing algorithm** — Two-pass parse of `data-val-{rule}` and `data-val-{rule}-{param}`
4. **Smart validation timing** — Validate on blur/change initially, clear on input after first error
5. **Empty-value passthrough** — Let `required` handle presence; all other validators skip empty values
6. **Configurable CSS classes** — Allow integration with various CSS frameworks
7. **`formnovalidate` support** — Respect the HTML standard

### 12.2 What We Should Do Differently
1. **Use Constraint Validation API** — `setCustomValidity()` instead of custom state tracking
2. **ARIA from day one** — `aria-invalid`, `aria-describedby`, `aria-live`
3. **`textContent` not `innerHTML`** — Prevent XSS
4. **`WeakMap` for state tracking** — Instead of GUID arrays
5. **Synchronous validation** — No Promises needed without remote validation
6. **Integrate with enhanced navigation** — Hook into submit flow, don't replace it
7. **`enhancedload` event** — Instead of MutationObserver for DOM change detection
8. **Blazor-native CSS classes** — Use Blazor's existing validation class conventions as default, with MVC classes available via configuration

### 12.3 Protocol Decision: `data-val-*` vs. Custom Approach
The library strongly validates that the `data-val-*` protocol is a proven, well-understood standard in the ASP.NET ecosystem. Adopting it for Blazor SSR means:
- Community libraries (like this one) work out of the box
- MVC-to-Blazor migration is seamless
- The protocol is extensible without code changes (just add `data-val-{newrule}` attributes)

**Recommendation:** Adopt the `data-val-*` protocol.
