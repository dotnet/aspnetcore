# Client-Side Validation JS Library — Prototype Implementation Plan

**Issue:** [dotnet/aspnetcore#51040](https://github.com/dotnet/aspnetcore/issues/51040)
**Based on:** Prior art analysis of [aspnet-client-validation](https://github.com/haacked/aspnet-client-validation) (`02-prior-art.md`)
**Scope:** Prototype of the JavaScript validation engine (Layers 1–3 from the design doc)

---

## 1. Goals & Constraints

### 1.1 Prototype Goals

1. **Functional validation**: Parse `data-val-*` attributes from the DOM, validate on blur/change/submit, prevent submission when invalid
2. **Constraint Validation API**: Use `setCustomValidity()` as the primary validity state mechanism — enables `:invalid`/`:valid` CSS pseudo-classes and screen reader integration
3. **Enhanced navigation compatibility**: Survive DOM patching; re-scan on `enhancedload` event
4. **Extensible provider architecture**: Allow registration of custom validation rules via `addProvider(name, callback)`
5. **ARIA-ready design**: Architecture must accommodate ARIA attributes (`aria-invalid`, `aria-describedby`, `aria-live`) without requiring them in the prototype
6. **MVC protocol compatibility**: Use the same `data-val-*` / `data-valmsg-*` attribute protocol as MVC unobtrusive validation

### 1.2 Constraints

| Constraint | Rationale |
|------------|-----------|
| **Synchronous only** | No async/remote validation — simplifies the pipeline, no Promises needed |
| **No jQuery dependency** | Web standards only — Constraint Validation API, native DOM APIs |
| **No framework coupling** | Core engine must be host-agnostic (Blazor wiring is a separate layer) |
| **`textContent` only** | Never use `innerHTML` for error messages — XSS prevention |
| **`WeakMap` for state** | Instead of GUID-based tracking — O(1) lookup, auto-GC on element removal |

### 1.3 Out of Scope for Prototype

- ARIA attribute emission (design for it, don't implement)
- Remote/async validation
- Compare (`equalto`), CreditCard, Phone, FileExtensions providers
- Custom element (`<asp-validation-message>`)
- Validation message localization infrastructure
- Source generator integration (C# side)
- Full test suite (manual testing with BlazorSSR sample app)

---

## 2. Architecture

### 2.1 Three-Layer Stack

```
┌─────────────────────────────────────────────────────┐
│  Layer 3: Blazor Wiring                             │
│  - Initializes on DOMContentLoaded                  │
│  - Re-scans on enhancedload event                   │
│  - Sets novalidate on forms                         │
│  - Idempotent (no double-binding)                   │
├─────────────────────────────────────────────────────┤
│  Layer 2: Unobtrusive Adapter                       │
│  - Parses data-val-* attributes into directives     │
│  - Binds event listeners (submit, input, change)    │
│  - Manages validation timing (blur→input→submit)    │
│  - Updates error display (messages + summary)       │
│  - Manages CSS classes on inputs and messages       │
├─────────────────────────────────────────────────────┤
│  Layer 1: Core Validation Engine                    │
│  - Provider registry: { name → ValidationProvider } │
│  - Validates a value against a provider + params    │
│  - Uses setCustomValidity() for validity state      │
│  - Built-in providers for standard rules            │
└─────────────────────────────────────────────────────┘
```

### 2.2 Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| State tracking | `WeakMap<Element, State>` | Auto-GC, O(1) lookup, no GUID arrays |
| Validity state | `setCustomValidity()` | Web standard, `:invalid` CSS, screen readers |
| Error display | `textContent` on `data-valmsg-for` spans | XSS-safe, MVC-compatible protocol |
| Submit interception | Capture-phase `submit` listener | Runs before enhanced nav's bubbling listener |
| DOM change detection | `enhancedload` event + explicit `scan()` | More efficient than MutationObserver for Blazor |
| Validation timing | Blur-first, then input-on-correction | Smart UX: no red-while-typing |
| Empty value handling | All providers skip empty values | `required` handles presence; others handle format |

### 2.3 ARIA-Ready Design Points

The architecture must allow ARIA attributes to be added later without structural changes. These are the extension points:

| ARIA Attribute | Where It Applies | Design Implication |
|----------------|-----------------|-------------------|
| `aria-invalid="true"` | On invalid input elements | `setValidity()` method must have a hook point after setting custom validity |
| `aria-describedby="{id}"` | On input, pointing to error message | Error message elements need stable `id` attributes; input tracking must store the message element reference |
| `aria-live="polite"` | On error message containers | Message containers need to be identifiable at scan time |

**Concrete design requirement:** The validation state update path (`markInvalid` / `markValid`) must be methods on a class (not inline code) so ARIA logic can be added later as method extensions or overrides.

---

## 3. Module Design

### 3.1 Module 1: Provider Registry (`providers.ts`)

The provider registry is the core of the validation engine. It maps rule names to validation functions.

#### Types

```typescript
/**
 * A validation provider function.
 * @param value - The trimmed input value (empty string if no value)
 * @param element - The input/select/textarea element being validated
 * @param params - Parameters parsed from data-val-{rule}-{param} attributes
 * @returns true if valid, or a string error message if invalid
 */
type ValidationProvider = (
    value: string,
    element: ValidatableElement,
    params: Record<string, string>
) => true | string;

type ValidatableElement = HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement;
```

**Design note:** The return type is `true | string` (not `boolean | string`). Returning `true` means valid. Returning a string means invalid with that string as the error message. This is adopted from aspnet-client-validation — it allows providers to override the default error message (from `data-val-{rule}` attribute) with a computed one.

#### Provider Registration

```typescript
class ValidationEngine {
    private providers: Map<string, ValidationProvider> = new Map();

    addProvider(name: string, provider: ValidationProvider): void {
        // First registration wins (allows overriding built-ins by registering before init)
        if (!this.providers.has(name)) {
            this.providers.set(name, provider);
        }
    }

    // Force-set a provider (for overrides after initialization)
    setProvider(name: string, provider: ValidationProvider): void {
        this.providers.set(name, provider);
    }
}
```

### 3.2 Module 2: Built-in Providers (`built-in-providers.ts`)

Each provider implements the `ValidationProvider` signature. All providers **return `true` for empty values** — the `required` provider handles presence validation.

| Provider Name | `data-val-*` Attributes | Validation Logic |
|---------------|------------------------|------------------|
| `required` | `data-val-required="{msg}"` | Trim value, check non-empty. For checkboxes: check `checked`. For radio groups: check any in group is checked. |
| `length` | `data-val-length="{msg}"`, `-min`, `-max` | `value.length` against min (default 0) and max (default ∞) |
| `minlength` | `data-val-minlength="{msg}"`, `-min` | `value.length >= min` |
| `maxlength` | `data-val-maxlength="{msg}"`, `-max` | `value.length <= max` |
| `range` | `data-val-range="{msg}"`, `-min`, `-max` | `parseFloat(value)` then compare against min and max |
| `regex` | `data-val-regex="{msg}"`, `-pattern` | `new RegExp(pattern).test(value)` |
| `email` | `data-val-email="{msg}"` | RFC-compliant regex (same as aspnet-client-validation) |
| `url` | `data-val-url="{msg}"` | Check for valid URL format (`http://`, `https://`, `ftp://` prefix) |

#### Provider Implementation Pattern

```typescript
function registerBuiltInProviders(engine: ValidationEngine): void {
    engine.addProvider('required', (value, element, params) => {
        // Checkbox: must be checked
        if (element instanceof HTMLInputElement && element.type === 'checkbox') {
            return element.checked ? true : params[''] || 'This field is required.';
        }
        // Radio: at least one in group must be checked
        if (element instanceof HTMLInputElement && element.type === 'radio') {
            const form = element.closest('form');
            if (form) {
                const radios = form.querySelectorAll<HTMLInputElement>(
                    `input[type="radio"][name="${element.name}"]`
                );
                const anyChecked = Array.from(radios).some(r => r.checked);
                return anyChecked ? true : params[''] || 'This field is required.';
            }
        }
        // Text: trim and check non-empty
        return value.trim().length > 0 ? true : params[''] || 'This field is required.';
    });

    engine.addProvider('range', (value, element, params) => {
        if (!value) return true; // Skip empty — let 'required' handle it
        const num = parseFloat(value);
        if (isNaN(num)) return params[''] || 'Please enter a valid number.';
        const min = parseFloat(params['min']);
        const max = parseFloat(params['max']);
        if (!isNaN(min) && num < min) return params[''] || `Value must be at least ${min}.`;
        if (!isNaN(max) && num > max) return params[''] || `Value must be at most ${max}.`;
        return true;
    });

    // ... similar for length, minlength, maxlength, regex, email, url
}
```

### 3.3 Module 3: Directive Parser (`directive-parser.ts`)

Adopted from aspnet-client-validation's two-pass algorithm. Parses `data-val-*` attributes into a structured representation.

#### Types

```typescript
interface ValidationDirective {
    /** The rule name (e.g., "required", "range") */
    rule: string;
    /** The error message from data-val-{rule} attribute */
    message: string;
    /** Parameters from data-val-{rule}-{param} attributes */
    params: Record<string, string>;
}
```

#### Algorithm

```typescript
function parseDirectives(element: ValidatableElement): ValidationDirective[] {
    // Pass 1: Collect all data-val-* attributes into a flat map
    const attrs: Record<string, string> = {};
    for (const attr of element.attributes) {
        if (attr.name.startsWith('data-val-')) {
            const key = attr.name.substring('data-val-'.length); // e.g., "required", "range-min"
            attrs[key] = attr.value;
        }
    }

    // Pass 2: Group into directives
    const directives: ValidationDirective[] = [];
    for (const key of Object.keys(attrs)) {
        if (key.includes('-')) continue; // Skip parameters (handled below)

        const rule = key;
        const message = attrs[rule];
        const params: Record<string, string> = { '': message }; // '' key = default error message

        // Collect parameters: data-val-{rule}-{param}
        for (const paramKey of Object.keys(attrs)) {
            if (paramKey.startsWith(rule + '-')) {
                const paramName = paramKey.substring(rule.length + 1);
                params[paramName] = attrs[paramKey];
            }
        }

        directives.push({ rule, message, params });
    }

    return directives;
}
```

### 3.4 Module 4: Validation Coordinator (`coordinator.ts`)

The coordinator ties together providers, directives, and DOM elements. It manages per-element state and orchestrates validation.

#### Per-Element State

```typescript
interface ElementState {
    directives: ValidationDirective[];
    /** Whether this field has ever been shown as invalid (for input-event gating) */
    hasBeenInvalid: boolean;
    /** The currently displayed error message (empty = valid) */
    currentError: string;
    /** Reference to the message display element(s) for this input */
    messageElements: Element[];
    /** Event listeners attached to this element (for cleanup) */
    listeners: { event: string; handler: EventListener }[];
}
```

**State storage:** `WeakMap<ValidatableElement, ElementState>` — when elements are GC'd, state is automatically cleaned up.

#### Core Validation Loop

```typescript
class ValidationCoordinator {
    private engine: ValidationEngine;
    private elementState: WeakMap<ValidatableElement, ElementState> = new WeakMap();
    private managedForms: WeakSet<HTMLFormElement> = new WeakSet();

    /**
     * Validate a single element against all its directives.
     * Returns the first error message, or empty string if valid.
     */
    validateElement(element: ValidatableElement): string {
        const state = this.elementState.get(element);
        if (!state) return '';

        const value = this.getElementValue(element);

        for (const directive of state.directives) {
            const provider = this.engine.getProvider(directive.rule);
            if (!provider) continue;

            const result = provider(value, element, directive.params);
            if (result !== true) {
                // Provider returned an error string
                const errorMessage = typeof result === 'string' ? result : directive.message;
                return errorMessage;
            }
        }

        return ''; // All providers passed
    }

    /**
     * Validate and update the validity state + error display for an element.
     */
    validateAndUpdate(element: ValidatableElement): boolean {
        const error = this.validateElement(element);
        if (error) {
            this.markInvalid(element, error);
            return false;
        } else {
            this.markValid(element);
            return true;
        }
    }

    /**
     * Validate all tracked inputs within a form.
     * Returns true if all inputs are valid.
     */
    validateForm(form: HTMLFormElement): boolean {
        const inputs = form.querySelectorAll<ValidatableElement>(
            'input[data-val="true"], select[data-val="true"], textarea[data-val="true"]'
        );
        let allValid = true;
        let firstInvalid: ValidatableElement | null = null;

        for (const input of inputs) {
            if (!this.validateAndUpdate(input)) {
                allValid = false;
                if (!firstInvalid) firstInvalid = input;
            }
        }

        // Focus first invalid field for accessibility
        if (firstInvalid) {
            firstInvalid.focus();
        }

        this.updateSummary(form);
        return allValid;
    }
}
```

#### Validity State Management (Constraint Validation API)

```typescript
class ValidationCoordinator {
    /**
     * Mark an element as invalid.
     * Uses setCustomValidity() for Constraint Validation API integration.
     * 
     * ARIA extension point: Add aria-invalid="true" and aria-describedby here.
     */
    private markInvalid(element: ValidatableElement, message: string): void {
        const state = this.elementState.get(element);
        if (!state) return;

        // Constraint Validation API
        element.setCustomValidity(message);

        // Track that this field has been shown invalid (for input-event gating)
        state.hasBeenInvalid = true;
        state.currentError = message;

        // CSS classes
        element.classList.add('input-validation-error');
        element.classList.remove('input-validation-valid');

        // Update per-field message display
        for (const msgEl of state.messageElements) {
            msgEl.textContent = message;
            msgEl.classList.add('field-validation-error');
            msgEl.classList.remove('field-validation-valid');
        }

        // ARIA extension point (not implemented in prototype):
        // element.setAttribute('aria-invalid', 'true');
        // element.setAttribute('aria-describedby', msgEl.id);
    }

    /**
     * Mark an element as valid.
     * Clears setCustomValidity() and CSS classes.
     *
     * ARIA extension point: Remove aria-invalid here.
     */
    private markValid(element: ValidatableElement): void {
        const state = this.elementState.get(element);
        if (!state) return;

        // Constraint Validation API
        element.setCustomValidity('');

        state.currentError = '';

        // CSS classes
        element.classList.remove('input-validation-error');
        element.classList.add('input-validation-valid');

        // Clear per-field message display
        for (const msgEl of state.messageElements) {
            msgEl.textContent = '';
            msgEl.classList.remove('field-validation-error');
            msgEl.classList.add('field-validation-valid');
        }

        // ARIA extension point (not implemented in prototype):
        // element.removeAttribute('aria-invalid');
    }
}
```

### 3.5 Module 5: Event Management (`events.ts`)

#### Submit Interception

**Critical integration point with enhanced navigation.** The validation submit handler must run **before** Blazor's enhanced navigation handler.

```
Event flow for <form data-enhance>:

  User clicks Submit
       │
       ▼
  ┌──────────────────────────────────────────────────┐
  │ CAPTURE PHASE (top → target)                     │
  │                                                  │
  │  Our validation handler (capture: true)           │
  │  → Validates all fields                          │
  │  → If invalid: preventDefault() + stopPropagation│
  │  → If valid: let event propagate                 │
  └──────────────────────────────────────────────────┘
       │ (only if valid)
       ▼
  ┌──────────────────────────────────────────────────┐
  │ BUBBLE PHASE (target → top)                      │
  │                                                  │
  │  Blazor enhanced nav handler (no capture flag)   │
  │  → Checks data-enhance, event.defaultPrevented   │
  │  → Sends fetch() request                         │
  │  → Patches DOM with response                     │
  └──────────────────────────────────────────────────┘
```

```typescript
class EventManager {
    private submitHandler: ((e: SubmitEvent) => void) | null = null;

    attachSubmitInterception(coordinator: ValidationCoordinator): void {
        this.submitHandler = (event: SubmitEvent) => {
            const form = event.target as HTMLFormElement;

            // Only intercept forms that have validation-tracked inputs
            if (!form || !form.querySelector('[data-val="true"]')) return;

            // Respect formnovalidate on the submit button (HTML spec)
            const submitter = event.submitter;
            if (submitter?.hasAttribute('formnovalidate')) return;

            // Validate the form
            const isValid = coordinator.validateForm(form);

            if (!isValid) {
                // Block submission — prevents enhanced nav from running
                event.preventDefault();
                event.stopPropagation();
            }
            // If valid: do nothing — let the event propagate to enhanced nav
        };

        // CAPTURE PHASE: runs before Blazor's bubbling-phase handler
        document.addEventListener('submit', this.submitHandler, true);
    }

    detachSubmitInterception(): void {
        if (this.submitHandler) {
            document.removeEventListener('submit', this.submitHandler, true);
            this.submitHandler = null;
        }
    }
}
```

#### Input/Change Event Handling (Smart Timing)

Adopted from aspnet-client-validation's "input clears, change sets" pattern:

```typescript
class EventManager {
    attachInputListeners(
        element: ValidatableElement,
        coordinator: ValidationCoordinator
    ): void {
        const state = coordinator.getState(element);
        if (!state) return;

        const inputHandler = () => {
            // On 'input' event: only CLEAR existing errors (don't set new ones)
            // This prevents "red while typing" UX
            if (state.hasBeenInvalid && state.currentError) {
                coordinator.validateAndUpdate(element);
            }
        };

        const changeHandler = () => {
            // On 'change'/'blur' event: full validation (can set new errors)
            coordinator.validateAndUpdate(element);
            coordinator.updateSummary(element.closest('form'));
        };

        // Attach listeners based on element type
        if (element instanceof HTMLSelectElement) {
            element.addEventListener('change', changeHandler);
            state.listeners.push({ event: 'change', handler: changeHandler });
        } else {
            element.addEventListener('input', inputHandler);
            element.addEventListener('change', changeHandler);
            state.listeners.push(
                { event: 'input', handler: inputHandler },
                { event: 'change', handler: changeHandler }
            );
        }
    }
}
```

**Design note:** No debounce in the prototype. The `input` handler only runs when `hasBeenInvalid` is true (meaning the field was previously shown invalid), so it only fires validation when clearing an error. This is already efficient — the validation loop is synchronous and operates on a single element.

### 3.6 Module 6: Error Display (`display.ts`)

#### Per-Field Messages

Targets `<span data-valmsg-for="FieldName">` elements — the same protocol as MVC.

```typescript
class ErrorDisplay {
    /**
     * Find message elements for a given input.
     * Uses the input's `name` attribute to match data-valmsg-for values.
     */
    findMessageElements(
        input: ValidatableElement,
        form: HTMLFormElement
    ): Element[] {
        const name = input.getAttribute('name');
        if (!name) return [];

        return Array.from(
            form.querySelectorAll(`[data-valmsg-for="${CSS.escape(name)}"]`)
        );
    }

    /**
     * Update the validation summary container for a form.
     * Targets <div data-valmsg-summary="true"> elements.
     */
    updateSummary(form: HTMLFormElement, errors: Map<string, string>): void {
        const summary = form.querySelector('[data-valmsg-summary="true"]');
        if (!summary) return;

        // Build error list
        const ul = summary.querySelector('ul') || document.createElement('ul');
        ul.textContent = ''; // Clear existing — using textContent, not innerHTML

        if (errors.size > 0) {
            // Deduplicate error messages
            const uniqueMessages = new Set(errors.values());
            for (const message of uniqueMessages) {
                const li = document.createElement('li');
                li.textContent = message;
                ul.appendChild(li);
            }
            summary.classList.add('validation-summary-errors');
            summary.classList.remove('validation-summary-valid');
        } else {
            summary.classList.remove('validation-summary-errors');
            summary.classList.add('validation-summary-valid');
        }

        if (!ul.parentElement) {
            summary.appendChild(ul);
        }
    }
}
```

#### CSS Class Configuration

CSS classes should be configurable but default to MVC-compatible values, since we are using the MVC `data-val-*` / `data-valmsg-*` protocol:

```typescript
interface CssClassConfig {
    inputError: string;       // default: 'input-validation-error'
    inputValid: string;       // default: 'input-validation-valid'
    messageError: string;     // default: 'field-validation-error'
    messageValid: string;     // default: 'field-validation-valid'
    summaryError: string;     // default: 'validation-summary-errors'
    summaryValid: string;     // default: 'validation-summary-valid'
}
```

**Open question for later:** Whether to also support Blazor's existing CSS classes (`invalid`, `modified`, `validation-message`, `validation-errors`) as an alternative preset. For the prototype, use MVC defaults since they match the `data-val-*` protocol.

### 3.7 Module 7: DOM Scanner (`scanner.ts`)

Scans a DOM subtree for validatable elements and sets up tracking.

```typescript
class DomScanner {
    /**
     * Scan a root element (or document) for validatable inputs and wire them up.
     * Idempotent — skips elements already tracked.
     */
    scan(
        root: ParentNode,
        coordinator: ValidationCoordinator,
        eventManager: EventManager
    ): void {
        // Find all inputs with data-val="true"
        const inputs = root.querySelectorAll<ValidatableElement>(
            'input[data-val="true"], select[data-val="true"], textarea[data-val="true"]'
        );

        for (const input of inputs) {
            // Idempotency: skip if already tracked
            if (coordinator.hasState(input)) continue;

            // Parse directives from data-val-* attributes
            const directives = parseDirectives(input);
            if (directives.length === 0) continue;

            // Find the parent form
            const form = input.closest('form');
            if (!form) continue;

            // Find message display elements
            const messageElements = this.display.findMessageElements(input, form);

            // Register element state
            coordinator.registerElement(input, {
                directives,
                hasBeenInvalid: false,
                currentError: '',
                messageElements,
                listeners: []
            });

            // Attach input/change event listeners
            eventManager.attachInputListeners(input, coordinator);

            // Set novalidate on the form (suppress native browser tooltips)
            // We handle validation ourselves via setCustomValidity()
            if (!form.hasAttribute('novalidate')) {
                form.setAttribute('novalidate', '');
            }

            // Track the form for submit interception
            coordinator.trackForm(form);
        }
    }

    /**
     * Remove tracking and event listeners for all elements within a root.
     * Called before re-scanning after DOM updates, or for cleanup.
     */
    remove(root: ParentNode, coordinator: ValidationCoordinator): void {
        const inputs = root.querySelectorAll<ValidatableElement>(
            'input[data-val="true"], select[data-val="true"], textarea[data-val="true"]'
        );

        for (const input of inputs) {
            coordinator.unregisterElement(input);
        }
    }
}
```

### 3.8 Module 8: Blazor Integration Layer (`blazor-wiring.ts`)

This is the Blazor-specific entry point. It initializes validation on page load and re-initializes on enhanced navigation.

```typescript
/**
 * Initialize client-side validation for Blazor SSR forms.
 *
 * Usage: Call once, after blazor.web.js has loaded.
 * Handles initial page load + enhanced navigation lifecycle.
 */
function initializeBlazorValidation(options?: Partial<CssClassConfig>): void {
    const engine = new ValidationEngine();
    registerBuiltInProviders(engine);

    const display = new ErrorDisplay(options);
    const coordinator = new ValidationCoordinator(engine, display);
    const eventManager = new EventManager();
    const scanner = new DomScanner(display);

    // Attach document-level submit interception (capture phase)
    eventManager.attachSubmitInterception(coordinator);

    // Initial page scan
    scanner.scan(document, coordinator, eventManager);

    // Enhanced navigation: re-scan after DOM patches
    // The enhancedload event fires after synchronizeDomContent() completes
    // (Boot.Web.ts line 63, via JSEventRegistry)
    if (typeof Blazor !== 'undefined' && Blazor.addEventListener) {
        Blazor.addEventListener('enhancedload', () => {
            // Re-scan the entire document for new/changed forms
            // Idempotent: already-tracked elements are skipped
            scanner.scan(document, coordinator, eventManager);
        });
    }

    // Expose public API for extensibility
    (window as any).__aspnetValidation = {
        addProvider: (name: string, provider: ValidationProvider) => {
            engine.addProvider(name, provider);
        },
        scan: (root?: ParentNode) => {
            scanner.scan(root || document, coordinator, eventManager);
        },
        validateForm: (form: HTMLFormElement) => {
            return coordinator.validateForm(form);
        },
        validateField: (input: ValidatableElement) => {
            return coordinator.validateAndUpdate(input);
        }
    };
}
```

#### Enhanced Navigation Compatibility Details

| Scenario | Behavior |
|----------|----------|
| **Initial page load** | `scan(document)` called after DOMContentLoaded |
| **Enhanced link navigation** | `enhancedload` fires → `scan(document)` re-scans; new forms are found; kept elements are skipped (idempotent) |
| **Enhanced form submission (valid)** | Validation passes → event propagates → enhanced nav fetches → response patched → `enhancedload` fires → re-scan picks up updated DOM |
| **Enhanced form submission (invalid)** | Validation fails → `preventDefault()` + `stopPropagation()` → enhanced nav never runs → user sees errors |
| **Streaming rendering** | Streaming updates DOM incrementally; call `scan()` after streaming completes (or listen for `enhancedload` which fires after streaming too) |
| **DOM patching preserves elements** | DomSync's `Operation.Keep` preserves element references → WeakMap state survives → event listeners survive → no re-binding needed |
| **DOM patching replaces elements** | DomSync's `Operation.Insert`/`Delete` creates new elements → old WeakMap entries auto-GC → new elements found by re-scan |

#### `novalidate` Management

The scanner sets `novalidate` on forms that contain `data-val="true"` inputs. This is necessary because:

1. We use `setCustomValidity()` to set validation state
2. Without `novalidate`, the browser would show native tooltip UI when `checkValidity()` returns false
3. We want to show our own error messages in `data-valmsg-for` elements instead
4. The enhanced nav handler doesn't call `checkValidity()`, so `novalidate` doesn't affect the submission flow

---

## 4. MVC Compatibility Analysis

### 4.1 Can MVC Use This Library?

**Yes, with caveats.** The library is designed to be host-agnostic (Layer 1 + Layer 2). MVC could use it as a replacement for `jquery.validate.unobtrusive.js`:

| Aspect | Compatibility |
|--------|--------------|
| **`data-val-*` attribute protocol** | ✅ Fully compatible — same naming convention |
| **`data-valmsg-for` message spans** | ✅ Fully compatible — same protocol |
| **`data-valmsg-summary` containers** | ✅ Fully compatible — same protocol |
| **CSS class names** | ✅ Same defaults as MVC |
| **Provider extensibility** | ✅ `addProvider()` replaces jQuery Validate's `$.validator.addMethod()` |
| **Submit interception** | ⚠️ Capture-phase listener — MVC doesn't use enhanced nav, so this works but the approach is different from jQuery Validate |
| **`formnovalidate` support** | ✅ Same behavior |

### 4.2 What MVC Would Need Beyond the Prototype

1. **Additional providers**: `equalto` (Compare), `creditcard`, `phone`, `fileextensions`, `remote`
2. **`remote` validation (async)**: Requires the async pipeline we're deliberately excluding from the prototype
3. **MVC-specific initialization**: MVC doesn't have `enhancedload` — it would use `DOMContentLoaded` only, or a MutationObserver for partial-view scenarios (AJAX partials)
4. **jQuery Validate parity**: Some edge cases in jQuery Validate's behavior may not be replicated (e.g., `$.validator.unobtrusive.parse()` for dynamically loaded content)

### 4.3 Recommendation

Build the prototype with MVC compatibility in mind (same protocol, same CSS defaults, host-agnostic core), but **do not invest in MVC-specific wiring for the prototype**. MVC integration can be a follow-up effort that adds:
- An MVC wiring layer (parallel to the Blazor wiring layer)
- The missing providers (equalto, creditcard, phone, fileextensions)
- Async provider support (for remote validation)

---

## 5. File Structure

```
src/Components/Web.JS/src/Validation/
├── index.ts                    # Public API exports
├── ValidationEngine.ts         # Provider registry + core validation logic
├── BuiltInProviders.ts         # required, length, range, regex, email, url
├── DirectiveParser.ts          # Two-pass data-val-* attribute parser
├── ValidationCoordinator.ts    # Per-element state, validate + mark valid/invalid
├── EventManager.ts             # Submit interception, input/change handlers
├── ErrorDisplay.ts             # Message spans, validation summary, CSS classes
├── DomScanner.ts               # Scan/remove, idempotent element discovery
├── BlazorWiring.ts             # enhancedload integration, initialization
└── Types.ts                    # Shared types (ValidatableElement, etc.)
```

**Integration point:** `BlazorWiring.ts` will be imported and called from the existing Blazor Web.JS boot pipeline or loaded as a standalone script. For the prototype, we will load it as a standalone `<script>` in the BlazorSSR sample app, keeping it decoupled from `blazor.web.js` initially.

---

## 6. Implementation Order

### Phase 1: Core Engine (foundation)

| # | Task | Dependencies |
|---|------|-------------|
| 1 | `Types.ts` — Shared type definitions | None |
| 2 | `ValidationEngine.ts` — Provider registry, `addProvider()`, `getProvider()` | Types |
| 3 | `BuiltInProviders.ts` — All 8 providers | ValidationEngine |
| 4 | `DirectiveParser.ts` — Two-pass `parseDirectives()` | Types |

### Phase 2: Coordination & Display

| # | Task | Dependencies |
|---|------|-------------|
| 5 | `ErrorDisplay.ts` — `findMessageElements()`, `updateSummary()`, CSS class config | Types |
| 6 | `ValidationCoordinator.ts` — `WeakMap` state, `markInvalid`/`markValid` with `setCustomValidity()`, `validateElement()`, `validateForm()` | Engine, Parser, Display |

### Phase 3: Event Wiring & DOM

| # | Task | Dependencies |
|---|------|-------------|
| 7 | `EventManager.ts` — Capture-phase submit interception, input/change handlers with smart timing | Coordinator |
| 8 | `DomScanner.ts` — Idempotent scan/remove, `novalidate` management | Parser, Coordinator, EventManager, Display |

### Phase 4: Integration & Testing

| # | Task | Dependencies |
|---|------|-------------|
| 9 | `BlazorWiring.ts` — `initializeBlazorValidation()`, `enhancedload` hook, public API | All modules |
| 10 | `index.ts` — Public exports | BlazorWiring |
| 11 | Build setup — TypeScript compilation, bundle as standalone JS | index.ts |
| 12 | BlazorSSR sample integration — Create `ContactManual.razor` (copy of `Contact.razor` with hardcoded `data-val-*` attributes and `data-valmsg-for` spans), add route and nav link, load validation script. Keep original `Contact.razor` unchanged for comparison. | Build |
| 13 | Manual testing — Test all validation scenarios with the sample app | Sample integration |

### Phase 5: Verification

| # | Task | Dependencies |
|---|------|-------------|
| 14 | Test: Required field validation (empty submit, fill and re-submit) | Sample running |
| 15 | Test: StringLength, MinLength, MaxLength validation | Sample running |
| 16 | Test: Range validation (numeric bounds) | Sample running |
| 17 | Test: RegularExpression validation | Sample running |
| 18 | Test: Email, URL validation | Sample running |
| 19 | Test: Enhanced navigation — navigate away and back, re-submit | Sample running |
| 20 | Test: `formnovalidate` button bypasses validation | Sample running |
| 21 | Test: Validation timing — errors show on blur, clear on input | Sample running |
| 22 | Test: `setCustomValidity()` — inspect validity state via devtools | Sample running |

---

## 7. Sample App Modifications

For the prototype, we will create a **new page `ContactManual.razor`** — a copy of `Contact.razor` with manually hardcoded `data-val-*` attributes. The original `Contact.razor` is kept unchanged as a baseline for comparison (no client-side validation, server round-trip only).

### File Layout

```
src/Components/Samples/BlazorSSR/Pages/
├── Index.razor              # Home page — updated with links to both forms
├── Contact.razor            # UNCHANGED — original server-only validation
└── ContactManual.razor      # NEW — copy with data-val-* attrs for prototype testing
```

### ContactManual.razor — Input Example (manually added data-val-*)

```html
@page "/contact-manual"

<!-- Same form structure as Contact.razor, but with data-val-* attributes -->
<InputText @bind-Value="Model.Name" id="Name"
    data-val="true"
    data-val-required="The Name field is required."
    data-val-length="The field Name must be a string with a minimum length of 2 and a maximum length of 100."
    data-val-length-min="2"
    data-val-length-max="100" />
<span data-valmsg-for="Name" class="field-validation-valid"></span>
```

### Validation Summary Container (in ContactManual.razor)

```html
<div data-valmsg-summary="true" class="validation-summary-valid">
    <ul></ul>
</div>
```

### Script Loading (in App.razor)

```html
<!-- After blazor.web.js -->
<script src="js/aspnet-validation.js"></script>
<script>
    // Initialize after Blazor is ready
    document.addEventListener('DOMContentLoaded', () => {
        __aspnetValidation.init();
    });
</script>
```

### Comparison Strategy

| Page | Route | Client Validation | Server Validation | Purpose |
|------|-------|-------------------|-------------------|---------|
| `Contact.razor` | `/contact` | ❌ None | ✅ Full | Baseline — current behavior |
| `ContactManual.razor` | `/contact-manual` | ✅ Prototype | ✅ Full | Prototype — client-side validation demo |

This allows side-by-side comparison of the UX with and without client-side validation.

---

## 8. Risk Analysis

| Risk | Impact | Mitigation |
|------|--------|------------|
| Capture-phase submit handler conflicts with other libraries | Medium | Only intercept forms with `data-val="true"` inputs; respect `event.defaultPrevented` |
| `setCustomValidity()` triggers native tooltips despite `novalidate` | Low | `novalidate` prevents `reportValidity()` tooltips; `checkValidity()` does not show tooltips. Verified in browser spec. |
| DOM patching creates new elements, losing WeakMap state | Low | Re-scan on `enhancedload` picks up new elements; old state auto-GCs |
| `data-val-*` attribute naming conflicts with MVC's existing system | Low | We intentionally use the same protocol; conflict would only arise if both MVC and Blazor validation run on the same page (unlikely in practice) |
| Synchronous validation blocks UI on large forms | Low | DataAnnotation checks are computationally trivial; hundreds of fields would still validate in <1ms |
| Enhanced nav handler reads form data before our capture handler can modify it | None | Enhanced nav listens on bubble phase; our capture handler runs first and can `preventDefault()` |

---

## 9. Success Criteria

The prototype is complete when:

1. ✅ All 8 providers (required, length, minlength, maxlength, range, regex, email, url) work correctly
2. ✅ Submitting an invalid form shows errors and prevents submission (no server round-trip)
3. ✅ Submitting a valid form proceeds normally (enhanced nav fetch works)
4. ✅ Error messages appear in `data-valmsg-for` spans and `data-valmsg-summary` container
5. ✅ `setCustomValidity()` is set on invalid elements (verifiable via `element.validationMessage` in devtools)
6. ✅ Validation timing works: errors appear on blur/change, clear on input
7. ✅ Enhanced navigation: navigate away and back, form is re-scanned and validation works
8. ✅ `formnovalidate` button bypasses client validation
9. ✅ No jQuery or other dependencies
10. ✅ Architecture has clear extension points for ARIA (comments in code showing where)
