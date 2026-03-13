# Blazor Changes for Async Client-Side Validation Compatibility

This document proposes concrete, minimal changes to Blazor's JavaScript infrastructure to enable async client-side validation (e.g., remote validation) while preserving enhanced navigation.

---

## The Problem

After async client-side validation completes, we need to programmatically submit the form in a way that Blazor's enhanced navigation processes correctly. The standard browser APIs don't work:

- **`form.submit()`** — directly submits without firing a SubmitEvent. Enhanced navigation never sees it → falls back to full page reload. Enhanced nav SPA experience is lost.
- **`form.requestSubmit()`** — fires a new SubmitEvent, but in testing it does not reliably trigger enhanced navigation from within a microtask/Promise callback. The re-dispatched event is swallowed before reaching NavigationEnhancement's bubble-phase handler.

The core issue: our validation handler must call `preventDefault()` synchronously (before knowing if validation passes), then re-submit after async validation. But re-submitting in a way that Blazor's enhanced navigation recognizes is not possible with standard browser APIs alone.

---

## Current Architecture

### Three Submit Handlers (in execution order)

| Handler | Phase | File | What It Does |
|---------|-------|------|-------------|
| **Validation EventManager** | Capture | `Validation/EventManager.ts` | Validates form; `preventDefault` if invalid |
| **Blazor EventDelegator** | Capture | `Rendering/Events/EventDelegator.ts` | Dispatches to .NET handlers (interactive mode only — NOT present in SSR) |
| **NavigationEnhancement** | Bubble | `Services/NavigationEnhancement.ts` | Intercepts for enhanced nav fetch-based submit |

### NavigationEnhancement's `onDocumentSubmit`

The function that processes form submissions for enhanced navigation (lines 131-197):

```typescript
function onDocumentSubmit(event: SubmitEvent) {
  if (hasInteractiveRouter() || event.defaultPrevented) { return; }
  const formElem = event.target;
  if (!(formElem instanceof HTMLFormElement)) return;
  if (!enhancedNavigationIsEnabledForForm(formElem)) return;
  // ... method/target checks ...
  event.preventDefault();
  const url = new URL(event.submitter?.getAttribute('formaction') || formElem.action, document.baseURI);
  const formData = new FormData(formElem);
  // ... submitter name/value, enctype handling ...
  performEnhancedPageLoad(url.toString(), false, fetchOptions);
}
```

The key: it calls `performEnhancedPageLoad()` which is an exported async function that does the actual fetch + DOM merge.

### Why `requestSubmit()` Fails

