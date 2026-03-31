# Implementation Plan — Validation Timing and Per-Field Overrides

Changes to the JS prototype to match MVC's jQuery validation defaults and add `data-val-event` support.

## Changes Overview

Three behavior changes + one new feature:

1. **Skip hidden fields** — DomScanner should not register fields that are not visible
2. **Track form "submitted" state** — after first submit, enable eager input validation
3. **Full validate on input (not clear-only)** — when input validation is active, run full validation (can show/replace errors, not just clear)
4. **`data-val-event` per-field override** — respect custom event configuration per field

## File-by-File Changes

### 1. `DomScanner.ts` — Skip hidden fields

Add a visibility check when scanning elements. Hidden means not visible to the user (zero offsetWidth/offsetHeight and no client rects), matching jQuery's `:hidden` behavior.

```typescript
// In scan(), after getting the element and before parseDirectives:
if (this.isHidden(input)) {
  continue;
}

// New private method:
private isHidden(element: HTMLElement): boolean {
  return !(element.offsetWidth || element.offsetHeight || element.getClientRects().length);
}
```

**Note:** This check runs at scan time. Elements that become hidden later (e.g., wizard step change) should be re-scanned. The `enhancedload` re-scan handles this for Blazor. For MVC, `parse()` can be called manually.

Also: during `validateFormSync` and `validateForm`, skip hidden elements at validation time too (not just scan time), so dynamically hidden fields are excluded from submit validation.

### 2. `EventManager.ts` — Track form "submitted" state

Add a `Set<HTMLFormElement>` to track which forms have been submitted at least once. After successful or failed submit validation, add the form to this set. On reset, remove it.

```typescript
export class EventManager {
  private submitHandler: ((e: SubmitEvent) => void) | null = null;
  private resubmitting = false;
  private submittedForms = new WeakSet<HTMLFormElement>();  // NEW

  // In attachSubmitInterception, after validateFormSync runs (both valid and invalid paths):
  // this.submittedForms.add(form);

  // Expose for input handler to check:
  isFormSubmitted(form: HTMLFormElement): boolean {
    return this.submittedForms.has(form);
  }

  // In reset handler:
  // this.submittedForms.delete(form);
}
```

### 3. `EventManager.ts` — Change input handler to full validate

The `inputHandler` currently only clears errors. Change it to run full validation when the form has been submitted OR the field is currently invalid (matching jQuery's `onkeyup` logic).

**Before:**
```typescript
const inputHandler = () => {
  if (state.hasBeenInvalid && state.currentError) {
    this.coordinator.validateAndUpdate(element).then(() => {
      const form = element.closest('form');
      if (form) { this.coordinator.updateFormSummary(form); }
    });
  }
};
```

**After:**
```typescript
const inputHandler = () => {
  const form = element.closest('form');
  // Validate on input if: form was submitted OR field is currently invalid
  const shouldValidate = state.currentError || 
    (form instanceof HTMLFormElement && this.isFormSubmitted(form));
  if (shouldValidate) {
    this.coordinator.validateAndUpdate(element).then(() => {
      if (form instanceof HTMLFormElement) {
        this.coordinator.updateFormSummary(form);
      }
    });
  }
};
```

This matches jQuery's `onkeyup` behavior: `element.name in this.submitted || element.name in this.invalid`.

### 4. `EventManager.ts` + `DomScanner.ts` — `data-val-event` support

Read the `data-val-event` attribute on each element during `attachInputListeners`. Use it to override which events trigger validation.

**In `attachInputListeners`:**

```typescript
attachInputListeners(element: ValidatableElement): void {
  const state = this.coordinator.getState(element);
  if (!state) { return; }

  const customEvent = element.getAttribute('data-val-event');
  
  // "none" = submit-only, no real-time validation
  if (customEvent === 'none') {
    return;
  }

  const events = customEvent 
    ? customEvent.split(/\s+/)              // Custom: e.g., "change" or "input change"
    : (element instanceof HTMLSelectElement 
        ? ['change']                         // Default for <select>
        : ['input', 'change']);              // Default for text inputs

  for (const eventName of events) {
    const handler = eventName === 'input' ? inputHandler : changeHandler;
    element.addEventListener(eventName, handler);
    state.listeners.push({ event: eventName, handler });
  }
}
```

The `inputHandler` and `changeHandler` remain as defined above — `inputHandler` has the submitted/invalid gate, `changeHandler` always validates.

### 5. `ValidationCoordinator.ts` — Skip hidden fields during form validation

In `validateFormSync` and `validateForm`, add a visibility check to skip hidden elements:

```typescript
// In validateFormSync and validateForm, when iterating inputs:
for (const input of inputs) {
  if (this.isHidden(input)) {
    // Clear any existing error state on hidden fields
    const state = this.elementState.get(input);
    if (state?.currentError) {
      this.markValid(input, state);
    }
    continue;
  }
  // ... existing validation logic
}

private isHidden(element: HTMLElement): boolean {
  return !(element.offsetWidth || element.offsetHeight || element.getClientRects().length);
}
```

### 6. `EventManager.ts` — Form reset clears submitted state

In the reset handler (to be added), clear the submitted tracking:

```typescript
attachResetInterception(): void {
  document.addEventListener('reset', (event) => {
    const form = event.target;
    if (form instanceof HTMLFormElement && form.querySelector('[data-val="true"]')) {
      this.submittedForms.delete(form);
      setTimeout(() => this.coordinator.clearForm(form), 0);
    }
  }, true);
}
```

## Summary of Changes

| File | Change | Lines (est.) |
|---|---|---|
| `DomScanner.ts` | Add `isHidden()`, skip hidden in scan | ~8 |
| `EventManager.ts` | `submittedForms` WeakSet + tracking | ~10 |
| `EventManager.ts` | Input handler: full validate (not clear-only) | ~5 (changed) |
| `EventManager.ts` | `data-val-event` support in `attachInputListeners` | ~15 |
| `EventManager.ts` | Reset handler + submitted state clear | ~10 |
| `ValidationCoordinator.ts` | Skip hidden in `validateFormSync`/`validateForm`, add `clearForm` | ~20 |
| **Total** | | **~68 lines** |

## Test Plan

### Timing behavior tests (update existing, add new):
- Pristine field: typing does NOT trigger validation
- Pristine field: blur triggers validation, shows error
- After submit: typing triggers full validation (shows/clears/replaces errors)
- After reset: typing does NOT trigger validation (back to pristine)
- Invalid field: typing triggers full validation (can replace error, not just clear)

### Hidden field tests (new):
- Hidden field (`display: none`) is skipped during scan
- Hidden field is skipped during form submit validation
- Field that becomes visible after scan is picked up on re-scan

### `data-val-event` tests (new):
- `data-val-event="change"`: no validation on input, validates on change/blur
- `data-val-event="none"`: no real-time validation, only on submit
- `data-val-event="input change"`: explicit default behavior
- No `data-val-event`: default behavior (input + change for text, change for select)

### Reset tests (new):
- Reset clears CSS classes, messages, summary
- Reset clears submitted state (typing goes back to pristine behavior)
- Reset clears ARIA attributes
