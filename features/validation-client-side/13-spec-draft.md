# Summary

This document proposes adding **first-party, zero-dependency client-side form validation** to ASP.NET Core that works for **Blazor Web Apps** (static SSR with both enhanced and non-enhanced forms) and **MVC / Razor Pages**, closing a long-standing gap where Blazor forms require interactivity for client validation and MVC relies on a large and hard-to-maintain jQuery-based library bundle.

We propose creating a new minimal **JavaScript validation library** that understands the existing `data-val-*` attribute protocol and uses the browser-native **Constraint Validation API** as its validation state mechanism. For Blazor, the validation JS is **bundled into `blazor.web.js`** and the C# service layer is **enabled by default via `AddRazorComponents()`**. For MVC and Razor Pages, the JS library is compatible with the existing HTML generation and can serve as a **drop-in replacement** for the jQuery validation stack.

## Goals

- **Enable client-side validation for Blazor SSR without interactivity.** Today, Blazor Web Apps rendered via static SSR have no client-side validation — every validation check requires a full .NET invocation. This feature adds immediate, in-browser validation feedback using standard `DataAnnotations`, without requiring Blazor Server interactivity or WebAssembly. Client-side validation is activated per-form via the existing `<DataAnnotationsValidator />` component. Standard form input components (`<InputText>`, `<InputNumber>`, etc.) automatically emit `data-val-*` attributes on statically-rendered forms, and the validation JS validates on the client.

- **Provide a zero-dependency JavaScript validation library.** The current MVC client-side validation stack depends on the combination of `jQuery`, `jquery-validate`, and `jquery-validation-unobtrusive`, totalling more than 40 KB (gzip). This feature delivers a JavaScript validation library with no external dependencies that understands the same `data-val-*` attribute protocol. It uses the browser-native Constraint Validation API (`setCustomValidity`, `ValidityState`, `checkValidity`) as the validation state mechanism and extensibility surface, enabling third-party libraries to read standardized validity state without coupling to our internals. For Blazor, the JS library is bundled into `blazor.web.js`. The target size is ≤ 5 KB gzip-compressed.

- **Work across the Blazor enhanced navigation lifecycle.** The client-side validation supports both enhanced and non-enhanced SSR forms. Blazor's enhanced navigation patches the DOM without full page loads. The validation library automatically re-scans for new or changed forms after each navigation, handles DOM element reuse, and intercepts form submission so validation runs before the enhanced navigation fetch. No developer action is required beyond the initial setup.

- **Support extensibility for custom validators.** Developers can register custom validation providers on the JavaScript side and custom adapters on the C# side, enabling the same custom-attribute-to-client-rule pipeline that MVC provides but with fewer layers of indirection.

- **Be compatible with ASP.NET Core MVC / Razor Pages as a drop-in replacement for jQuery unobtrusive validation.** The JS library reads the same `data-val-*` attributes that MVC tag helpers already emit and uses the same CSS class names (`input-validation-error`, `field-validation-error`, `validation-summary-errors`, etc.) and `data-valmsg-for` / `data-valmsg-summary` conventions.

## Non-goals

- **Full parity with the jQuery unobtrusive validation rule ecosystem.** The feature targets a focused subset of validators aligned with core `System.ComponentModel.DataAnnotations` attributes (Required, StringLength, MinLength, MaxLength, Range, RegularExpression, Email, Url, Phone, CreditCard, Compare, FileExtensions). Niche MVC-specific validators and any third-party jquery-validation plugins are not replicated. Custom validators can be added via the extensibility API.

- **Remote / async server-backed validation in Blazor SSR.** MVC's `[Remote]` attribute pattern (where the server is queried via AJAX during client-side validation) is supported only when used from MVC apps. In Blazor SSR, async and remote validation is **not supported in this release**. `RemoteAttribute` on Blazor models will throw `NotSupportedException` at render time. Custom async JavaScript providers registered in Blazor mode will throw at registration time. This is a deliberate constraint — async validation in Blazor conflicts with the mechanism through which enhanced navigation intercepts form submission. Making these compatible would require more extensive changes which are out-of-scope here.

