# Validation Library Packaging Plan

## Goal

Structure the validation code so that:

1. **Blazor bundle** (`blazor.web.js`) includes only the sync validation core + Blazor-specific validators. No async path, no remote validator, no `number` validator. The code is tree-shaken to minimum size.
2. **Standalone bundle** (`aspnet-core-validation.js`) includes everything: core + all validators + async path + remote validator + `number` validator + MVC-specific initialization.
3. Both bundles share the same core source files — the difference is only in which entry point imports what.

## Current File Structure

```
src/Validation/
  index.ts              ← Standalone entry point (DOMContentLoaded auto-init)
  ValidationSetup.ts    ← Creates engine + scanner + events, exports public API
  ValidationEngine.ts   ← Core: tracking, validation loop, error state
  DomScanner.ts         ← DOM scanning + reconciliation
  EventManager.ts       ← Submit/reset interception, input listeners
  ErrorDisplay.ts       ← CSS classes, message elements, summary, ARIA
  Validator.ts          ← Types: ValidatableElement, ValidationContext, ValidatorRegistry
  Utils.ts              ← shouldSkipElement, getElementForm
  BuiltInValidators.ts  ← All built-in validators registered in one function
```

## Proposed File Structure

```
src/Validation/
  # Core (shared, tree-shakable)
  ValidationEngine.ts       ← unchanged
  DomScanner.ts              ← unchanged
  EventManager.ts            ← unchanged
  ErrorDisplay.ts            ← unchanged
  Validator.ts               ← unchanged
  Utils.ts                   ← unchanged

  # Validators (each importable independently)
  validators/
    Required.ts
    StringLength.ts
    Range.ts
    Regex.ts
    Email.ts
    Url.ts
    Phone.ts
    CreditCard.ts
    EqualTo.ts
    FileExtensions.ts
    Number.ts              ← MVC only
    Remote.ts              ← MVC only (future)

  # Registration functions (tree-shaking entry points)
  BlazorValidators.ts      ← registers only Blazor-relevant validators
  StandaloneValidators.ts  ← registers all validators including MVC-specific

  # Entry points (one per bundle)
  BlazorValidation.ts      ← Blazor entry: called from Boot.Web.ts
  index.ts                 ← Standalone entry: self-initializing MVC bundle
```

## Split: BlazorValidators vs StandaloneValidators

**BlazorValidators.ts:**
```typescript
import { ValidatorRegistry } from './Validator';
import { requiredValidator } from './validators/Required';
import { stringLengthValidator } from './validators/StringLength';
import { rangeValidator } from './validators/Range';
import { regexValidator } from './validators/Regex';
import { emailValidator } from './validators/Email';
import { urlValidator } from './validators/Url';
import { phoneValidator } from './validators/Phone';
import { creditcardValidator } from './validators/CreditCard';
import { equaltoValidator } from './validators/EqualTo';
import { fileextensionsValidator } from './validators/FileExtensions';

export function registerBlazorValidators(registry: ValidatorRegistry): void {
  registry.set('required', requiredValidator);
  registry.set('length', stringLengthValidator);
  registry.set('minlength', stringLengthValidator);
  registry.set('maxlength', stringLengthValidator);
  registry.set('range', rangeValidator);
  registry.set('regex', regexValidator);
  registry.set('email', emailValidator);
  registry.set('url', urlValidator);
  registry.set('phone', phoneValidator);
  registry.set('creditcard', creditcardValidator);
  registry.set('equalto', equaltoValidator);
  registry.set('fileextensions', fileextensionsValidator);
}
```

**StandaloneValidators.ts:**
```typescript
import { ValidatorRegistry } from './Validator';
import { registerBlazorValidators } from './BlazorValidators';
import { numberValidator } from './validators/Number';
// import { remoteValidator } from './validators/Remote'; // future

export function registerStandaloneValidators(registry: ValidatorRegistry): void {
  registerBlazorValidators(registry);
  registry.set('number', numberValidator);
  // registry.set('remote', remoteValidator); // future
}
```

Because `registerStandaloneValidators` imports `numberValidator` (and eventually `remoteValidator`), those modules are pulled into the standalone bundle. Because `registerBlazorValidators` does NOT import them, they are tree-shaken out of the Blazor bundle.

## Blazor Entry Point: BlazorValidation.ts

This file is imported from `Boot.Web.ts`. It does NOT self-initialize — the boot file controls when it runs.

```typescript
import { registerBlazorValidators } from './BlazorValidators';
import { DomScanner } from './DomScanner';
import { ErrorDisplay } from './ErrorDisplay';
import { EventManager } from './EventManager';
import { ValidationEngine } from './ValidationEngine';
import { ValidatableElement, Validator, ValidatorRegistry } from './Validator';

export interface BlazorValidationService {
  addValidator(name: string, validator: Validator): void;
  scan(elementOrSelector?: ParentNode | string): void;
  validateField(element: ValidatableElement): boolean;
  validateForm(form: HTMLFormElement): boolean;
}

export function createBlazorValidation(): BlazorValidationService {
  const registry = new ValidatorRegistry();
  registerBlazorValidators(registry);

  const errorDisplay = new ErrorDisplay();
  const engine = new ValidationEngine(registry, errorDisplay);
  const eventManager = new EventManager(engine);
  const scanner = new DomScanner(engine, eventManager);

  eventManager.attachFormInterceptors();
  scanner.scan(document);

  return {
    addValidator: (name, validator) => registry.set(name, validator),
    scan: (elementOrSelector?) => scanner.scan(elementOrSelector),
    validateField: (element) => engine.validateElement(element),
    validateForm: (form) => engine.validateForm(form),
  };
}
```

