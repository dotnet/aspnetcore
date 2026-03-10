# Client-Side Form Validation for Blazor SSR — Progress Log

**Issue:** [dotnet/aspnetcore#51040](https://github.com/dotnet/aspnetcore/issues/51040)
**Branch:** `oroztocil/validation-client-side`

---

## Step 1: Research & Requirements

**Commit:** `2c675efa07`

Conducted comprehensive research and wrote `features/validation-client-side/01-research.md` covering:

- **GitHub issue analysis**: Read #51040 thread (18 comments), the design doc by @javiercn (#issuecomment-3706000376), and related issue #28640.
- **MVC unobtrusive validation architecture**: Studied the full pipeline from DataAnnotations → `IClientModelValidatorProvider` → adapter classes (`RequiredAttributeAdapter`, `RangeAttributeAdapter`, etc.) → `data-val-*` HTML attributes → `jquery.validate.unobtrusive.js`. Key code in `src/Mvc/Mvc.DataAnnotations/src/`, `src/Mvc/Mvc.TagHelpers/src/`, `src/Mvc/Mvc.ViewFeatures/src/`.
- **Blazor forms & validation architecture**: Studied `EditForm`, `EditContext`, `InputBase<T>`, `DataAnnotationsValidator`, `ValidationMessage<T>`, `ValidationSummary`, SSR form mapping (`FormMappingContext`, `FormMappingValidator`), and the enhanced navigation JS layer (`NavigationEnhancement.ts`, `DomSync.ts`, `Boot.Web.ts`).
- **Community & prior art**: Reviewed Phil Haack's [aspnet-client-validation](https://github.com/haacked/aspnet-client-validation) (~4 KB gzip, jQuery-free), Damian Edwards' [WasmClientSideValidation](https://github.com/DamianEdwards/WasmClientSideValidation) experiment, and community workaround attempts (SmartInput component by @sweeperq).
- **Web standards**: Documented the Constraint Validation API (`setCustomValidity`, `ValidityState`, `checkValidity`, `novalidate`) — universal browser support, zero dependencies.
- **Requirements**: Defined 8 functional requirements (attribute emission, client-side validation, message display, enhanced nav compatibility, opt-in behavior, etc.), 6 non-functional requirements (payload size, accessibility, extensibility, MVC convergence), explicit out-of-scope items, and 8 open design questions.

## Step 2: BlazorSSR Sample App

**Commits:** `2c675efa07`, `8ddac031ef`

Created a new sample app at `src/Components/Samples/BlazorSSR/` — a pure static SSR Blazor app with **no interactive render modes** (no `AddInteractiveServerComponents`, no WebAssembly).

### Structure

```
src/Components/Samples/BlazorSSR/
├── App.razor                    # Root component (HTML shell, blazor.web.js)
├── BlazorSSR.csproj             # Web SDK project, no interactivity references
├── Layout/
│   └── MainLayout.razor         # Minimal layout
├── Models/
│   └── ContactModel.cs          # Form model with DataAnnotations
├── Pages/
│   ├── Index.razor              # Home page with link to contact form
│   └── Contact.razor            # Form page with EditForm + validation
├── Program.cs                   # AddRazorComponents() / MapRazorComponents<App>() only
├── Properties/
│   └── launchSettings.json      # http://localhost:5280
├── Routes.razor
├── _Imports.razor
├── appsettings.json
├── appsettings.Development.json
└── wwwroot/css/site.css
```

### ContactModel Validation Attributes

The model (`Models/ContactModel.cs`) exercises the core DataAnnotations that will need client-side support:

| Property | Attributes |
|----------|-----------|
| `Name` | `[Required]`, `[StringLength(100, MinimumLength = 2)]` |
| `Email` | `[Required]`, `[EmailAddress]` |
| `PhoneNumber` | `[Phone]` (optional) |
| `Age` | `[Required]`, `[Range(18, 120)]` |
| `Website` | `[Url]` (optional) |
| `Message` | `[Required]`, `[MinLength(10)]`, `[MaxLength(1000)]` |
| `ReferenceCode` | `[RegularExpression(@"^[A-Z]{2}-\d{4}$")]` (optional) |

### Verification with Playwright

Tested the running app interactively:

1. **Home page** (`/`) — renders correctly, link to contact form works
2. **Empty form submit** — all `[Required]` errors appear in both `ValidationSummary` (list at top) and per-field `ValidationMessage` components
3. **Invalid data submit** — tested with short name (StringLength), bad email (EmailAddress), out-of-range age (Range), short message (MinLength), invalid reference code (RegularExpression) — all produce correct error messages
4. **Valid data submit** — filling all fields correctly shows success message: "Thank you, Jane Smith! We received your message."

**Key observation**: Today, Blazor SSR forms render **no `data-val-*` attributes** on inputs, and `ValidationMessage`/`ValidationSummary` render **nothing** when there are no errors. All validation requires a server round-trip. This is the gap the feature will close.

## Step 3: Prior Art Analysis

**File:** `features/validation-client-side/02-prior-art.md` (uncommitted)

Deep-dive analysis of [haacked/aspnet-client-validation](https://github.com/haacked/aspnet-client-validation) — a jQuery-free (~4 KB gzip) drop-in replacement for `jquery.validate.unobtrusive.js`. Reviewed the full TypeScript source (~1,565 lines) and documented:

- **Architecture**: `ValidationService` orchestrator with pluggable `ValidationProvider` functions `(value, element, params) => boolean | string`. Providers registered by name, parsed from `data-val-{rule}` / `data-val-{rule}-{param}` attributes via a clean two-pass algorithm.
- **Built-in providers**: 12 validators (required, length, maxlength, minlength, range, regex, equalto, email, url, phone, creditcard, remote) — all skip validation on empty values, deferring to `required`.
- **Validation timing**: Debounced input/change events with smart UX — `input` events only *clear* errors, `change`/blur events can *set* errors. This prevents "red while typing" annoyance.
- **DOM manipulation**: Updates `data-valmsg-for` spans and `data-valmsg-summary` containers with CSS class toggling.
- **MutationObserver**: Watches for dynamic DOM changes (added/removed inputs).
- **Extensibility**: `addProvider()`, overridable hooks (preValidate, handleValidated, highlight/unhighlight), configurable CSS class names.

### Key Decisions from Analysis

**Adopt from the library:**
1. `data-val-*` attribute protocol (same as MVC, ecosystem compatible)
2. Provider/plugin architecture with `addProvider(name, callback)`
3. Two-pass directive parsing algorithm
4. Smart validation timing (clear on input, invalidate on change/blur)
5. Empty-value passthrough convention
6. `formnovalidate` support per HTML spec

**Do differently in our implementation:**
1. **Constraint Validation API** (`setCustomValidity()`) instead of custom state tracking — enables `:invalid` CSS pseudo-class, screen reader integration
2. **ARIA from day one** — `aria-invalid`, `aria-describedby`, `aria-live` (library has none)
3. **`textContent` not `innerHTML`** — prevent XSS in error message display
4. **`WeakMap` for state tracking** — instead of GUID arrays (O(1) lookup, auto-GC)
5. **Synchronous validation** — no Promises needed without remote validation
6. **Enhanced navigation integration** — hook into submit flow before enhanced nav, don't call `form.submit()` (which bypasses enhanced nav entirely)
7. **`enhancedload` event** — instead of MutationObserver for post-navigation re-scan

## Step 4: Prototype Implementation Plan

**File:** `features/validation-client-side/03-prototype-plan.md` (uncommitted)

Wrote a full implementation plan for the JavaScript validation library prototype. Key architectural decisions:

- **Three-layer architecture**: Core validation engine (host-agnostic) → Unobtrusive adapter (event wiring, error display) → Blazor wiring layer (`enhancedload` integration)
- **Constraint Validation API**: `setCustomValidity()` as the primary validity state mechanism — enables `:invalid` CSS pseudo-class and screen reader integration
- **Capture-phase submit interception**: Validation handler runs in capture phase, before Blazor's enhanced navigation handler (which uses bubble phase). If validation fails, `preventDefault()` + `stopPropagation()` blocks enhanced nav.
- **WeakMap state tracking**: `WeakMap<Element, State>` for O(1) lookup and automatic GC when elements are removed by DOM patching
- **ARIA-ready design**: `markInvalid()`/`markValid()` are explicit methods with commented extension points for `aria-invalid`, `aria-describedby`
- **Synchronous-only**: No async/Promise pipeline — keeps the prototype simple
- **MVC protocol compatibility**: Uses `data-val-*` / `data-valmsg-*` attribute protocol, MVC CSS class defaults

Planned file structure under `src/Components/Web.JS/src/Validation/`:
- `Types.ts`, `ValidationEngine.ts`, `BuiltInProviders.ts`, `DirectiveParser.ts`
- `ValidationCoordinator.ts`, `EventManager.ts`, `ErrorDisplay.ts`, `DomScanner.ts`
- `BlazorWiring.ts`, `index.ts`

8 built-in providers: required, length, minlength, maxlength, range, regex, email, url.
22 implementation tasks across 5 phases. Sample app to be modified with manual `data-val-*` attributes for testing.

## Step 5: Prototype Implementation

**Files created:**

### TypeScript source (`src/Components/Web.JS/src/Validation/`)

| File | Purpose | LOC |
|------|---------|-----|
| `Types.ts` | Shared types: `ValidatableElement`, `ValidationProvider`, `ValidationDirective`, `ElementState`, `CssClassConfig` | 50 |
| `ValidationEngine.ts` | Provider registry with `addProvider`/`setProvider`/`getProvider` | 27 |
| `BuiltInProviders.ts` | 8 providers: required, length, minlength, maxlength, range, regex, email, url | 70 |
| `DirectiveParser.ts` | Two-pass `data-val-*` attribute parser → `ValidationDirective[]` | 48 |
| `ErrorDisplay.ts` | `findMessageElements()`, `showFieldError()`, `clearFieldError()`, `updateSummary()` with CSS class management | 80 |
| `ValidationCoordinator.ts` | `WeakMap<Element, State>` state management, `markInvalid`/`markValid` with `setCustomValidity()`, `validateElement()`, `validateForm()` | 130 |
| `EventManager.ts` | Capture-phase submit interception (runs before enhanced nav), smart input/change handlers | 85 |
| `DomScanner.ts` | Idempotent DOM scanning, `novalidate` management | 50 |
| `BlazorWiring.ts` | `createValidationService()`, `initializeBlazorValidation()` with `enhancedload` hook, public API | 60 |
| `index.ts` | Entry point — auto-initializes on `DOMContentLoaded` | 10 |

### Build integration

- Added `'aspnet-validation': './src/Validation/index.ts'` entry to `rollup.config.mjs`
- Rollup produces `dist/Debug/aspnet-validation.js` (~7 KB raw, IIFE format with sourcemap)
- Copied to `src/Components/Samples/BlazorSSR/wwwroot/js/aspnet-validation.js`

### Sample app changes

| File | Change |
|------|--------|
| `Pages/ContactManual.razor` | **NEW** — Copy of Contact.razor at `/contact-manual` with hardcoded `data-val-*` attributes, `data-valmsg-for` spans, validation summary container, and `formnovalidate` bypass button |
| `Pages/Contact.razor` | **UNCHANGED** — Baseline server-only validation |
| `Pages/Index.razor` | Updated with links to both form pages |
| `App.razor` | Added `<script src="js/aspnet-validation.js">` after `blazor.web.js` |
| `wwwroot/css/site.css` | Added MVC-compatible validation CSS classes (`input-validation-error`, `field-validation-error`, `validation-summary-errors`, etc.) |

### Verification (jsdom tests — all pass)

| Test | Result |
|------|--------|
| Empty form → 4 required errors shown | ✅ |
| Valid data → all errors clear, form valid | ✅ |
| Invalid email → email error shown | ✅ |
| Out-of-range age → range error shown | ✅ |
| Short name (length min) → length error shown | ✅ |
| Invalid regex pattern → regex error shown | ✅ |
| Valid regex pattern → clears | ✅ |
| Invalid URL → url error shown | ✅ |
| Valid URL → all clear, form valid | ✅ |
| CSS classes toggle correctly | ✅ |
| `novalidate` set on form automatically | ✅ |
| `setCustomValidity()` called (Constraint Validation API) | ✅ |
| Validation summary `<li>` items rendered | ✅ |
| Optional fields (no `data-val`) skip validation | ✅ |

### Key architectural properties verified

1. **Capture-phase submit handler** — registered with `addEventListener('submit', handler, true)`, runs before enhanced nav's bubble-phase handler
2. **`WeakMap` state** — elements tracked without GUID arrays, auto-GC on DOM removal
3. **Smart timing** — `input` events only clear existing errors, `change` events can set new errors
4. **`setCustomValidity()`** — sets browser validity state, enables `:invalid` CSS pseudo-class
5. **`textContent` only** — no `innerHTML` for error messages (XSS prevention)
6. **ARIA extension points** — commented hooks in `markInvalid`/`markValid` for future `aria-invalid` / `aria-describedby`
7. **MVC protocol compatible** — same `data-val-*` / `data-valmsg-*` / CSS class names as MVC unobtrusive validation

## Step 6: C# Server-Side Design for Attribute Emission

**File:** `features/validation-client-side/04-csharp-design.md`

Designed the C# infrastructure to automatically emit `data-val-*` HTML attributes from Blazor input components. This involved:

### Analysis Performed

1. **MVC pipeline deep-dive**: Traced the full path from `InputTagHelper` → `DefaultHtmlGenerator.AddValidationAttributes()` → `DefaultValidationHtmlAttributeProvider` → `DataAnnotationsClientModelValidatorProvider` → `ValidationAttributeAdapterProvider` → concrete adapters (`RequiredAttributeAdapter`, `RangeAttributeAdapter`, etc.). Each adapter implements `AddValidation(ClientModelValidationContext)` calling `MergeAttribute()` to write `data-val-*` into an attributes dictionary.

2. **Blazor input component analysis**: Studied `InputBase<T>` (which only has `FieldIdentifier` — model ref + field name string, no access to validation attributes), all subclasses (`InputText`, `InputNumber`, `InputTextArea`, `InputSelect`), `EditForm` (cascades `EditContext`), `ValidationMessage<T>` (renders `<div>` per error, no `data-valmsg-for`), and `ValidationSummary` (renders `<ul>`, no `data-valmsg-summary`).

3. **Validation metadata discovery**: Analyzed both reflection-based discovery (`PropertyInfo.GetCustomAttributes<ValidationAttribute>()` with `ConcurrentDictionary` cache, as used by `DataAnnotationsValidator`) and the new source generator infrastructure (`ValidatableTypeInfo` / `ValidatablePropertyInfo` with `ValidationAttributeCache`).

4. **Localization integration**: Read the full proposal in [#65539](https://github.com/dotnet/aspnetcore/issues/65539). The design explicitly states that `ErrorMessageProviderContext` excludes validation-execution types so that "localized and formatted messages are retrievable *outside* of validation execution" — designed precisely for our client-side validation attribute emission use case.

### Key Design Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Integration point | Cascaded service via `EditContext.Properties` | Non-invasive; reuses existing `EditContext` cascade; no changes to `EditForm` |
| Opt-in | `<ClientSideValidator />` component | Follows `<DataAnnotationsValidator />` pattern; explicit per-form opt-in |
| ValidationMessage / Summary | Modify rendering when service is present | Backwards-compatible; renders `<span data-valmsg-for>` instead of `<div>` per message |
| Custom validators | `IClientValidationAdapter` + `ClientValidationAdapterRegistry` via DI | Blazor-native, no MVC dependency; options-based registry pattern |
| Metadata discovery | Source generator preferred, reflection fallback | AOT-friendly with graceful degradation |
| Localization | `ValidationOptions.ErrorMessageProvider` / `DisplayNameProvider` | From [#65539](https://github.com/dotnet/aspnetcore/issues/65539), designed for this use case |
| Script emission | `<ClientSideValidator IncludeScript="true" />` (default on) | Minimal ceremony; opt-out via `IncludeScript="false"` |

### Architecture

```
ClientSideValidator (component)
    → stores IClientValidationService on EditContext.Properties
    → optionally emits <script> tag

InputBase<T> (modified)
    → reads IClientValidationService from EditContext.Properties
    → merges data-val-* into AdditionalAttributes (Option B — zero subclass changes)

DefaultClientValidationService
    → discovers ValidationAttributes (source gen or reflection)
    → maps each to IClientValidationAdapter via ClientValidationAdapterRegistry
    → adapters emit data-val-* into attributes dictionary
    → error messages resolved via ValidationOptions.ErrorMessageProvider (localized)

ValidationMessage<T> (modified)
    → when service present: renders <span data-valmsg-for="fieldName">
ValidationSummary (modified)
    → when service present: renders container with data-valmsg-summary="true"
```

### New Types

| Type | Kind | Assembly |
|---|---|---|
| `IClientValidationService` | Interface | `Microsoft.AspNetCore.Components.Forms` |
| `IClientValidationAdapter` | Interface | `Microsoft.AspNetCore.Components.Forms` |
| `ClientValidationAdapterRegistry` | Options class | `Microsoft.AspNetCore.Components.Forms` |
| `ClientValidationContext` | Class | `Microsoft.AspNetCore.Components.Forms` |
| `DefaultClientValidationService` | Internal class | `Microsoft.AspNetCore.Components.Forms` |
| `BuiltInAdapterRegistration` | Internal class | `Microsoft.AspNetCore.Components.Forms` |
| 11 built-in adapters | Internal classes | `Microsoft.AspNetCore.Components.Forms` |
| `ErrorMessageResolver` | Internal static | `Microsoft.AspNetCore.Components.Forms` |
| `ClientSideValidator` | Component | `Microsoft.AspNetCore.Components.Web` |

### Built-In Adapters (11 total)

Mirrors MVC's `ValidationAttributeAdapterProvider` mapping:
- `RequiredAttribute` → `data-val-required`
- `StringLengthAttribute` → `data-val-length`, `-max`, `-min`
- `MinLengthAttribute` → `data-val-minlength`, `-min`
- `MaxLengthAttribute` → `data-val-maxlength`, `-max`
- `RangeAttribute` → `data-val-range`, `-min`, `-max`
- `RegularExpressionAttribute` → `data-val-regex`, `-pattern`
- `EmailAddressAttribute` → `data-val-email`
- `UrlAttribute` → `data-val-url`
- `CreditCardAttribute` → `data-val-creditcard`
- `PhoneAttribute` → `data-val-phone`
- `CompareAttribute` → `data-val-equalto`, `-other`

### Implementation phases: 5 phases, 24 tasks (detailed in design doc)

## Step 7: Enhanced Navigation Test Pages & Bug Fix

### Enhanced Navigation Test Pages

Extended the BlazorSSR sample app with pages that test adding a second form via enhanced navigation:

| File | Route | Purpose |
|------|-------|---------|
| `Models/SubscribeModel.cs` | — | Simple model (Name + Email with `[Required]`) |
| `Pages/ContactExtra.razor` | `/contact-extra` | Two forms (Contact + Subscribe), server-only validation |
| `Pages/ContactManualExtra.razor` | `/contact-manual-extra` | Two forms with hardcoded `data-val-*` attributes, client-side validation |
| `Pages/Contact.razor` | `/contact` | Added link to `/contact-extra` |
| `Pages/ContactManual.razor` | `/contact-manual` | Added link to `/contact-manual-extra` |
| `Pages/Index.razor` | `/` | Added links to all form pages |
| `App.razor` | — | Added `enhancedload` debug event handler, favicon link |

### Bug: Stale Validation State After Enhanced Navigation

**Symptom:** Hard-navigate to `/contact-manual` (1-form page), then enhanced-navigate to `/contact-manual-extra` (2-form page). Submit the subscribe form → Email field shows "Name is required." instead of "Email address is required."

**Root Cause — DomSync Element Reuse:**

Blazor's enhanced navigation uses DomSync (`src/Components/Web.JS/src/Services/NavigationEnhancement/DomSync.ts`) to patch the DOM rather than replacing it. DomSync uses Levenshtein edit distance with suffix matching:

1. **Suffix matching**: The old page's single `<form>` (contact form) gets matched with the new page's *last* `<form>` (subscribe form) because suffix matching matches forms from the end of the document.
2. **Element reuse**: DomSync KEEPS the old DOM node and calls `synchronizeAttributes()` to update its attributes to the new values.
3. **WeakMap retains stale state**: The validation library's `WeakMap<Element, ElementState>` still holds the old directives (with wrong error messages) for the kept DOM node.
4. **Scanner skips tracked elements**: The original `scan()` method checked `hasState(input)` and skipped elements that were already tracked — so it never re-parsed the (now changed) `data-val-*` attributes.

**Fix — Directive Fingerprinting:**

Added a `directiveFingerprint` field to `ElementState` (in `Types.ts`) — a sorted concatenation of all `data-val*` attribute name=value pairs. The `DomScanner.scan()` method now:

1. Checks if the element already has state
2. If yes, computes the current fingerprint and compares to the stored one
3. If fingerprints differ (attributes changed by DomSync), calls `unregisterElement()` to clean up old listeners/state, then re-registers with fresh directives
4. If fingerprints match, skips (efficient no-op)

**Files changed:**
- `src/Components/Web.JS/src/Validation/Types.ts` — Added `directiveFingerprint: string` to `ElementState`
- `src/Components/Web.JS/src/Validation/DomScanner.ts` — Added `getDirectiveFingerprint()` method, changed `scan()` to compare fingerprints and re-register on mismatch
- Rebuilt `aspnet-validation.js` (~7.4 KB)

**Verified with Playwright:** Enhanced nav from `/contact-manual` → `/contact-manual-extra`, submit subscribe form → Email correctly shows "Email address is required." and Name shows "Your name is required."

## Step 8: C# Server-Side Infrastructure — Design & Implementation Plan

**Documents:**
- `04-csharp-prototype-design.md` — Comprehensive design doc covering architecture decisions
- `05-csharp-prototype-plan.md` — Tests-first implementation plan in 5 phases

### Key Design Decisions

- **`ClientValidationContext`** is a `readonly struct` with private `IDictionary<string, string>` field — adapters interact only through `MergeAttribute()` (add-only, first-wins semantics)
- **`IClientValidationAdapter.AddClientValidation(in ClientValidationContext, string errorMessage)`** — context passed by `in` reference, error message as separate parameter (not on context)
- **Error message resolution** is the service's responsibility, not the adapters'. Adapters only map validation attribute properties to `data-val-*` HTML attribute names/values
- **Integration via `EditContext.Properties`** — `ClientSideValidator` component stores `IClientValidationService` on the edit context; `InputBase<T>`, `ValidationMessage<T>`, `ValidationSummary` read from it
- **Localization alignment** — `IClientValidationAdapter` and `IValidationAttributeFormatter` (#65539) are complementary single-responsibility interfaces

## Step 9: C# Implementation — Phases 1–5

Implemented the full C# infrastructure in 5 phases using a tests-first approach.

### Phase 1: Core Abstractions

Created the public API surface:

| File | Type |
|------|------|
| `src/Components/Forms/src/IClientValidationService.cs` | Interface |
| `src/Components/Forms/src/IClientValidationAdapter.cs` | Interface |
| `src/Components/Forms/src/ClientValidation/ClientValidationAdapterRegistry.cs` | Options class |
| `src/Components/Forms/src/ClientValidationContext.cs` | Readonly struct |
| `src/Components/Forms/test/ClientValidation/ClientValidationContextTest.cs` | 8 tests |

### Phase 2: Built-In Adapters

Created 8 adapter classes in `src/Components/Forms/src/ClientValidation/Adapters/` covering 11 validation attributes:

| Adapter | Attributes Covered |
|---------|--------------------|
| `RequiredClientAdapter` | `RequiredAttribute` |
| `StringLengthClientAdapter` | `StringLengthAttribute` |
| `MinLengthClientAdapter` | `MinLengthAttribute` |
| `MaxLengthClientAdapter` | `MaxLengthAttribute` |
| `RangeClientAdapter` | `RangeAttribute` |
| `RegexClientAdapter` | `RegularExpressionAttribute` |
| `DataTypeClientAdapter` | `EmailAddressAttribute`, `UrlAttribute`, `CreditCardAttribute`, `PhoneAttribute` |
| `CompareClientAdapter` | `CompareAttribute` |

Plus `BuiltInAdapterRegistration` (registers adapters on `ClientValidationAdapterRegistry` via options pipeline).
Tests: 17 adapter tests + 8 registry tests.

### Phase 3: DefaultClientValidationService

Created `DefaultClientValidationService` (internal sealed) with:
- Reflection-based `ValidationAttribute` discovery on model properties
- Display name resolution: `DisplayAttribute` → `DisplayNameAttribute` → property name
- `ConcurrentDictionary<(Type, string), IReadOnlyDictionary<string, string>>` caching
- Trimmer annotation (`[UnconditionalSuppressMessage]` for `IL2077`)

Tests: 14 tests covering attribute discovery, display names, caching, custom providers.

### Phase 4: Component Integration

**New component:**
- `src/Components/Web/src/Forms/ClientSideValidator.cs` — injects `IClientValidationService` from DI, stores on `EditContext.Properties`, optionally renders `<script>` tag

**Modified components:**
- `InputBase.cs` — added `MergeClientValidationAttributes()` called from `UpdateAdditionalValidationAttributes()`, merges `data-val-*` into `AdditionalAttributes` using `TryAdd` (developer attributes take precedence)
- `ValidationMessage.cs` — conditional render: `<span data-valmsg-for="fieldName">` when service present, original `<div>` per message otherwise
- `ValidationSummary.cs` — conditional render: `<div data-valmsg-summary="true"><ul>...</ul></div>` when service present

Tests: 5 InputText tests + 3 ValidationMessage tests + 3 ValidationSummary tests = 11 total.

### Phase 5: DI Registration + Sample App

**DI extension methods** in `ClientValidationServiceCollectionExtensions.cs`:
- `AddClientSideValidation()` — registers `ClientValidationAdapterRegistry` options with built-in adapters via `IConfigureOptions<ClientValidationAdapterRegistry>` (idempotent via `TryAddEnumerable`), uses `IOptions<ClientValidationAdapterRegistry>` to resolve the registry
- `AddClientValidationAdapter<TAttribute>(services, factory)` — registers custom adapter via `Configure<ClientValidationAdapterRegistry>()` (last-wins override semantics)

**Sample app updated:**
- `Program.cs` — added `builder.Services.AddClientSideValidation()`
- `Contact.razor` — added `<ClientSideValidator />` inside `<EditForm>` after `<DataAnnotationsValidator />`

Tests: 8 DI registration tests.

### Test Summary

| Project | Total Tests | New Tests |
|---------|-------------|-----------|
| Forms | 138 | 52 (8 context + 17 adapter + 8 registry + 13 service + 8 DI) |
| Web | 284 | 11 (5 InputText + 3 ValidationMessage + 3 ValidationSummary) |

### Registry Refactoring Note

After initial implementation, the adapter provider pattern (`IClientValidationAdapterProvider` / `DefaultClientValidationAdapterProvider`) was refactored to an options-based registry pattern (`ClientValidationAdapterRegistry`). This aligns with the localization proposal (PR #65460) where `ValidationAttributeFormatterRegistry` uses the same pattern. Key changes:
- `IClientValidationAdapterProvider` interface → removed; replaced by `ClientValidationAdapterRegistry` (public sealed options class)
- `DefaultClientValidationAdapterProvider` → removed; built-in adapter registration moved to `BuiltInAdapterRegistration` (an `IConfigureOptions<ClientValidationAdapterRegistry>`)
- `AddClientValidationAdapterProvider<T>()` → replaced by `AddClientValidationAdapter<TAttribute>(factory)` using `Configure<ClientValidationAdapterRegistry>()`
- `DefaultClientValidationService` now takes `ClientValidationAdapterRegistry` instead of `IClientValidationAdapterProvider`
- Custom adapters can now override built-in adapters (last-wins semantics), whereas the old provider pattern gave built-ins precedence
- Self-adapting pattern added: a `ValidationAttribute` that implements `IClientValidationAdapter` is returned directly without needing a factory registration

## Step 10: JS Validator Fixes & Completions

Fixed bugs and filled gaps in the JS validation providers to achieve full parity with .NET `System.ComponentModel.DataAnnotations`.

### Regex Fix (full-string match)

Changed `regex` provider from `RegExp.test()` (partial match) to `exec()` with `match.index === 0 && match[0].length === value.length` check. This matches .NET `RegularExpressionAttribute.IsValid()` and jquery-validation-unobtrusive behavior — the pattern must match the **entire** input, not just a substring.

### New Providers

| Provider | .NET Attribute | Logic |
|----------|---------------|-------|
| `creditcard` | `CreditCardAttribute` | Luhn algorithm ported from .NET: iterates chars in reverse, skips `-` and ` `, rejects other non-digit chars, doubles every other digit, checksum mod 10 must equal 0 |
| `equalto` | `CompareAttribute` | Resolves `*.PropertyName` convention (strips prefix from current field name, finds other element via `form.elements.namedItem()`), compares `value === otherValue` |
| `fileextensions` | `FileExtensionsAttribute` | Extracts extension via `lastIndexOf('.')`, normalizes allowed list (strips spaces/dots, lowercases), checks membership. Default: `png,jpg,jpeg,gif` |

### C# Adapter Added

| File | Purpose |
|------|---------|
| `src/Components/Forms/src/ClientValidation/Adapters/FileExtensionsClientAdapter.cs` | Emits `data-val-fileextensions` and `data-val-fileextensions-extensions` |

Registered in `BuiltInAdapterRegistration` alongside other built-in adapters.

### Comparative Analysis

Wrote `features/validation-client-side/06-js-validators.md` — comprehensive comparison of validation logic across 5 layers:
1. .NET `System.ComponentModel.DataAnnotations` (server-side source of truth)
2. MVC built-in adapters (attribute emission)
3. `jquery-validation-unobtrusive` (adapter bridge)
4. `jquery-validation` (JS validation logic for MVC)
5. Our JS prototype

Key findings: MVC's phone validation is silently broken (no adapter registered), email validation is stricter client-side than server-side across all libraries (accepted behavior), our URL/phone/creditcard implementations match .NET exactly.

### Test Coverage

| Suite | Tests | Notes |
|-------|-------|-------|
| JS `BuiltInProviders.test.ts` | 69 | url(10) + phone(19) + regex(10) + creditcard(12) + equalto(6) + fileextensions(12) |
| C# Forms | 129 | Unchanged — FileExtensions adapter is internal, covered by existing DI registration tests |

### JS Bundle Size

| Metric | Size |
|--------|------|
| Brotli | 2.56 KB |
| Gzipped | 2.85 KB |