- **Client-side validation for interactive render modes.** This feature targets static SSR forms only. When a form is rendered interactively (Blazor Server or Blazor WebAssembly), validation is handled by the existing Blazor interactive validation pipeline (via `EditContext` and C# validation logic running on the circuit or in WebAssembly). The `data-val-*` attributes should not be emitted on interactive forms, and the JS validation code must not run or modify the DOM in interactive rendering contexts. This ensures no conflicts between the two validation approaches.

- **Replacing browser-native validation UI.** The library suppresses native browser validation tooltips and manages its own error display via CSS classes and message target elements. However, it does not attempt to provide a rich validation UI framework (tooltips, animations, etc.). Richer UI is left to application CSS and/or third-party libraries that can read the Constraint Validation API state.

- **Source generator for validation metadata.** The C# attribute discovery uses reflection with per-field caching. This is trimming- and AOT-compatible via the same annotations as the existing `DataAnnotationsValidator` — model types are application code and are not trimmed by default. A source-generator approach that avoids reflection entirely is a potential future optimization but is not part of this feature.

- **Custom element for validation targets.** The original proposal included an optional custom element (e.g., `<asp-validation-message>`) using `ElementInternals` for message rendering and built-in ARIA semantics. This has been deferred in favor of the simpler approach: plain `<span data-valmsg-for>` elements (compatible with both MVC and Blazor conventions) with ARIA attributes managed directly by the JS library. The custom element approach may be revisited in a future release if there is demand for a more standards-based abstraction.

- **Legacy ASP.NET MVC 5 support.** The drop-in replacement targets ASP.NET Core MVC only. While the `data-val-*` protocol is largely the same across ASP.NET MVC 5 and ASP.NET Core MVC, differences in anti-forgery token handling, remote validation URL generation, and other details mean that legacy MVC 5 apps are not a tested or supported scenario.

## Proposed solution

The solution consists of two main parts:

1. **A JavaScript validation library** bundled into `blazor.web.js` (and also available as a standalone file for MVC) that scans the DOM for `data-val-*` attributes, validates form fields on user interaction and submission, displays error messages, and integrates with the Constraint Validation API. For Blazor, it hooks into enhanced navigation to re-scan after DOM patching and exposes its API as `Blazor.validation` (on the global Blazor object). For MVC, it initializes on page load and exposes the same API as `window.__aspnetValidation`.

2. **A Blazor C# service layer**, enabled by default via `AddRazorComponents()`, that causes standard Blazor input components (`InputText`, `InputNumber`, `InputSelect`, etc.) to automatically emit `data-val-*` attributes derived from `DataAnnotations` on the model when rendering in a static SSR context. Client-side validation is activated per-form by including `<DataAnnotationsValidator />` — the same component that already enables DataAnnotations-based server-side validation. Without `<DataAnnotationsValidator />`, no `data-val-*` attributes are emitted and no client-side validation occurs. Opt-out is available via a parameter on `DataAnnotationsValidator`.

Client-side validation is a **progressive enhancement**: forms are fully functional before the script loads, and server-side validation always remains authoritative. The client library improves UX by providing immediate feedback without a round-trip.

**Supported modes:**

- Blazor SSR with enhanced forms (`<EditForm Enhance>`)
- Blazor SSR with non-enhanced forms (`<EditForm>` without `Enhance`)
- MVC / Razor Pages forms

### Scenario 1: Basic Blazor SSR form with client-side validation

A statically-rendered Blazor form gets immediate client-side validation without enabling interactive render modes.

**Program.cs — standard Blazor SSR setup:**

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents();  // Client-side validation is included

var app = builder.Build();
app.MapRazorComponents<App>();
app.Run();
```

**Register.razor:**

```razor
<EditForm Model="model" Enhance FormName="register">
    <DataAnnotationsValidator />
    <ValidationSummary />

    <div>
        <label>Email</label>
        <InputText @bind-Value="model.Email" />
        <ValidationMessage For="@(() => model.Email)" />
    </div>

    <div>
        <label>Password</label>
        <InputText @bind-Value="model.Password" type="password" />
        <ValidationMessage For="@(() => model.Password)" />
    </div>

    <button type="submit">Register</button>
</EditForm>

@code {
    RegisterModel model = new();

    class RegisterModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = "";
    }
}
```

**What happens:**

- The form input components automatically emit validation attributes based on the model's `DataAnnotations`. `<ValidationMessage>` and `<ValidationSummary>` render corresponding error display targets.
- The validation JS (bundled in `blazor.web.js`) scans the DOM, discovers the annotated elements, and wires validation.
- **On blur/change:** the field is validated and errors are shown or cleared.
- **On typing:** existing errors are cleared in real-time (but new errors are not shown until blur, to avoid flashing red while the user types).
- **On submit:** all fields are validated. If any are invalid, submission is blocked and errors are displayed. If all are valid, the form submits normally via enhanced navigation.

### Scenario 2: Supported validation rules (Blazor)

The following `DataAnnotations` attributes are supported out of the box for Blazor SSR. The mapping uses the same `data-val-*` attribute protocol as MVC, ensuring full compatibility:

| DataAnnotation | Validation Rule | Parameters |
|---|---|---|
| `[Required]` | `required` | — |
| `[StringLength]` | `length` | `min`, `max` |
| `[MinLength]` | `minlength` | `min` |
| `[MaxLength]` | `maxlength` | `max` |
| `[Range]` | `range` | `min`, `max` |
| `[RegularExpression]` | `regex` | `pattern` |
| `[EmailAddress]` | `email` | — |
| `[Url]` | `url` | — |
| `[Phone]` | `phone` | — |
| `[CreditCard]` | `creditcard` | — |
| `[Compare]` | `equalto` | `other` (e.g., `*.ConfirmPassword`) |
| `[FileExtensions]` | `fileextensions` | `extensions` |
| *(numeric types)* | `number` | — |

**Notes:**

- MVC's `NumericClientModelValidatorProvider` automatically emits `data-val-number` for `float`, `double`, and `decimal` properties. The JS library includes a `number` provider that validates the value is a parseable number, providing MVC compatibility. Blazor does not emit `data-val-number` — `InputNumber<T>` renders `type="number"` which provides browser-native numeric enforcement.
- `InputFile` does not extend `InputBase<T>` and does not participate in the client-side validation framework. Validation attributes such as `[Required]` or `[FileExtensions]` on `InputFile`-bound properties will not produce `data-val-*` attributes. File validation is server-side only.
- `InputRadioGroup<T>` extends `InputBase<T>` and emits `data-val-*` attributes on its container element. Individual `<InputRadio>` components do not emit validation attributes. The JS library's `required` provider detects radio inputs and validates that at least one radio with the same `name` attribute is checked within the form.
- Nested models are fully supported. A property like `model.Address.Street` produces validation attributes with the correct dotted-path field name, and `<ValidationMessage For="@(() => model.Address.Street)" />` renders the matching `data-valmsg-for` target.

### Scenario 3: Localized error messages (Blazor)

Error messages are resolved on the server at render time and embedded in the `data-val-*` attributes as static strings. By default, `ValidationAttribute.FormatErrorMessage()` is used with the display name derived from `[Display(Name = "...")]`, `[DisplayName("...")]`, or the property name as fallback.

When validation localization is enabled in the app (see [#65539](https://github.com/dotnet/aspnetcore/issues/65539)), client-side validation displays the **same localized messages** as server-side validation. This ensures a consistent user experience regardless of whether a validation error is caught on the client or the server.

**Setup — enable validation localization with a shared resource file:**

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents();

builder.Services.AddValidation();
builder.Services.AddValidationLocalization<ValidationMessages>(); // TBD API
```