Key differences from standalone:
- Factory function (`createBlazorValidation`) instead of self-initializing
- Returns the service object — the caller (Boot.Web.ts) assigns it to `Blazor.validation`
- No `window.__aspnetValidation` global
- Uses `registerBlazorValidators` (no number/remote)

## Boot.Web.ts Integration

Add validation initialization to `Boot.Web.ts`:

```typescript
import { createBlazorValidation, BlazorValidationService } from './Validation/BlazorValidation';

// In the boot() function, after DOMContentLoaded setup:

// Initialize validation and expose on Blazor global
const validation = createBlazorValidation();
Blazor.validation = validation;

// Re-scan after enhanced navigation updates the DOM
jsEventRegistry.addEventListener('enhancedload', () => {
  validation.scan();
});
```

This hooks into the existing `enhancedload` event that fires after every enhanced navigation DOM update (dispatched from the `documentUpdated` callback in Boot.Web.ts line 63).

## GlobalExports.ts Changes

Add `validation` to the `IBlazor` interface:

```typescript
// In the public API section:
validation?: BlazorValidationService;
```

This is optional (`?`) because it's only set after boot completes.

## Standalone Entry Point: index.ts (unchanged pattern)

```typescript
import { initializeStandaloneValidation } from './ValidationSetup';

function initialize(): void {
  initializeStandaloneValidation();
}

if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', initialize);
} else {
  initialize();
}
```

**ValidationSetup.ts** changes to use `registerStandaloneValidators`:

```typescript
import { registerStandaloneValidators } from './StandaloneValidators';

export function initializeStandaloneValidation(): void {
  const registry = new ValidatorRegistry();
  registerStandaloneValidators(registry);  // ← includes number, remote
  // ... rest unchanged
}
```

## Tree-Shaking Analysis

### blazor.web.js bundle

```
Boot.Web.ts
  └─ BlazorValidation.ts
       └─ BlazorValidators.ts
            ├─ validators/Required.ts
            ├─ validators/StringLength.ts
            ├─ validators/Range.ts
            ├─ validators/Regex.ts
            ├─ validators/Email.ts
            ├─ validators/Url.ts
            ├─ validators/Phone.ts
            ├─ validators/CreditCard.ts
            ├─ validators/EqualTo.ts
            └─ validators/FileExtensions.ts
       ├─ ValidationEngine.ts
       ├─ DomScanner.ts
       ├─ EventManager.ts
       ├─ ErrorDisplay.ts
       ├─ Validator.ts
       └─ Utils.ts
```

**NOT included** (tree-shaken out):
- `validators/Number.ts`
- `validators/Remote.ts`
- `StandaloneValidators.ts`
- `ValidationSetup.ts`
- `index.ts`

### aspnet-core-validation.js bundle

```
index.ts
  └─ ValidationSetup.ts
       └─ StandaloneValidators.ts
            └─ BlazorValidators.ts (all Blazor validators)
            ├─ validators/Number.ts
            └─ validators/Remote.ts (future)
       ├─ ValidationEngine.ts
       ├─ DomScanner.ts
       ├─ EventManager.ts
       ├─ ErrorDisplay.ts
       ├─ Validator.ts
       └─ Utils.ts
```

**Includes everything.**

## Rollup Config

No changes needed — the two entry points are already registered:

```javascript
inputOutputMap: {
  'blazor.web': './src/Boot.Web.ts',           // includes BlazorValidation
  'aspnet-core-validation': './src/Validation/index.ts',  // standalone
}
```

Rollup with `treeshake: 'smallest'` handles the rest.

## Future: Async Validation Path

When async validation is added for MVC (remote validator), the core `ValidationEngine.validateElementInternal` 
needs a sync path (Blazor) and an async path (standalone). Design options:

**Option A — Separate engine methods:**
- `validateElement()` remains sync (used by Blazor)
- `validateElementAsync()` added (used by standalone when async validators are present)
- Standalone `EventManager` calls async version; Blazor `EventManager` calls sync version

**Option B — Engine is always sync, async is a wrapper:**
- Core engine stays sync
- Async validators are wrapped at the registration layer — they set a pending state, 
  fire a fetch, and call back into the engine to update the error when the response arrives
- This keeps the core sync and avoids branching in ValidationEngine

Option B is recommended — it keeps the core simple and tree-shakable. The async wrapper code
lives in `validators/Remote.ts` and is only imported by the standalone bundle.

## Implementation Order

1. **Split BuiltInValidators.ts** into individual validator files under `validators/`
2. **Create BlazorValidators.ts** and **StandaloneValidators.ts** registration files
3. **Create BlazorValidation.ts** entry point (factory function)
4. **Update ValidationSetup.ts** to use `registerStandaloneValidators`
5. **Update index.ts** if needed (should be minimal)
6. **Integrate into Boot.Web.ts** — import, create, assign to `Blazor.validation`, hook `enhancedload`
7. **Update GlobalExports.ts** — add `validation` to `IBlazor`
8. **Verify tree-shaking** — build both bundles, check that `blazor.web.js` does not contain number/remote code
9. **Update tests** — adjust imports for split validator files