When called from a `.then()` callback after async validation:
1. `requestSubmit()` fires a new SubmitEvent
2. Our capture handler sees `resubmitting=true`, returns early (doesn't prevent)
3. The event should reach NavigationEnhancement in bubble phase...
4. But in testing, it doesn't. The browser appears to suppress the event's default action when dispatched from within a microtask that originated from a prevented submit event.

This behavior was reproduced in headless Chromium and is not explained by the HTML spec. It may be a browser implementation detail or a subtlety of event dispatch timing. Regardless, we cannot rely on `requestSubmit()` from async callbacks.

---

## Proposed Solution: Extract Form Submission Logic

### Core Idea

The form submission logic in `onDocumentSubmit` (URL resolution, FormData collection, submitter handling, enctype branching, `performEnhancedPageLoad` call) is ~35 lines. Extract it into a callable function that our validation library can invoke directly after async validation, bypassing the event system entirely.

### Option A: Export from NavigationEnhancement.ts (Recommended)

Refactor `onDocumentSubmit` to extract the submission logic:

```typescript
// In NavigationEnhancement.ts

/**
 * Submit a form through enhanced navigation without firing a SubmitEvent.
 * Used by the validation library after async validation completes.
 */
export function submitFormViaEnhancedNav(
  form: HTMLFormElement,
  submitter: HTMLElement | null
): void {
  const method = submitter?.getAttribute('formmethod') || form.method;
  const url = new URL(
    submitter?.getAttribute('formaction') || form.action,
    document.baseURI
  );

  const formData = new FormData(form);
  const submitterName = submitter?.getAttribute('name');
  const submitterValue = submitter?.getAttribute('value');
  if (submitterName && submitterValue) {
    formData.append(submitterName, submitterValue);
  }

  const fetchOptions: RequestInit = { method };
  const urlSearchParams = new URLSearchParams(formData as any).toString();

  if (method === 'get') {
    url.search = urlSearchParams;
    history.pushState(null, '', url.toString());
  } else {
    const enctype = submitter?.getAttribute('formenctype') || form.enctype;
    if (enctype === 'multipart/form-data') {
      fetchOptions.body = formData;
    } else {
      fetchOptions.body = urlSearchParams;
      fetchOptions.headers = {
        'content-type': enctype,
        'accept': acceptHeader,
      };
    }
  }

  performEnhancedPageLoad(url.toString(), false, fetchOptions);
}

// Refactored onDocumentSubmit — uses the extracted function
function onDocumentSubmit(event: SubmitEvent) {
  if (hasInteractiveRouter() || event.defaultPrevented) { return; }
  const formElem = event.target;
  if (!(formElem instanceof HTMLFormElement)) return;
  if (!enhancedNavigationIsEnabledForForm(formElem)) return;
  const method = event.submitter?.getAttribute('formmethod') || formElem.method;
  if (method === 'dialog') { /* warn */ return; }
  const target = event.submitter?.getAttribute('formtarget') || formElem.target;
  if (target !== '' && target !== '_self') { /* warn */ return; }
  event.preventDefault();
  submitFormViaEnhancedNav(formElem, event.submitter);
}
```

This is a pure refactoring — `onDocumentSubmit` delegates to `submitFormViaEnhancedNav` after its guard checks.

### Option B: Wire Through `Blazor._internal`

Additionally (or alternatively), expose via the Blazor global:

```typescript
// In GlobalExports.ts
import { submitFormViaEnhancedNav } from './Services/NavigationEnhancement';

_internal: {
  navigationManager: {
    ...navigationManagerInternalFunctions,
    submitEnhancedForm: submitFormViaEnhancedNav,
  },
}
```

This enables the validation library (when loaded as a standalone `<script>`) to call `Blazor._internal.navigationManager.submitEnhancedForm(form, submitter)` without needing a direct module import.

### Option C: Both (Recommended)

Use the direct import when the validation code is bundled with Blazor's JS (inside `blazor.web.js`). Use the `_internal` API when loaded as a standalone script (`aspnet-validation.js`). The standalone script detects the API at runtime:

```typescript
// In EventManager.ts
private submitAfterValidation(form: HTMLFormElement, submitter: HTMLElement | null): void {
  const blazor = (window as any).Blazor;
  const enhancedSubmit = blazor?._internal?.navigationManager?.submitEnhancedForm;

  if (enhancedSubmit && form.hasAttribute('data-enhance')) {
    enhancedSubmit(form, submitter);
  } else {
    form.submit();
  }
}
```

---

## Lines of Code

| File | Change | Lines |
|------|--------|-------|
| `NavigationEnhancement.ts` | Extract `submitFormViaEnhancedNav`, refactor `onDocumentSubmit` | ~5 net new (logic moved, not duplicated) |
| `GlobalExports.ts` | Import + add to `_internal.navigationManager` | ~3 |
| `EventManager.ts` | Add `submitAfterValidation` helper, use in `.then()` | ~10 |
| **Total** | | **~18 net new lines** |

---

## What This Enables

```
User clicks Submit on a Blazor SSR form
  → Capture phase: validation handler runs
  → preventDefault() + stopPropagation()
  → validateForm() runs all providers (sync resolve instantly via await)
  → .then(isValid => ...)
  → If invalid: errors displayed, form stays
  → If valid + data-enhance: submitFormViaEnhancedNav(form, submitter)
    → Builds FormData from form
    → Calls performEnhancedPageLoad(url, false, fetchOptions)
    → Fetch request sent with form data
    → Server processes POST, returns updated HTML
    → DOM merged (enhanced load)
    → enhancedload event fires
    → Validation library re-scans DOM
  → If valid + no data-enhance (MVC): form.submit()
```

This is the **exact same server-side flow** as a normal enhanced nav form submission. The only difference is the entry point: `submitFormViaEnhancedNav(form, submitter)` instead of the `onDocumentSubmit` event handler.

---

## Risk Assessment

| Concern | Assessment |
|---------|-----------|
| Breaking existing forms | **None** — `onDocumentSubmit` is refactored to use the extracted function; behavior identical |
| New public API surface | **Minimal** — `_internal` is not a public contract |
| Bundle size | **~0** — logic is moved, not duplicated |
| Testing | Existing enhanced nav tests cover the extracted logic; add one test for programmatic call |
| Future compatibility | If `onDocumentSubmit` logic changes, `submitFormViaEnhancedNav` changes with it (same file) |

---

## Comparison of All Approaches

| Approach | Enhanced Nav | Async Providers | Code Change | Reliability |
|----------|-------------|-----------------|-------------|-------------|
| `form.submit()` | ❌ Bypassed | ✅ | None | High |
| `requestSubmit()` | ❓ Unreliable | ✅ | None | Low |
| Sync-first + async fallback | ✅ Sync only | ❌ Blazor | ~60 lines | High |
| **`submitFormViaEnhancedNav`** | **✅ Full** | **✅ Full** | **~18 lines** | **High** |