**Model — uses `ErrorMessage` as localization key:**

```csharp
public class RegisterModel
{
    [Required(ErrorMessage = "RequiredError")]
    [EmailAddress(ErrorMessage = "EmailError")]
    [Display(Name = "EmailAddress")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "RequiredError")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "StringLengthError")]
    [Display(Name = "Password")]
    public string Password { get; set; } = "";
}
```

**Rendered HTML (for a French-language request):**

```html
<input type="text" name="model.Email"
       data-val="true"
       data-val-required="Le champ Adresse e-mail est obligatoire."
       data-val-email="Le champ Adresse e-mail n'est pas une adresse e-mail valide." />
<span data-valmsg-for="model.Email" class="field-validation-valid"></span>
```

### Scenario 4: Validation timing and UX behavior

The library follows the same validation timing strategy as MVC's jQuery validation stack, designed to avoid the "everything is red immediately" problem while providing responsive feedback:

**Before first submit** (pristine form):

| User action | Behavior |
|---|---|
| **Typing** (`input` event) | **No validation.** Fields are pristine — errors are not shown while the user fills out the form for the first time. |
| **Leaving a field** (`change`/`blur` event) | **Full validation.** Shows or clears errors. This is the primary trigger for displaying validation errors before the first submit. |
| **Submitting the form** | **Validates all fields.** Blocks submission if any field is invalid. Updates the validation summary. Marks the form as "submitted" — after this point, typing validation becomes active (see below). |
| **Resetting the form** | **Clears all validation state.** CSS classes, error messages, validation summary, ARIA attributes, and the submitted/invalid tracking are all reset. The form returns to its pristine state. |

