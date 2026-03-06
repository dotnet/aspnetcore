# Client-Side Form Validation for Blazor SSR — Research & Requirements

**Issue:** [dotnet/aspnetcore#51040](https://github.com/dotnet/aspnetcore/issues/51040)
**Milestone:** 11.0-preview4
**Date:** 2026-03-06

---

## 1. Background

### 1.1 The Gap

ASP.NET Core MVC and Razor Pages have long supported **unobtrusive client-side validation**: the server renders `data-val-*` HTML attributes derived from DataAnnotations on model properties, and a client-side JavaScript library (`jquery.validate.unobtrusive.js`) reads those attributes to enforce validation rules in the browser before form submission. This provides immediate, low-latency feedback to users without a server round-trip.

With .NET 8, Blazor introduced **static server-side rendering (SSR)** and **enhanced navigation**, enabling Blazor components to render as plain HTML without requiring an active SignalR circuit (Blazor Server) or WebAssembly download. Forms in SSR mode use `<EditForm>` with the `Enhance` attribute, submitting as standard HTTP POST requests, with server-side validation via `DataAnnotationsValidator`.

However, **client-side validation is not available in static SSR mode**. The [Microsoft documentation](https://learn.microsoft.com/aspnet/core/blazor/forms/validation) explicitly states: *"Client-side validation requires an active Blazor circuit"*. This means that for Blazor SSR forms, every validation check requires a full HTTP round-trip to the server, resulting in a degraded user experience compared to MVC/Razor Pages.

### 1.2 Demand

The tracking issue ([#51040](https://github.com/dotnet/aspnetcore/issues/51040)) has **35+ upvotes** and has been open since September 2023. Community members have repeatedly requested this feature across .NET 9 and .NET 10 planning cycles. The issue has been assigned to the **11.0-preview4 milestone**.

Key community sentiments:
- *"I don't really like having to switch my pure Static SSR site to Interactive SSR just to get this behavior."* — @julioct
- *"This will be very nice to have. It is worth investigating doing this with the native HTML validation without additional script."* — @Eirenarch
- *"Update the Inputs to spit out the `data-*` classes used for validation, then use Phil's library, or another like it for client-side validation."* — @sweeperq
- A community member (@sweeperq) built a proof-of-concept `SmartInput<T>` component that reads DataAnnotations via reflection and emits MVC-compatible `data-val-*` attributes. It works with Phil Haack's client-validation library but required workarounds for ValidationSummary/ValidationMessage components.

### 1.3 Related Work

| Project | Approach | Trade-offs |
|---------|----------|------------|
| **MVC Unobtrusive Validation** | Server emits `data-val-*` attrs; jQuery Validate parses them client-side | Proven, but depends on jQuery (~112 KB); validation logic duplicated in JS |
| **[aspnet-client-validation](https://github.com/haacked/aspnet-client-validation)** (Phil Haack) | Drop-in jQuery-free replacement for unobtrusive validation (~10.6 KB / ~4 KB gzip) | Community-maintained; same `data-val-*` protocol; supports MutationObserver for DOM changes |
| **[WasmClientSideValidation](https://github.com/DamianEdwards/WasmClientSideValidation)** (Damian Edwards) | Runs actual .NET DataAnnotation validators in browser via WebAssembly | Exact server-client parity; but requires Wasm download (~882 KB) which defeats the purpose of SSR-only |
| **Issue [#28640](https://github.com/dotnet/aspnetcore/issues/28640)** | Ship `ObjectGraphDataAnnotationsValidator` / complex type validation | Closed in .NET 11; complex type validation now supported via new Validation source generator |

---

## 2. Problem Definition

### 2.1 Core Problem

Blazor SSR forms currently provide **no client-side validation**. When a user fills out a form rendered via static SSR:

1. User enters invalid data (e.g., leaves a required field empty)
2. User clicks Submit
3. Browser sends HTTP POST to server
4. Server validates, finds errors, re-renders the page with validation messages
5. Browser receives and displays the updated page

This round-trip adds latency and degrades user experience. For many common validation rules (required fields, length limits, format patterns), the validation could and should happen instantly in the browser.

### 2.2 Why Existing Solutions Don't Apply

| Approach | Why It Doesn't Work for Blazor SSR |
|----------|-----------------------------------|
| **Blazor Server interactivity** | Requires SignalR circuit — server resources, latency, defeats purpose of SSR |
| **Blazor WebAssembly** | Requires downloading .NET runtime — heavy payload, defeats purpose of SSR |
| **MVC unobtrusive validation** | Blazor Input components don't emit `data-val-*` attributes; Blazor validation message/summary components don't render `data-valmsg-*` attributes; jQuery dependency |
| **Native HTML5 validation** | Limited to built-in rules (`required`, `pattern`, etc.); browser UI is inconsistent and not customizable; doesn't integrate with Blazor's validation message infrastructure |
| **Manual JavaScript** | Requires duplicating validation logic; no integration with DataAnnotations; fragile with enhanced navigation DOM patching |

### 2.3 Specific Challenges

1. **Enhanced Navigation Lifecycle**: Blazor's enhanced navigation patches the DOM without full page reloads. Any client-side validation JavaScript must survive DOM patching and reinitialize when new forms appear. The `enhancedload` event is the hook for this.

2. **Streaming Rendering**: During streaming SSR, form content may be updated incrementally. Validation must handle forms whose DOM is modified after initial render.

3. **No Existing Attribute Emission**: Blazor's `InputBase<T>`, `InputText`, `InputNumber<T>`, and other input components currently do **not** inspect DataAnnotations on the bound model property. They only render `name`, `id`, `class`, `value`, and additional user-supplied attributes.

4. **Validation Message Containers**: Blazor's `ValidationMessage<T>` and `ValidationSummary` components only render content when there are validation errors in the `EditContext`. In SSR, on initial GET, there are no errors, so these components render empty or nothing — there is no placeholder markup for client-side JavaScript to target.

5. **Server Validation Remains Authoritative**: Client-side validation is a UX optimization. The server must always re-validate on POST. The design must ensure that client and server validation rules are derived from the same source (DataAnnotations) to avoid divergence.

---

## 3. Current State Analysis

### 3.1 MVC Unobtrusive Validation Architecture

MVC's unobtrusive validation is a mature, well-tested system with the following architecture:

#### Server-Side: Data Attributes Emission Pipeline

```
Model Property with [Required], [StringLength], etc.
    ↓
Tag Helpers (InputTagHelper, ValidationMessageTagHelper, ValidationSummaryTagHelper)
    ↓
IHtmlGenerator → DefaultHtmlGenerator.AddValidationAttributes()
    ↓
DefaultValidationHtmlAttributeProvider
    ↓
ClientValidatorCache → IClientModelValidatorProvider
    ↓
DataAnnotationsClientModelValidatorProvider
    ↓
IValidationAttributeAdapterProvider → ValidationAttributeAdapterProvider
    ↓
Specific Adapters (RequiredAttributeAdapter, RangeAttributeAdapter, etc.)
    ↓
adapter.AddValidation(ClientModelValidationContext) → writes to Attributes dictionary
    ↓
HTML output: <input data-val="true" data-val-required="The Email field is required." ... />
```

#### Supported Validation Attributes and Their HTML Output

| DataAnnotation | Adapter Class | `data-val-*` Attributes |
|----------------|--------------|------------------------|
| `[Required]` | `RequiredAttributeAdapter` | `data-val-required="{message}"` |
| `[StringLength]` | `StringLengthAttributeAdapter` | `data-val-length="{msg}"`, `data-val-length-max`, `data-val-length-min` |
| `[MinLength]` | `MinLengthAttributeAdapter` | `data-val-minlength="{msg}"`, `data-val-minlength-min` |
| `[MaxLength]` | `MaxLengthAttributeAdapter` | `data-val-maxlength="{msg}"`, `data-val-maxlength-max` |
| `[Range]` | `RangeAttributeAdapter` | `data-val-range="{msg}"`, `data-val-range-min`, `data-val-range-max` |
| `[RegularExpression]` | `RegularExpressionAttributeAdapter` | `data-val-regex="{msg}"`, `data-val-regex-pattern` |
| `[Compare]` | `CompareAttributeAdapter` | `data-val-equalto="{msg}"`, `data-val-equalto-other` |
| `[EmailAddress]` | `DataTypeAttributeAdapter` | `data-val-email="{msg}"` |
| `[Url]` | `DataTypeAttributeAdapter` | `data-val-url="{msg}"` |
| `[Phone]` | `DataTypeAttributeAdapter` | `data-val-phone="{msg}"` |
| `[CreditCard]` | `DataTypeAttributeAdapter` | `data-val-creditcard="{msg}"` |
| `[FileExtensions]` | `FileExtensionsAttributeAdapter` | `data-val-fileextensions="{msg}"`, `data-val-fileextensions-extensions` |

#### Validation Message/Summary Markup

```html
<!-- ValidationMessageTagHelper output -->
<span data-valmsg-for="Email" data-valmsg-replace="true"
      class="field-validation-valid"></span>

<!-- ValidationSummaryTagHelper output -->
<div data-valmsg-summary="true" class="validation-summary-valid">
  <ul><li style="display:none"></li></ul>
</div>
```

#### Client-Side: jquery.validate.unobtrusive.js

- Scans DOM for elements with `data-val="true"`
- Extracts `data-val-{rule}` and `data-val-{rule}-{param}` attributes
- Configures jQuery Validate rules accordingly
- On blur/change/submit: validates, toggles CSS classes (`input-validation-error`/`input-validation-valid`), writes messages into `data-valmsg-for` containers

**Key files in repository:**
- `src/Mvc/Mvc.DataAnnotations/src/` — Adapters, providers
- `src/Mvc/Mvc.ViewFeatures/src/DefaultValidationHtmlAttributeProvider.cs`
- `src/Mvc/Mvc.TagHelpers/src/InputTagHelper.cs`
- `src/Mvc/Mvc.TagHelpers/src/ValidationMessageTagHelper.cs`
- `src/Mvc/Mvc.TagHelpers/src/ValidationSummaryTagHelper.cs`

### 3.2 Blazor Forms & Validation Architecture

#### Core Components

| Component | Location | Role |
|-----------|----------|------|
| `EditForm` | `Web/src/Forms/EditForm.cs` | Renders `<form>`, cascades `EditContext` |
| `EditContext` | `Forms/src/EditContext.cs` | Tracks field state, validation messages, events |
| `InputBase<T>` | `Web/src/Forms/InputBase.cs` | Abstract base for input components; two-way binding, CSS classes |
| `InputText`, `InputNumber<T>`, etc. | `Web/src/Forms/` | Concrete input implementations |
| `DataAnnotationsValidator` | `Forms/src/DataAnnotationsValidator.cs` | Subscribes to EditContext events; uses `System.ComponentModel.DataAnnotations.Validator` |
| `ValidationMessage<T>` | `Web/src/Forms/ValidationMessage.cs` | Renders per-field error messages |
| `ValidationSummary` | `Web/src/Forms/ValidationSummary.cs` | Renders all validation errors |

#### SSR-Specific Components

| Component | Location | Role |
|-----------|----------|------|
| `FormMappingContext` | `Web/src/Forms/Mapping/` | Accumulates binding errors from form POST |
| `FormMappingValidator` | `Web/src/Forms/Mapping/` | Bridges FormMappingContext errors to EditContext |
| `AntiforgeryToken` | `Web/src/Forms/` | Renders hidden antiforgery input |

#### What Blazor Renders Today (SSR)

```html
<!-- EditForm with Enhance -->
<form method="post" data-enhance>
  <input type="hidden" name="__RequestVerificationToken" value="..." />
  <input type="hidden" name="_handler" value="register" />

  <!-- InputText — NO data-val-* attributes -->
  <input id="Email" name="Email" value="" class="valid" />

  <!-- ValidationMessage — empty when no errors -->
  <div class="validation-message"></div>

  <!-- ValidationSummary — empty <ul> or nothing -->
  <ul class="validation-errors"></ul>

  <button type="submit">Save</button>
</form>
```

**Key observation:** There are no `data-val-*` attributes, no `data-valmsg-for` attributes, and no placeholder containers for client-side validation to target.

#### Enhanced Navigation & Form Submission Flow

1. `data-enhance` on `<form>` opts the form into enhanced submission
2. `NavigationEnhancement.ts` intercepts the `submit` event
3. Collects `FormData`, sends via `fetch()` as POST
4. Response HTML is diffed and patched into the DOM via `DomSync.ts`
5. Events fired: `enhancednavigationstart` → `enhancedload` → `enhancednavigationend`

**No client-side validation occurs at any point in this flow.**

### 3.3 Comparison: MVC vs Blazor SSR Forms

| Feature | MVC/Razor Pages | Blazor SSR |
|---------|----------------|------------|
| Server-side validation | ✅ ModelState + DataAnnotations | ✅ EditContext + DataAnnotationsValidator |
| `data-val-*` attributes on inputs | ✅ Emitted by tag helpers | ❌ Not emitted |
| Validation message containers | ✅ `data-valmsg-for` spans | ❌ Empty/absent when no errors |
| Client-side validation JS | ✅ jquery.validate.unobtrusive.js | ❌ None |
| Enhanced navigation support | N/A (full page loads) | ✅ DOM patching via `data-enhance` |
| Custom validator extensibility | ✅ `IClientModelValidator` | ❌ N/A |

---

## 4. Web Standards: The Constraint Validation API

The [Constraint Validation API](https://developer.mozilla.org/en-US/docs/Web/API/Constraint_validation) is a W3C standard built into all modern browsers. It provides a standardized way to perform form validation without any library dependencies.

### 4.1 Key APIs

| API | Description |
|-----|-------------|
| `element.validity` | Returns a `ValidityState` object with boolean flags for each constraint |
| `element.validationMessage` | The browser-composed error message string |
| `element.willValidate` | Whether the element participates in constraint validation |
| `element.checkValidity()` | Returns `true` if valid; fires `invalid` event if not |
| `element.reportValidity()` | Like `checkValidity()` but also triggers browser UI (tooltip) |
| `element.setCustomValidity(msg)` | Sets a custom error message; empty string clears the error |
| `form.noValidate` | Disables native browser validation UI on submit |
| `invalid` event | Fired on elements that fail constraint validation |

### 4.2 ValidityState Properties

| Property | Maps To |
|----------|---------|
| `valueMissing` | `required` attribute |
| `typeMismatch` | `type="email"`, `type="url"`, etc. |
| `patternMismatch` | `pattern` attribute |
| `tooLong` | `maxlength` attribute |
| `tooShort` | `minlength` attribute |
| `rangeUnderflow` | `min` attribute |
| `rangeOverflow` | `max` attribute |
| `stepMismatch` | `step` attribute |
| `badInput` | Browser can't parse user input (e.g., letters in `type="number"`) |
| `customError` | Set via `setCustomValidity()` |
| `valid` | `true` if all constraints satisfied |

### 4.3 Browser Support

The Constraint Validation API has **universal support** across all modern browsers:
- Chrome 10+, Edge 12+, Firefox 4+, Safari 5.1+, Opera 12.1+
- Full support on mobile: Android 4.4+, iOS Safari 5+

No polyfills are needed for any browser in active use as of 2025–2026.

### 4.4 Relevance to This Feature

The design doc ([comment by @javiercn](https://github.com/dotnet/aspnetcore/issues/51040#issuecomment-3706000376)) proposes using the Constraint Validation API as the **primary extensibility and validation semantics layer**:

- Validation state is expressed via `setCustomValidity()` — standard API, inspectable by any third-party library
- `ValidityState` properties provide a standardized vocabulary for "why is this field invalid?"
- The `invalid` event provides a hook for third-party UI libraries
- `form.noValidate` suppresses native browser tooltips while keeping programmatic validation functional
- No dependency on jQuery or any third-party library

This approach is a significant improvement over MVC's jQuery dependency, using web standards that are:
- Zero-dependency (built into the browser)
- Inspectable and composable by third-party libraries
- Future-proof (W3C standard)

---

## 5. Design Proposal Overview (from @javiercn)

The design doc proposes a **three-layer architecture**:

### Layer 1: Core Validation Library (JS, no dependencies)
- Pure JavaScript validation engine
- Reads `data-val-*` attributes from DOM elements
- Evaluates rules against element values
- Uses `setCustomValidity()` to set/clear validity state
- Host-agnostic — could be consumed by MVC as well

### Layer 2: Unobtrusive Adapter
- Consumes a **subset** of MVC-style `data-val-*` attributes (not full parity)
- Decides **when** to validate: `input`, `change`, `blur`, `submit` events
- Supported rules: Required, MinLength/MaxLength/StringLength, Range, RegularExpression, basic type constraints (email, url, number)
- **Out of scope**: Compare, CreditCard, Phone, FileExtensions, Remote validation

### Layer 3: Blazor Wiring Layer
- Initializes validation on page load
- Re-initializes on `enhancedload` (enhanced navigation DOM patching)
- Sets `novalidate` on participating forms
- Idempotent — avoids double-binding

### Optional: Validation Message Custom Element
- A web component (e.g., `<asp-validation-message>`) that binds to a field
- Reads validity state from Constraint Validation API
- Handles ARIA and accessible rendering
- Uses `ElementInternals` for form association

---

## 6. Community Feedback & Workarounds

### 6.1 Common Workarounds Used Today

1. **Switch to Interactive SSR**: Adding `@rendermode InteractiveServer` to get validation — but this creates a SignalR circuit, adding server load and latency
2. **Native HTML5 Attributes**: Manually adding `required`, `pattern`, `min`, `max` to inputs — limited, inconsistent browser UI, no integration with Blazor validation
3. **Custom JavaScript**: Hand-writing validation scripts — duplicates logic, fragile with enhanced navigation
4. **Third-party Libraries**: Using Phil Haack's `aspnet-client-validation` with custom `SmartInput` components that emit `data-val-*` attributes via reflection — functional but requires significant boilerplate

### 6.2 Key Community Insights

- **Strong preference for no-Wasm solution**: When Damian Edwards shared his WebAssembly-based validation experiment, community pushback was clear: *"This is the reverse of what is needed here. If we could accept the downsides of downloading and running wasm we would use Blazor WASM to begin with."* — @Eirenarch
- **Native HTML validation interest**: Some community members want to leverage browser-native validation without additional JavaScript where possible
- **MVC parity is expected**: Developers migrating from MVC/Razor Pages expect the same client-validation experience in Blazor SSR
- **`data-val-*` protocol is well-understood**: The MVC `data-val-*` attribute protocol is familiar to the ASP.NET ecosystem and has proven extensibility via `IClientModelValidator`

---

## 7. Requirements

### 7.1 Functional Requirements

#### FR-1: Emit Validation Attributes on Input Elements
Blazor input components (`InputText`, `InputNumber<T>`, `InputDate<T>`, `InputSelect<T>`, `InputTextArea`, `InputCheckbox`) MUST emit `data-val-*` HTML attributes derived from DataAnnotations on the bound model property when rendered in SSR mode.

**Supported DataAnnotations (minimum set):**
- `[Required]`
- `[StringLength]`
- `[MinLength]`
- `[MaxLength]`
- `[Range]`
- `[RegularExpression]`
- `[EmailAddress]`, `[Url]` (type-based validation)

#### FR-2: Client-Side Validation Before Submission
A JavaScript validation engine MUST validate form inputs client-side and prevent form submission when validation fails, without requiring a server round-trip.

#### FR-3: Validation Message Display
Validation error messages MUST be displayed to the user adjacent to the invalid field and/or in a summary area, consistent with the `ValidationMessage<T>` and `ValidationSummary` component patterns.

#### FR-4: Enhanced Navigation Compatibility
Client-side validation MUST work correctly across the enhanced navigation lifecycle:
- On initial page load
- After enhanced navigation DOM patches (new forms appearing, existing forms being updated)
- During and after streaming rendering updates

#### FR-5: Server Validation Remains Authoritative
Client-side validation is a UX optimization. Server-side validation MUST continue to work exactly as it does today. Client and server validation MUST be derived from the same DataAnnotations source to prevent divergence.

#### FR-6: Opt-in Behavior
Client-side validation SHOULD be opt-in (not breaking change). Existing Blazor SSR forms without the feature enabled MUST continue to work as they do today.

#### FR-7: Validation Timing
The validation engine SHOULD support configurable validation timing:
- On blur (field loses focus) — default for initial interaction
- On input/change (as user types) — after field has been shown invalid once
- On submit (form submission attempt)

#### FR-8: No Third-Party Dependencies
The client-side validation JavaScript MUST NOT depend on jQuery, jQuery Validate, or any other third-party library. It SHOULD use web standards (Constraint Validation API) as the foundation.

### 7.2 Non-Functional Requirements

#### NFR-1: Payload Size
The client-side JavaScript MUST be lightweight. Target: significantly smaller than the MVC jQuery stack (~112 KB for jQuery + jQuery Validate + unobtrusive). The `aspnet-client-validation` library achieves ~4 KB gzipped as a reference point.

#### NFR-2: No New Server Dependencies
Enabling client-side validation MUST NOT require an active Blazor circuit, WebSocket connection, or WebAssembly download.

#### NFR-3: Accessibility
Validation messages and error states MUST be accessible:
- Use appropriate ARIA attributes (`aria-invalid`, `aria-describedby`, `aria-live`)
- Error messages must be associated with their input fields
- Screen readers must announce validation errors

#### NFR-4: Extensibility
The validation system SHOULD provide extensibility points for:
- Custom validation rules (third-party `data-val-*` attributes)
- Custom validation UI rendering
- Integration with CSS frameworks (configurable CSS class names)

#### NFR-5: MVC Convergence Path
The core validation JavaScript SHOULD be host-agnostic so that MVC/Razor Pages can eventually consume it to replace the jQuery-dependent unobtrusive validation, enabling a migration path.

#### NFR-6: Consistency with MVC Protocol
Where supported, the `data-val-*` attribute naming and semantics SHOULD be compatible with the existing MVC unobtrusive validation protocol, enabling existing client-side validation libraries (e.g., `aspnet-client-validation`) to work with Blazor SSR forms.

### 7.3 Out of Scope

- Full parity with all MVC client validators (Compare, CreditCard, Phone, FileExtensions, Remote)
- Remote/async server-backed validation
- Complex/nested object graph validation on the client
- Custom browser validation UI (native tooltips) — these will be explicitly suppressed
- Interactive render mode validation changes (Blazor Server, Blazor WebAssembly already work)

---

## 8. Open Questions

1. **Attribute emission scope**: Should `data-val-*` attributes be emitted only in SSR mode, or also in interactive mode (for consistency)?

2. **Opt-in mechanism**: How should developers enable client-side validation? Options include:
   - A parameter on `EditForm` (e.g., `ClientValidation="true"`)
   - A component (e.g., `<ClientValidationScript />`)
   - Automatic when `DataAnnotationsValidator` is present in SSR mode
   - Global configuration in `Program.cs`

3. **Validation message containers**: Should `ValidationMessage<T>` and `ValidationSummary` render placeholder markup (e.g., empty `<span data-valmsg-for="...">`) even when there are no errors, to give client-side JS a target? Or should a custom element approach be used?

4. **JavaScript bundling**: Should the validation JS be part of `blazor.web.js` (always loaded) or a separate script loaded on demand?

5. **CSS class compatibility**: Should the default CSS classes match MVC's (`input-validation-error`, `field-validation-error`) or Blazor's existing (`invalid`, `validation-message`)?

6. **Custom validator extensibility**: Should there be an equivalent to MVC's `IClientModelValidator` for Blazor? How would custom components emit custom `data-val-*` attributes?

7. **Interaction with `[SupplyParameterFromForm]`**: How does this feature interact with the `[SupplyParameterFromForm]` attribute binding in SSR mode?

8. **Interaction with the new Validation source generator**: The new `Microsoft.AspNetCore.Validation` source generator (from .NET 11) generates validation code. Should the client-side attribute emission leverage the same source generator, or should it use runtime reflection like MVC does?

---

## 9. References

### Documentation
- [Blazor Forms Validation](https://learn.microsoft.com/aspnet/core/blazor/forms/validation)
- [Blazor Static SSR JS Interop](https://learn.microsoft.com/aspnet/core/blazor/javascript-interoperability/static-server-rendering)
- [MVC Model Validation](https://learn.microsoft.com/aspnet/core/mvc/models/validation)
- [MDN: Constraint Validation](https://developer.mozilla.org/en-US/docs/Web/HTML/Guides/Constraint_validation)
- [MDN: Constraint Validation API](https://developer.mozilla.org/en-US/docs/Web/API/Constraint_validation)
- [MDN: ValidityState](https://developer.mozilla.org/en-US/docs/Web/API/ValidityState)
- [MDN: setCustomValidity](https://developer.mozilla.org/en-US/docs/Web/API/HTMLObjectElement/setCustomValidity)
- [MDN: ElementInternals](https://developer.mozilla.org/en-US/docs/Web/API/ElementInternals)

### Source Code (in this repository)
- MVC Adapters: `src/Mvc/Mvc.DataAnnotations/src/`
- MVC Tag Helpers: `src/Mvc/Mvc.TagHelpers/src/InputTagHelper.cs`
- MVC Validation Provider: `src/Mvc/Mvc.ViewFeatures/src/DefaultValidationHtmlAttributeProvider.cs`
- Blazor EditForm: `src/Components/Web/src/Forms/EditForm.cs`
- Blazor InputBase: `src/Components/Web/src/Forms/InputBase.cs`
- Blazor EditContext: `src/Components/Forms/src/EditContext.cs`
- Blazor DataAnnotationsValidator: `src/Components/Forms/src/DataAnnotationsValidator.cs`
- Blazor ValidationMessage: `src/Components/Web/src/Forms/ValidationMessage.cs`
- Blazor ValidationSummary: `src/Components/Web/src/Forms/ValidationSummary.cs`
- Web.JS Enhanced Navigation: `src/Components/Web.JS/src/Services/NavigationEnhancement.ts`
- Web.JS DOM Sync: `src/Components/Web.JS/src/DomSync.ts`
- Web.JS Boot: `src/Components/Web.JS/src/Boot.Web.ts`

### External Projects
- [aspnet-client-validation](https://github.com/haacked/aspnet-client-validation) — jQuery-free MVC client validation (~4 KB gzip)
- [WasmClientSideValidation](https://github.com/DamianEdwards/WasmClientSideValidation) — Wasm-based experiment by Damian Edwards

### Issue Tracker
- [#51040](https://github.com/dotnet/aspnetcore/issues/51040) — Main tracking issue
- [#28640](https://github.com/dotnet/aspnetcore/issues/28640) — Complex type validation (closed, related)
