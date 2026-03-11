# Blazor Async Validation Compatibility — Architecture Options

## The Problem

Our validation library needs to support async providers (e.g., remote validation) while working with Blazor's enhanced navigation. The fundamental tension:

- **Async validation** requires `preventDefault()` in the submit handler (must decide synchronously), then re-submitting after validation completes.
- **Blazor's EventDelegator** intercepts submit events in capture phase, always calls `preventDefault()` for events with .NET handlers, and only routes to .NET handlers when the event originates from its tracked event flow. Synthetic events from `requestSubmit()` are intercepted but **not routed to .NET**.
- **Blazor's enhanced nav** (SSR mode) listens in bubble phase and checks `event.defaultPrevented` — if EventDelegator already prevented it, enhanced nav never sees it.

The current sync-first approach works for the 99% case (sync providers), but async providers in Blazor require a different strategy.

---

## How Blazor Processes Form Submissions

### Two Independent Systems

1. **Interactive Mode (EventDelegator):**
   - Capture phase on `document`
   - Always `preventDefault()` for submit events
   - Walks `composedPath()` to find .NET handlers
   - Dispatches to .NET via `dispatchEvent()` → `DispatchEventAsync()`
   - For submit (non-bubbling event), only dispatches to the target element

2. **SSR Enhanced Mode (NavigationEnhancement):**
   - Bubble phase on `document`
   - Checks `event.defaultPrevented` — exits if true
   - Checks `enhancedNavigationIsEnabledForForm()` — requires `data-enhance` attribute
   - Builds `FormData`, sends via `fetch()`, merges response into DOM

### Why `requestSubmit()` Fails

When our library calls `requestSubmit()` after async validation:

1. New SubmitEvent fires
2. EventDelegator (capture) intercepts it, calls `preventDefault()`, walks `composedPath()`
3. EventDelegator finds the .NET handler (from `EditForm`'s `OnValidSubmit`), dispatches to .NET
4. BUT: the dispatch expects the event to carry the correct rendering context. The synthetic event from `requestSubmit()` does carry `event.target` pointing to the form, so `composedPath()` is correct
5. The real issue: EventDelegator calls `preventDefault()` on the synthetic event. Enhanced nav (bubble phase) sees `defaultPrevented=true` and exits. Meanwhile, EventDelegator dispatches to .NET but...

Actually, based on the code analysis, EventDelegator DOES dispatch the synthetic event to .NET. The `dispatchEvent()` call sends `eventHandlerId`, `eventName`, and `eventFieldInfo` to the server. The `EventFieldInfo.fromEvent()` reads `event.target`'s value, which should be correct since the form fields haven't changed.

**The actual failure mode needs more investigation** — it's possible the dispatch works but the .NET handler doesn't fire because the component state or form handler mapping is stale after enhanced nav.

---

## Architecture Options for Always-Async

### Option A: Replicate Enhanced Nav's Form Submission Logic

Instead of `requestSubmit()`, our library could directly call Blazor's enhanced navigation fetch logic. We'd replicate what `onDocumentSubmit` does:

```typescript
// After async validation passes:
async function submitFormViaEnhancedNav(form: HTMLFormElement, submitter: HTMLElement | null) {
  const url = new URL(
    submitter?.getAttribute('formaction') || form.action,
    document.baseURI
  );
  const method = submitter?.getAttribute('formmethod') || form.method;
  const formData = new FormData(form);

  if (submitter) {
    const name = submitter.getAttribute('name');
    const value = submitter.getAttribute('value');
    if (name && value) formData.append(name, value);
  }

  const body = new URLSearchParams(formData as any).toString();

  // Call Blazor's internal enhanced page load
  // This is the key: bypass the event system entirely
  await performEnhancedPageLoad(url.toString(), false, {
    method,
    body,
    headers: { 'content-type': 'application/x-www-form-urlencoded', 'accept': 'text/html' }
  });
}
```

**Problem:** `performEnhancedPageLoad` is not a public API. We'd need to either:
- Access it via `Blazor._internal` (fragile, unsupported)
- Get the Blazor team to expose a public API for programmatic form submission
- Import it directly if our library is bundled with Blazor's JS

**Viability:** High if we own the Blazor JS bundle (we do — this is the aspnetcore repo). We can import `performEnhancedPageLoad` directly from `NavigationEnhancement.ts`.

**Pros:**
- No synthetic events, no EventDelegator interaction
- Exact same fetch logic as enhanced nav
- Works for both SSR and could be adapted for interactive

**Cons:**
- Couples our library to Blazor internals (but we're in the same repo)
- MVC mode still needs `requestSubmit()` (no Blazor)
- Need to detect which mode we're in

### Option B: Export a Blazor API for Programmatic Form Submission

Add a public API to Blazor:

```typescript
// New API on the Blazor object
Blazor.submitForm(form: HTMLFormElement, submitter?: HTMLElement): Promise<void>
```

This would:
1. Build FormData from the form
2. Call `performEnhancedPageLoad` in SSR mode
3. Dispatch to EventDelegator in interactive mode
4. Handle all the edge cases (method, enctype, formaction, submitter name/value)

Our validation library would then:
```typescript
// After async validation passes:
if (typeof Blazor !== 'undefined' && Blazor.submitForm) {
  await Blazor.submitForm(form, submitter);
} else {
  form.requestSubmit(submitter); // MVC fallback
}
```

**Viability:** High — this is the cleanest solution. It's a small addition to the Blazor public API.

**Pros:**
- Clean separation of concerns
- Works for any third-party validation library, not just ours
- Future-proof: if Blazor changes its submit internals, the API contract holds

**Cons:**
- Requires a Blazor API change (but we control Blazor)
- Need to handle both SSR and interactive modes in the implementation

### Option C: Use a Custom DOM Event as a Communication Channel

Instead of `requestSubmit()`, dispatch a custom event that Blazor's enhanced nav can listen for:

```typescript
// After async validation passes:
const submitEvent = new CustomEvent('blazor:validated-submit', {
  detail: { form, submitter },
  bubbles: true,
  cancelable: false
});
form.dispatchEvent(submitEvent);
```

Enhanced nav would listen for this custom event and process it like a form submit:

```typescript
// In NavigationEnhancement.ts
document.addEventListener('blazor:validated-submit', (event: CustomEvent) => {
  const { form, submitter } = event.detail;
  // Same logic as onDocumentSubmit but without the defaultPrevented check
  // ...
});
```

**Viability:** Medium — requires changes to NavigationEnhancement.ts.

**Pros:**
- No synthetic submit events, no EventDelegator interference
- Clean event-based communication
- Custom event is not in `nonBubblingEvents` or `alwaysPreventDefaultEvents`

**Cons:**
- Only works for SSR enhanced nav, not interactive mode
- Requires coordination between two separate systems via event naming convention
- Less discoverable than a public API

### Option D: Modify EventDelegator to Support Re-submitted Events

Add a mechanism for EventDelegator to recognize and pass through re-submitted events:

```typescript
// In our validation library:
const resubmitMarker = new WeakSet<Event>();

// After async validation:
const event = new SubmitEvent('submit', { submitter, bubbles: true, cancelable: true });
resubmitMarker.add(event);
form.dispatchEvent(event);

// In EventDelegator's globalListener:
if (resubmitMarker.has(evt)) {
  // Skip preventDefault, let the event propagate to enhanced nav
  return;
}
```

**Problem:** WeakSet can't be shared between separately bundled modules. Would need a DOM-based marker like a data attribute on the event or a global flag.

Alternative: Use a `data-validation-passed` attribute on the form:

```typescript
// After async validation:
form.setAttribute('data-validation-passed', '');
form.requestSubmit(submitter);

// In EventDelegator: check for the attribute and skip preventDefault
// In our handler: remove the attribute after processing
```

**Viability:** Low — requires modifying EventDelegator, which is Blazor core code. The attribute approach is fragile.

### Option E: Integrate Validation into Blazor's Event Pipeline

Make validation a first-class part of Blazor's event processing. Instead of a separate validation library intercepting submit events, validation would be a step in EventDelegator's submit processing:

```typescript
// In EventDelegator, before dispatching submit to .NET:
if (browserEvent.type === 'submit') {
  const form = browserEvent.target as HTMLFormElement;
  const validationResult = await window.__aspnetValidation?.validateForm(form);
  if (validationResult === false) {
    // Don't dispatch to .NET
    return;
  }
}
```

**Viability:** Low for prototype, but worth considering for the final product.

**Pros:**
- No event interception conflicts
- Validation runs at the right point in the pipeline
- Works for both SSR and interactive

**Cons:**
- Requires modifying EventDelegator (core Blazor code)
- Makes validation synchronous from EventDelegator's perspective (or requires making EventDelegator async)
- Tight coupling between Blazor's event system and the validation library

---

## Recommended Approach: Option B (Public Blazor API)

### Why Option B

1. **Cleanest architecture** — validation library doesn't need to know Blazor internals
2. **Works for all modes** — SSR enhanced, interactive, and MVC (with fallback)
3. **Benefits the ecosystem** — any validation library (not just ours) can use it
4. **Small surface area** — one method on the Blazor object
5. **We control both sides** — we own the Blazor JS and the validation library

### Proposed API

```typescript
// On the Blazor global object
interface BlazorSubmitApi {
  /**
   * Programmatically submit a form, bypassing the native submit event.
   * In SSR enhanced mode: builds FormData and uses enhanced page load.
   * In interactive mode: dispatches to the .NET form handler.
   * Returns a Promise that resolves when the submission is processed.
   */
  submitForm(form: HTMLFormElement, submitter?: HTMLElement | null): Promise<void>;
}
```

### Implementation Sketch

```typescript
// In NavigationEnhancement.ts or a new FormSubmission.ts
export function submitFormProgrammatically(
  form: HTMLFormElement,
  submitter: HTMLElement | null
): Promise<void> {
  if (hasInteractiveRouter()) {
    // Interactive mode: dispatch to EventDelegator's .NET handler
    // Create a trusted-like submit flow
    return submitFormInteractive(form, submitter);
  }

  if (enhancedNavigationIsEnabledForForm(form)) {
    // SSR enhanced mode: use enhanced page load
    return submitFormEnhanced(form, submitter);
  }

  // Fallback: native form submission
  form.requestSubmit(submitter);
  return Promise.resolve();
}

async function submitFormEnhanced(
  form: HTMLFormElement,
  submitter: HTMLElement | null
): Promise<void> {
  const url = new URL(
    submitter?.getAttribute('formaction') || form.action,
    document.baseURI
  );
  const method = submitter?.getAttribute('formmethod') || form.method;
  const formData = new FormData(form);

  if (submitter) {
    const name = submitter.getAttribute('name');
    const value = submitter.getAttribute('value');
    if (name && value) formData.append(name, value);
  }

  const body = new URLSearchParams(formData as any).toString();
  const fetchOptions: RequestInit = {
    method,
    body: method !== 'get' ? body : undefined,
    headers: method !== 'get'
      ? { 'content-type': 'application/x-www-form-urlencoded' }
      : undefined,
  };

  if (method === 'get') {
    url.search = body;
    history.pushState(null, '', url.toString());
  }

  await performEnhancedPageLoad(url.toString(), false, fetchOptions);
}
```

### Validation Library Integration

```typescript
// In EventManager.ts — submit handler
attachSubmitInterception(): void {
  this.submitHandler = (event: SubmitEvent) => {
    const form = event.target as HTMLFormElement;
    // ... guards ...

    // Try sync validation first
    const syncResult = this.coordinator.validateFormSync(form);

    if (syncResult === true) {
      return; // Let the event through
    }

    if (syncResult === false) {
      event.preventDefault();
      event.stopPropagation();
      return;
    }

    // Async path needed
    event.preventDefault();
    event.stopPropagation();

    const submitter = event.submitter;
    this.coordinator.validateForm(form).then(isValid => {
      if (isValid) {
        this.submitFormAfterValidation(form, submitter);
      }
    });
  };
}

private submitFormAfterValidation(
  form: HTMLFormElement,
  submitter: HTMLElement | null
): void {
  // Use Blazor's API if available (SSR enhanced or interactive)
  const blazor = (window as any).Blazor;
  if (blazor?.submitForm) {
    blazor.submitForm(form, submitter);
    return;
  }

  // MVC/plain HTML fallback
  this.resubmitting = true;
  form.requestSubmit(submitter as HTMLElement | undefined);
  this.resubmitting = false;
}
```

### What This Enables

With Option B, the always-async path would be:

1. **Sync providers only (99% case):** `validateFormSync()` returns `true` → event passes through → Blazor/MVC handles normally. **No `requestSubmit` needed.**

2. **Async providers (remote validation):** `validateFormSync()` returns `'async'` → `preventDefault()` → async validation → `Blazor.submitForm()` (Blazor) or `requestSubmit()` (MVC). **Blazor API bypasses the event system entirely.**

This is the always-async architecture with Blazor compatibility. The sync-first optimization is a performance shortcut, not a fundamental design constraint.

---

## Alternative: Option A (Direct Import) — Quick Prototype Path

If we want a working prototype without adding a public Blazor API, we can import `performEnhancedPageLoad` directly since our validation library is in the same repository and could be bundled with Blazor's JS.

However, for the standalone `aspnet-validation.js` bundle (used by MVC and loaded separately), this wouldn't work. The standalone bundle can't import Blazor internals.

**Hybrid approach:** The validation code that ships inside `blazor.web.js` could use the direct import. The standalone `aspnet-validation.js` would use `requestSubmit()` for MVC.

---

## Summary

| Option | Effort | Coupling | Works for SSR | Works for Interactive | Works for MVC |
|--------|--------|----------|--------------|----------------------|---------------|
| **A: Replicate fetch** | Medium | High (internals) | ✅ | ❌ | N/A (requestSubmit) |
| **B: Blazor.submitForm API** | Medium | Low (public API) | ✅ | ✅ | ✅ (requestSubmit fallback) |
| **C: Custom DOM event** | Medium | Medium (event naming) | ✅ | ❌ | N/A |
| **D: Modify EventDelegator** | High | Very High | ✅ | ✅ | N/A |
| **E: Validation in pipeline** | High | Very High | ✅ | ✅ | N/A |

**Recommendation:** Option B (`Blazor.submitForm` API) for the final product. Current sync-first approach works well as a prototype; Option B can be added as a separate follow-up without changing the validation library's architecture.