**After first submit** (or after a field has been shown invalid):

| User action | Behavior |
|---|---|
| **Typing** (`input` event) | **Full validation.** Once the form has been submitted or a field has been marked invalid, typing triggers real-time validation — errors can be shown, cleared, or replaced as the user types. This provides immediate feedback as the user corrects issues. |
| **Leaving a field** | **Full validation** (same as before submit). |

**Hidden fields** are skipped. Fields that are not visible (e.g., in a hidden step of a multi-step form) are excluded from validation, matching the jQuery validation default.

#### Per-field validation event override (`data-val-event`)

Individual fields can override the default validation events using the `data-val-event` attribute. This is useful for fields that need different timing (e.g., a field that should only validate on submit, or a select that should validate immediately on change):

```html
<!-- Default: validates on input (after submit) and change/blur -->
<input data-val="true" data-val-required="Required." />

<!-- Only validate on blur, not while typing (even after submit) -->
<input data-val="true" data-val-required="Required." data-val-event="change" />

<!-- Only validate on form submit, skip real-time validation entirely -->
<input data-val="true" data-val-required="Required." data-val-event="none" />
```

In Blazor, the `data-val-event` attribute can be set via `AdditionalAttributes`:

```razor
<InputText @bind-Value="model.Name" data-val-event="change" />
```

### Scenario 5: Enhanced navigation and streaming (Blazor)

Forms that appear after a Blazor enhanced navigation or streaming rendering update are **automatically wired for validation** with no developer action required.

**How it works for the developer:** The validation JS (bundled in `blazor.web.js`) automatically re-scans the DOM after each enhanced navigation or streaming rendering update. New form fields are discovered and wired automatically. Existing fields whose validation attributes changed (e.g., because the server re-rendered with different rules) are detected and re-registered.

**Multiple forms on a page** are supported. Each form is validated independently—submitting one form does not trigger validation on another.

### Scenario 6: Opt-in and opt-out

#### Blazor

Client-side validation is activated for a form by including `<DataAnnotationsValidator />`, the same component that already enables server-side DataAnnotations validation. In a static SSR context, `DataAnnotationsValidator` enables both server-side and client-side validation. Forms without `<DataAnnotationsValidator />` have no DataAnnotations validation at all (neither server nor client).

To keep server-side DataAnnotations validation but disable client-side validation on a specific form, use the `EnableClientValidation` parameter:

```razor
<EditForm Model="model" Enhance FormName="admin-form">
    <DataAnnotationsValidator EnableClientValidation="false" />
    <!-- This form uses server-side validation only -->
    ...
</EditForm>
```

When `EnableClientValidation="false"`, input components do not emit `data-val-*` attributes and `ValidationMessage`/`ValidationSummary` use the standard Blazor rendering (no `data-valmsg-for` / `data-valmsg-summary`). The JS library has nothing to find and the form behaves exactly as it does today.

#### MVC

MVC apps reference the standalone `aspnet-core-validation.js` file. The library auto-scans all forms on page load.

#### Shared HTML-level controls

**Per-button opt-out:** The standard `formnovalidate` HTML attribute skips validation for that submit action:

```html
<button type="submit">Save</button>                           <!-- validates -->
<button type="submit" formnovalidate>Save as Draft</button>   <!-- skips validation -->
```

**Per-field opt-out:** Fields without `DataAnnotations` attributes will not have `data-val` emitted and are ignored by the library.

### Scenario 7: Interactive render modes are not affected (Blazor)

Client-side validation targets static SSR only. When components render interactively (Blazor Server or Blazor WebAssembly), the existing Blazor validation pipeline handles validation through the live `EditContext` and C# logic running on the circuit or in WebAssembly.

**Behavior in interactive contexts:**

- **`data-val-*` attributes are not emitted.** The C# service layer detects that the form is rendering interactively and does not merge `data-val-*` attributes into input components. `ValidationMessage` and `ValidationSummary` use their standard interactive rendering (per-message `<div>` elements, no `data-valmsg-for`).
- **The JS validation library has no effect.** The library scans the DOM for `[data-val="true"]` elements. Since interactive forms have no `data-val-*` attributes, the scan finds nothing and no validation behavior is attached. No special detection of interactive mode is needed on the JS side — the C# layer is the single point of control.
- **Mixed pages work correctly.** On pages with both statically-rendered and interactive forms, the JS validation applies only to elements with `data-val-*` attributes (the static ones). Interactive forms continue to use the existing Blazor validation pipeline.

```razor
@* This page has both static and interactive forms *@

@* Static SSR form — gets data-val-* attributes and JS validation *@
<EditForm Model="contactModel" Enhance FormName="contact">
    <DataAnnotationsValidator />
    <InputText @bind-Value="contactModel.Name" />
    <ValidationMessage For="@(() => contactModel.Name)" />
    <button type="submit">Send</button>
</EditForm>

@* Interactive form — uses Blazor's EditContext validation, no data-val-* *@
<EditForm Model="chatModel" @rendermode="InteractiveServer">
    <DataAnnotationsValidator />
    <InputText @bind-Value="chatModel.Message" />
    <ValidationMessage For="@(() => chatModel.Message)" />
    <button type="submit">Send</button>
</EditForm>
```

### Scenario 8: Accessibility, standards, and events

The library sets `novalidate` on all participating forms at runtime (after the JS loads), preventing browsers from showing their native validation tooltips. Instead, the library uses `setCustomValidity()` to set validation state on each element, which means the browser-standard **Constraint Validation API** is populated with the current validation state.

**Accessibility (ARIA).** The library automatically manages ARIA attributes for screen reader support:

- **On the input:** `aria-invalid="true"` is set when a field has an error and removed when the error is cleared. `aria-describedby` is set to point to the associated validation message element (auto-generating an `id` on the message element if one is not already present).
- **On message elements:** `role="alert"` and `aria-live="assertive"` are set when the element is first discovered during DOM scanning. This causes screen readers to announce error messages as they appear.
- **Cleanup:** When a field error is cleared, `aria-invalid` and `aria-describedby` are removed from the input.

These ARIA attributes are set by the JS library at runtime, not server-rendered. Developer-specified ARIA attributes on inputs (via `AdditionalAttributes` in Blazor or HTML attributes in MVC) take precedence and are not overwritten.

**Validation message replacement.** By default, the library replaces the validation message element's text content with the error message. Setting `data-valmsg-replace="false"` on a message element preserves its existing content and only toggles CSS classes.

**For third-party library authors and advanced scenarios**, any validated element exposes standard Constraint Validation API properties:

```javascript
const input = document.querySelector('input[name="Email"]');
input.validity.valid;          // false
input.validity.customError;    // true
input.validationMessage;       // "The Email field is required."
input.willValidate;            // true
```

Third-party libraries can listen for the standard `invalid` event, read `ValidityState`, and build custom UI without depending on our library's internals.

**Form validation event.** After a full form validation completes (triggered by form submission), the library dispatches a `validationcomplete` custom event on the form element. The event bubbles, enabling document-level listeners:

```javascript
document.addEventListener('validationcomplete', (e) => {
    console.log('Form:', e.target);           // The <form> element
    console.log('Valid:', e.detail.valid);     // true or false
});
```

This enables integration with CSS frameworks that need a class on the form after validation (e.g., Bootstrap's `.was-validated`):

```javascript
document.addEventListener('validationcomplete', (e) => {
    e.target.classList.add('was-validated');
});
```

### Scenario 9: Custom validation attributes with client-side support

Custom `ValidationAttribute` subclasses can have client-side validation in both Blazor and MVC. The pattern has two parts: a **server-side adapter** (different API per framework) that emits `data-val-*` attributes, and a **JavaScript provider** (shared, same API for both frameworks) that runs the validation logic in the browser.

#### JavaScript provider registration

Custom client-side validators are registered via the validation API object (`Blazor.validation` in Blazor, `window.__aspnetValidation` in MVC). The provider function is identical for both:

```javascript
// Blazor: Blazor.validation.addProvider(...)
// MVC:    window.__aspnetValidation.addProvider(...)
addProvider('notequalto', (value, element, params) => {
    const otherName = params.other.replace('*.', '');
    const form = element.closest('form');
    const otherField = form?.querySelector(`[name$=".${otherName}"], [name="${otherName}"]`);
    return !otherField || value !== otherField.value;
});
```

The provider function receives the field value, the DOM element, and the parsed parameters from `data-val-notequalto-*` attributes. It returns `true` (valid), `false` (invalid, uses the default error message from the `data-val-notequalto` attribute), or a `string` (invalid, with a custom error message).

`addProvider` has **overriding semantics** — calling it with a name that already exists replaces the previous provider. This allows developers to override built-in providers (e.g., to customize the `email` regex) by registering their own provider with the same name.

#### Server-side: Blazor adapter

In Blazor, the attribute emits `data-val-*` attributes via the `IClientValidationAdapter` interface. There are two options:

**Option A — The attribute implements `IClientValidationAdapter` directly** (simplest):

```csharp
public class NotEqualToAttribute : ValidationAttribute, IClientValidationAdapter
{
    public string OtherProperty { get; }
    public NotEqualToAttribute(string otherProperty) => OtherProperty = otherProperty;

    protected override ValidationResult? IsValid(object? value, ValidationContext context) { /* server logic */ }

    public void AddClientValidation(in ClientValidationContext context, string errorMessage)
    {
        context.MergeAttribute("data-val", "true");
        context.MergeAttribute("data-val-notequalto", errorMessage);
        context.MergeAttribute("data-val-notequalto-other", "*." + OtherProperty);
    }
}
```

**Option B — Register a separate adapter via DI** (when you don't control the attribute type):

```csharp
// In Program.cs
builder.Services.AddClientValidationAdapter<NotEqualToAttribute>(
    attr => new NotEqualToClientAdapter(attr));
```

`ClientValidationContext.MergeAttribute()` uses **first-wins** semantics: if a `data-val-*` key already exists, subsequent writes are ignored. This matches MVC's `TagBuilder.MergeAttribute()` behavior. Custom adapters should use unique rule name prefixes to avoid collisions with built-in rules.

### Scenario 10: Drop-in replacement for jQuery unobtrusive validation (MVC)

An existing ASP.NET Core MVC app removes the jQuery validation stack and replaces it with the new library. **No C# changes required.**

**Before:**

```html
<script src="~/lib/jquery/jquery.min.js"></script>
<script src="~/lib/jquery-validation/jquery.validate.min.js"></script>
<script src="~/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js"></script>
```

**After:**

```html
<script src="~/lib/aspnet-core-validation/aspnet-core-validation.js"></script>
```

**What happens:**

- Including the script is all that's needed. The library scans the DOM on page load and wires validation automatically.
- All `data-val-*` attributes generated by MVC tag helpers (`asp-for`, `asp-validation-for`, `asp-validation-summary`) are consumed identically.
- The same CSS classes are toggled (`input-validation-error`, `field-validation-error`, `validation-summary-errors`).
- `data-valmsg-replace="true|false"` is respected.
- `formnovalidate` on submit buttons is honored.
- `[Remote]` attribute validation works out of the box — the library uses the modern `fetch` API (instead of jQuery's `$.ajax`) to send requests, with per-element caching.

> ⚠️ **Blazor SSR:** Remote validation is **not supported**. Using `[Remote]` on a model in a Blazor SSR form throws `NotSupportedException` at render time. Custom async JavaScript providers in Blazor mode throw at registration time.

#### Migrating custom validators from jQuery unobtrusive

MVC apps that switch from jQuery unobtrusive validation to the new library need to rewrite their JavaScript-side custom validator registrations. The C# server-side (`IClientModelValidator`) is unchanged.

**Before (jQuery unobtrusive — two JS registrations):**

```javascript
// 1. Register the validation method with jquery-validation
$.validator.addMethod('notequalto', function (value, element, params) {
    var otherField = $(params).val();
    return this.optional(element) || value !== otherField;
});

// 2. Register the adapter with jquery-validation-unobtrusive
$.validator.unobtrusive.adapters.add('notequalto', ['other'], function (options) {
    options.rules['notequalto'] = '#' + options.params.other.replace('*.', '');
    options.messages['notequalto'] = options.message;
});
```

**After (new library — one registration using the same provider as Scenario 9):**

```javascript
window.__aspnetValidation.addProvider('notequalto', (value, element, params) => {
    // Same provider function as shown in Scenario 9
    const otherName = params.other.replace('*.', '');
    const form = element.closest('form');
    const otherField = form?.querySelector(`[name$=".${otherName}"], [name="${otherName}"]`);
    return !otherField || value !== otherField.value;
});
```

Key differences:

- **One registration instead of two.** The adapter/method split from jQuery unobtrusive is eliminated. The `addProvider` callback receives the pre-parsed parameters directly (the library handles attribute parsing).
- **No jQuery dependency.** Use standard DOM APIs (`querySelector`, `.value`) instead of jQuery selectors and `$(...)`.
- **No `this.optional(element)` pattern.** The library automatically skips validation on empty values for all rules except `required`, matching the jQuery convention without requiring the developer to handle it.
- **Return values.** Return `true` (valid), `false` (invalid with default message), or a `string` (invalid with custom message). No separate `options.messages` registration needed—the error message is embedded in the `data-val-notequalto` attribute and used as the default.

#### Dynamic content: replacing `$.validator.unobtrusive.parse()`

MVC apps that dynamically inject form content (e.g., via AJAX partial views) use `$.validator.unobtrusive.parse()` to wire validation on new elements. The replacement is `scan()`, which is available on both the Blazor and MVC APIs:

```javascript
// MVC
window.__aspnetValidation.scan('#dynamic-container');
window.__aspnetValidation.scan(document.getElementById('dynamic-container'));

// Blazor (called automatically on enhancedload, but can also be called manually)
Blazor.validation.scan(document.getElementById('dynamic-container'));
```

`scan()` accepts a CSS selector string, an `Element`, or a `ParentNode`. When called with no arguments, it scans the entire document.

#### Common jQuery API equivalents

| jQuery Validation API | New Library Equivalent |
|---|---|
| `$('form').valid()` | `window.__aspnetValidation.validateForm(form)` |
| `$('form').validate().element(input)` | `window.__aspnetValidation.validateField(input)` |
| `$.validator.unobtrusive.parse(selector)` | `window.__aspnetValidation.scan(selector)` |
| `$.validator.unobtrusive.adapters.add(...)` | `window.__aspnetValidation.addProvider(name, fn)` |
| `$.validator.addMethod(...)` | `window.__aspnetValidation.addProvider(name, fn)` |
| `$.validator.defaults.ignore` | Hidden fields are auto-skipped; no configuration needed |

## Assumptions

- **Browser support:** The library targets the same browsers as ASP.NET Core Blazor — current versions of Chrome, Edge, Firefox, and Safari (see [Blazor supported platforms](https://learn.microsoft.com/aspnet/core/blazor/supported-platforms)). No IE11 or legacy browser support is provided. All required browser APIs (Constraint Validation API, `WeakMap`, `CSS.escape()`, `CustomEvent`) are available in these browsers.
- **`data-val-*` protocol stability:** The `data-val-*` attribute protocol used by MVC's unobtrusive validation is stable and will not change in a breaking way.
- **Progressive enhancement:** Client-side validation is a UX enhancement. Server-side validation always remains authoritative. There is a brief window between page load and script initialization where the browser's native validation (or no validation) is active; any submission during that window is handled by server-side validation.
- **`RemoteAttribute` is MVC-specific:** `[Remote]` belongs to the `Microsoft.AspNetCore.Mvc` namespace and is not expected on Blazor models.

## References

- **GitHub issue:** [dotnet/aspnetcore#51040 — Create server rendered forms with client validation using Blazor without a circuit](https://github.com/dotnet/aspnetcore/issues/51040)
- **Validation localization proposal:** [dotnet/aspnetcore#65539 — Validation localization support for Microsoft.Extensions.Validation](https://github.com/dotnet/aspnetcore/issues/65539) — Design for `ErrorMessageProvider` and `DisplayNameProvider` delegates used by the client validation service for localized messages.
- **Original proposal (javiercn):** [dotnet/aspnetcore#51040 (comment)](https://github.com/dotnet/aspnetcore/issues/51040#issuecomment-3706000376) — Detailed proposal for core validation library, unobtrusive adapter, Blazor wiring layer, and Constraint Validation API integration.
- **Prior art — aspnet-client-validation:** [github.com/haacked/aspnet-client-validation](https://github.com/haacked/aspnet-client-validation) — Community library providing jQuery-free unobtrusive validation. Informed the provider pattern and validation timing design.
- **Prior art — WasmClientValidation:** [github.com/DamianEdwards/WasmClientValidation](https://github.com/DamianEdwards/WasmClientValidation) — Experiment exploring WebAssembly-based client validation (community feedback favored JS approach).
- **MDN — Constraint Validation API:** [developer.mozilla.org/docs/Web/API/Constraint_validation](https://developer.mozilla.org/docs/Web/API/Constraint_validation)
- **MDN — ValidityState:** [developer.mozilla.org/docs/Web/API/ValidityState](https://developer.mozilla.org/docs/Web/API/ValidityState)
- **MDN — setCustomValidity:** [developer.mozilla.org/docs/Web/API/HTMLObjectElement/setCustomValidity](https://developer.mozilla.org/docs/Web/API/HTMLObjectElement/setCustomValidity)
- **MDN — HTMLFormElement.noValidate:** [developer.mozilla.org/docs/Web/API/HTMLFormElement/noValidate](https://developer.mozilla.org/docs/Web/API/HTMLFormElement/noValidate)
- **Blazor enhanced navigation guidance:** [learn.microsoft.com/aspnet/core/blazor/javascript-interoperability/static-server-rendering](https://learn.microsoft.com/aspnet/core/blazor/javascript-interoperability/static-server-rendering)
- **Blazor forms validation docs:** [learn.microsoft.com/aspnet/core/blazor/forms/validation](https://learn.microsoft.com/aspnet/core/blazor/forms/validation)
- **Browser compatibility — Constraint Validation:** [caniuse.com/constraint-validation](https://caniuse.com/constraint-validation)
- **Internal research documents:** `features/validation-client-side/01-research.md` through `12-blazor-async-investigation.md`
