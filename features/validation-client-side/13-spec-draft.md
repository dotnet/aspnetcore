# Summary

This document proposes adding **first-party, zero-dependency client-side form validation** to ASP.NET Core that works for **Blazor Web Apps** (static SSR with both enhanced and non-enhanced forms) and **MVC / Razor Pages**, closing a long-standing gap where Blazor forms require interactivity for client validation and MVC relies on a large and hard-to-maintain jQuery-based library bundle.

We propose creating a new minimal **JavaScript validation library** (target: ≤ 5 KB Brotli-compressed, no dependencies) that understands the existing `data-val-*` attribute protocol and uses the browser-native **Constraint Validation API** as its validation state mechanism. For Blazor, the validation JS is **bundled into `blazor.web.js`** and the C# service layer is **enabled by default via `AddRazorComponents()`**. For MVC and Razor Pages, the JS library is compatible with the existing HTML generation and can serve as a **drop-in replacement** for the jQuery validation stack.

## Goals

- **Enable client-side validation for Blazor SSR without a circuit.** Today, Blazor Web Apps rendered via static SSR have no client-side validation—every validation check requires a full server round-trip. This feature adds immediate, in-browser validation feedback using standard `DataAnnotations`, without requiring Blazor Server interactivity or WebAssembly. Client-side validation is **enabled by default** when `AddRazorComponents()` is called—all existing `<InputText>`, `<InputNumber>`, etc. components automatically emit `data-val-*` attributes on statically-rendered forms, and the validation JS (bundled into `blazor.web.js`) validates on the client. This works for both enhanced (`Enhance`) and non-enhanced SSR forms.

- **Provide a zero-dependency JavaScript validation library (≤ 5 KB Brotli).** The current MVC client-side validation stack depends on jQuery (87 KB), jquery-validate (23 KB), and jquery-validation-unobtrusive (8 KB). This feature delivers a JavaScript validation library with no external dependencies that understands the same `data-val-*` attribute protocol. It uses the browser-native Constraint Validation API (`setCustomValidity`, `ValidityState`, `checkValidity`) as the validation state mechanism and extensibility surface, enabling third-party libraries to read standardized validity state without coupling to our internals. For Blazor, the JS library is bundled into `blazor.web.js`.

- **Work across the Blazor enhanced navigation lifecycle.** Blazor's enhanced navigation patches the DOM without full page loads. The validation library automatically re-scans for new or changed forms after each navigation, handles DOM element reuse, and intercepts form submission so validation runs before the enhanced navigation fetch. No developer action is required beyond the initial setup.

- **Support extensibility for custom validators.** Developers can register custom validation providers on the JavaScript side and custom adapters on the C# side, enabling the same custom-attribute-to-client-rule pipeline that MVC provides but with fewer layers of indirection.

- **Be compatible with ASP.NET Core MVC / Razor Pages as a drop-in replacement for jQuery unobtrusive validation.** The JS library reads the same `data-val-*` attributes that MVC tag helpers already emit and uses the same CSS class names (`input-validation-error`, `field-validation-error`, `validation-summary-errors`, etc.) and `data-valmsg-for` / `data-valmsg-summary` conventions. We build the JS library and ensure MVC compatibility; the MVC team owns distribution, integration, and testing for MVC scenarios.

## Non-goals

- **Full parity with the jQuery unobtrusive validation rule ecosystem.** The feature targets a focused subset of validators aligned with core `System.ComponentModel.DataAnnotations` attributes (Required, StringLength, MinLength, MaxLength, Range, RegularExpression, Email, Url, Phone, CreditCard, Compare, FileExtensions). Niche MVC-specific validators and any third-party jquery-validation plugins are not replicated. Custom validators can be added via the extensibility API.

- **Remote / async server-backed validation in Blazor SSR.** MVC's `[Remote]` attribute pattern (where the server is queried via AJAX during client-side validation) is supported only when used from MVC apps. In Blazor SSR, async and remote validation is **not supported in this release**. `RemoteAttribute` on Blazor models will throw `NotSupportedException` at render time. Custom async JavaScript providers registered in Blazor mode will throw at registration time. This is a deliberate constraint — async validation in Blazor conflicts with the mechanism through which enhanced navigation intercepts form submission. Making these compatible would require more extensive changes which are out-of-scope here.

- **Client-side validation for interactive render modes.** This feature targets static SSR forms only. When a form is rendered interactively (Blazor Server or Blazor WebAssembly), validation is handled by the existing Blazor interactive validation pipeline (via `EditContext` and C# validation logic running on the circuit or in WebAssembly). The `data-val-*` attributes should not be emitted on interactive forms, and the JS validation code must not run or modify the DOM in interactive rendering contexts. This ensures no conflicts between the two validation approaches.

- **Replacing browser-native validation UI.** The library suppresses native browser validation tooltips and manages its own error display via CSS classes and message target elements. However, it does not attempt to provide a rich validation UI framework (tooltips, animations, etc.). Richer UI is left to application CSS and/or third-party libraries that can read the Constraint Validation API state.

- **Source generator for validation metadata.** The C# attribute discovery uses reflection (`Type.GetProperty`, `GetCustomAttributes<ValidationAttribute>`) with per-field caching. This is trimming-compatible using the same `[DynamicallyAccessedMembers]` annotations and `[UnconditionalSuppressMessage]` suppressions as the existing `DataAnnotationsValidator` — model types are application code and are not trimmed by default. A source-generator approach that avoids reflection entirely is a potential future optimization but is not part of this feature.

- **Custom element for validation targets.** The original proposal included an optional custom element (e.g., `<asp-validation-message>`) using `ElementInternals` for message rendering and built-in ARIA semantics. This has been deferred in favor of the simpler approach: plain `<span data-valmsg-for>` elements (compatible with both MVC and Blazor conventions) with ARIA attributes managed directly by the JS library. The custom element approach may be revisited in a future release if there is demand for a more standards-based abstraction.

- **Legacy ASP.NET MVC 5 support.** The drop-in replacement targets ASP.NET Core MVC only. While the `data-val-*` protocol is largely the same across ASP.NET MVC 5 and ASP.NET Core MVC, differences in anti-forgery token handling, remote validation URL generation, and other details mean that legacy MVC 5 apps are not a tested or supported scenario.

## Proposed solution

The solution consists of two main parts:

1. **A JavaScript validation library** bundled into `blazor.web.js` (and also available as a standalone file for MVC) that scans the DOM for `data-val-*` attributes, validates form fields on user interaction and submission, displays error messages, and integrates with the Constraint Validation API. The library auto-detects whether it's running in a Blazor or MVC context and adapts accordingly (e.g., re-scanning after Blazor enhanced navigation, or providing a `parse()` API for MVC dynamic content).

2. **A Blazor C# service layer**, enabled by default via `AddRazorComponents()`, that causes existing Blazor input components (`InputText`, `InputNumber`, `InputSelect`, etc.) to automatically emit `data-val-*` attributes derived from `DataAnnotations` on the model when rendering in a static SSR context. The feature is active only for statically-rendered forms — interactive render modes are not affected. Per-form opt-out is available via a parameter on `DataAnnotationsValidator`.

Client-side validation is a **progressive enhancement**: forms are fully functional before the script loads, and server-side validation always remains authoritative. The client library improves UX by providing immediate feedback without a round-trip.

**Supported modes:**
- Blazor SSR with enhanced forms (`<EditForm Enhance>`)
- Blazor SSR with non-enhanced forms (`<EditForm>` without `Enhance`)
- MVC / Razor Pages forms

### Scenario 1: Basic Blazor SSR form with client-side validation

The simplest and most common scenario. A statically-rendered Blazor form gets immediate client-side validation without enabling interactive render modes. **No additional setup is required** — client-side validation is enabled by default when using `AddRazorComponents()`.

**Program.cs — standard Blazor setup (no extra calls needed):**
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
- Because `AddRazorComponents()` registers the client validation service, all `InputBase<T>`-derived components (`InputText`, `InputNumber`, `InputSelect`, `InputDate`, `InputTextArea`, `InputCheckbox`, `InputRadioGroup`) automatically emit `data-val-*` attributes based on the model's `DataAnnotations` when rendering in a static SSR context.
- `<ValidationMessage>` renders a `<span data-valmsg-for="...">` target for per-field errors.
- `<ValidationSummary>` renders a `<div data-valmsg-summary="true">` container for the error list.
- The validation JS (bundled in `blazor.web.js`) scans the DOM, finds the annotated elements, and wires validation.
- **On blur/change:** the field is validated and errors are shown or cleared.
- **On typing:** existing errors are cleared in real-time (but new errors are not shown until blur, to avoid flashing red while the user types).
- **On submit:** all fields are validated. If any are invalid, submission is blocked and errors are displayed. If all are valid, the form submits normally via enhanced navigation.
- **Server-side validation** via `DataAnnotationsValidator` always runs on the server and remains authoritative.

> **Note:** `InputFile` and `InputRadio` do not extend `InputBase<T>` and do not automatically emit `data-val-*` attributes. `InputRadio` is validated through its parent `InputRadioGroup<T>` which does extend `InputBase<T>`. File validation is server-side only.

### Scenario 2: Supported validation rules

The following `DataAnnotations` attributes are supported out of the box. The mapping uses the same `data-val-*` attribute protocol as MVC, ensuring full compatibility:

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

The `number` rule is a special case: it is not driven by a `DataAnnotation` attribute. MVC's `NumericClientModelValidatorProvider` automatically emits `data-val-number` for `float`, `double`, and `decimal` properties. The JS library includes a `number` provider for MVC compatibility. Blazor does not emit `data-val-number` — `InputNumber<T>` renders `type="number"` which provides browser-native numeric enforcement.

**Error messages** are resolved on the server at render time and embedded in the `data-val-*` attributes as static strings. By default, `ValidationAttribute.FormatErrorMessage()` is used with the display name derived from `[Display(Name = "...")]`, `[DisplayName("...")]`, or the property name as fallback. When validation localization is enabled, the same localization providers are used for both client-side and server-side messages — see [Scenario 3: Localized error messages](#scenario-3-localized-error-messages).

**Nested models** are fully supported. A property like `model.Address.Street` produces validation attributes with the correct dotted-path field name, and `<ValidationMessage For="@(() => model.Address.Street)" />` renders the matching `data-valmsg-for` target.

### Scenario 3: Localized error messages

When validation localization is enabled in the app (via the `Microsoft.Extensions.Validation.Localization` package, see [#65539](https://github.com/dotnet/aspnetcore/issues/65539)), client-side validation displays the **same localized messages** as server-side validation. This ensures a consistent user experience regardless of whether a validation error is caught on the client or the server.

**How it works:** Error messages and display names are resolved on the server at render time—before the HTML is sent to the browser. The client validation service uses `ValidationOptions.ErrorMessageProvider` and `ValidationOptions.DisplayNameProvider` (the same delegates used by server-side validation) to produce localized, formatted error messages. These messages are then embedded in the `data-val-*` attributes as static strings. No localization happens in the browser; the JS library simply displays whatever message is in the attribute.

**Setup — enable validation localization with a shared resource file:**
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents();     // Includes client-side validation
builder.Services.AddValidation();
builder.Services.AddValidationLocalization<SharedValidationMessages>();
```

**Model — no per-attribute resource annotations needed:**
```csharp
public class RegisterModel
{
    [Required]                                   // Uses localized "required" template
    [EmailAddress]                               // Uses localized "email" template
    [Display(Name = "EmailAddress")]             // Display name key for localization
    public string Email { get; set; } = "";

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [Display(Name = "Password")]
    public string Password { get; set; } = "";
}
```

**Resource file (`SharedValidationMessages.fr.resx`):**
```
EmailAddress       → Adresse e-mail
Password           → Mot de passe
RequiredAttribute  → Le champ {0} est obligatoire.
StringLengthAttribute → Le champ {0} doit contenir entre {2} et {1} caractères.
EmailAddressAttribute → Le champ {0} n'est pas une adresse e-mail valide.
```

**Rendered HTML (for a French-language request):**
```html
<input type="text" name="model.Email"
       data-val="true"
       data-val-required="Le champ Adresse e-mail est obligatoire."
       data-val-email="Le champ Adresse e-mail n'est pas une adresse e-mail valide." />
<span data-valmsg-for="model.Email" class="field-validation-valid"></span>
```

**Result:** When the user submits the form with an empty email field:
- **Client-side** (JS library reads `data-val-required`): displays "Le champ Adresse e-mail est obligatoire."
- **Server-side** (validation pipeline uses the same `ErrorMessageProvider`): returns "Le champ Adresse e-mail est obligatoire."
- Both messages are identical because they come from the same localization provider and resource file.

This works because the client validation service resolves messages through the same `ValidationOptions` pipeline as server-side validation:
1. If `ValidationOptions.ErrorMessageProvider` is configured (by `AddValidationLocalization()`), it is used to produce the localized, formatted message.
2. If `ValidationOptions.DisplayNameProvider` is configured, it resolves localized display names (e.g., "Adresse e-mail" instead of "Email").
3. If neither is configured (no localization enabled), the service falls back to `ValidationAttribute.FormatErrorMessage()` with the default display name, exactly as in the non-localized scenario.

### Scenario 4: Validation timing and UX behavior

The library uses a smart validation timing strategy to avoid poor "everything is red immediately" UX:

| User action | Behavior |
|---|---|
| **Typing** (`input` event) | Only **clears** existing errors. Does **not** show new errors while the user is actively typing. |
| **Leaving a field** (`change`/`blur` event) | **Shows or clears** errors. This is the primary trigger for displaying validation errors. |
| **Submitting the form** | **Validates all fields.** Blocks submission if any field is invalid. Updates the validation summary. |
| **Resetting the form** | **Clears all validation state.** CSS classes, error messages, validation summary, ARIA attributes, and the "has been invalid" flag are all reset. Fields return to their pristine state (typing does not trigger validation until the next blur/submit). |

Once a field has been marked invalid, subsequent keystrokes will clear the error in real-time as soon as the input becomes valid—providing immediate feedback as the user corrects the issue.

**Form reset** is handled by listening for the `reset` event on forms. This provides MVC parity (jquery-validation-unobtrusive supports reset) and works for Blazor SSR forms. Note that Blazor's broader form-reset story (resetting `EditContext` modification/validation state in interactive modes) is a separate gap outside the scope of this feature.

### Scenario 5: Enhanced navigation and streaming (Blazor)

Forms that appear after a Blazor enhanced navigation or streaming rendering update are **automatically wired for validation** with no developer action required.

**How it works for the developer:** The validation JS (bundled in `blazor.web.js`) subscribes to Blazor's `enhancedload` event and re-scans the DOM after each navigation or streaming update. New form fields are discovered and wired automatically. Existing fields whose validation attributes changed (e.g., because the server re-rendered with different rules) are detected and re-registered.

**Multiple forms on a page** are supported. Each form is validated independently—submitting one form does not trigger validation on another.

### Scenario 6: Enabled by default, per-form opt-out

**Blazor — enabled by default:** Client-side validation is automatically enabled for all statically-rendered forms when `AddRazorComponents()` is called. No additional service registration or component is needed. Existing apps that upgrade to .NET 10 get client-side validation automatically.

**Per-form opt-out:** To disable client-side validation on a specific form, use a parameter on `DataAnnotationsValidator`:

```razor
<EditForm Model="model" Enhance FormName="admin-form">
    <DataAnnotationsValidator EnableClientValidation="false" />
    <!-- This form uses server-side validation only -->
    ...
</EditForm>
```

When `EnableClientValidation="false"`, input components do not emit `data-val-*` attributes and `ValidationMessage`/`ValidationSummary` use the standard Blazor rendering (no `data-valmsg-for` / `data-valmsg-summary`). The JS library has nothing to find and the form behaves exactly as it does today.

**MVC — include the script:** MVC apps reference the standalone `aspnet-validation.js` file. The library auto-scans all forms on page load. MVC distribution and integration is owned by the MVC team.

**Per-button opt-out:** The standard `formnovalidate` HTML attribute skips validation for that submit action:
```html
<button type="submit">Save</button>                           <!-- validates -->
<button type="submit" formnovalidate>Save as Draft</button>   <!-- skips validation -->
```

**Per-field opt-out:** Fields without `DataAnnotations` attributes will not have `data-val` emitted and are ignored by the library.

### Scenario 7: Interactive render modes are not affected

Client-side validation targets static SSR only. When components render interactively (Blazor Server or Blazor WebAssembly), the existing Blazor validation pipeline handles validation through the live `EditContext` and C# logic running on the circuit or in WebAssembly.

**Behavior in interactive contexts:**
- **`data-val-*` attributes are not emitted.** The C# service layer detects that the form is rendering interactively and does not merge `data-val-*` attributes into input components. `ValidationMessage` and `ValidationSummary` use their standard interactive rendering (per-message `<div>` elements, no `data-valmsg-for`).
- **The JS validation code does not run.** The validation JS does not scan or modify the DOM for forms in interactive rendering contexts. This ensures no conflicts between the JS-based client validation and Blazor's interactive validation (which uses C# `EditContext.Validate()` via the circuit or WebAssembly).
- **Mixed pages work correctly.** On pages with both statically-rendered and interactive forms, client-side JS validation applies only to the static forms. Interactive forms continue to use the existing Blazor validation pipeline.

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

### Scenario 8: Suppressed browser validation, Constraint Validation API, and accessibility

The library sets `novalidate` on all participating forms at runtime (after the JS loads), preventing browsers from showing their native validation tooltips. Instead, the library uses `setCustomValidity()` to set validation state on each element, which means the browser-standard Constraint Validation API is populated with the current validation state.

**Accessibility (ARIA).** The library automatically manages ARIA attributes for screen reader support:

- **On the input:** `aria-invalid="true"` is set when a field has an error and removed when the error is cleared. `aria-describedby` is set to point to the associated validation message element (auto-generating an `id` on the message element if one is not already present).
- **On message elements:** `role="alert"` and `aria-live="assertive"` are set when the element is first discovered during DOM scanning. This causes screen readers to announce error messages as they appear.
- **Cleanup:** When a field error is cleared, `aria-invalid` and `aria-describedby` are removed from the input.

These ARIA attributes are set by the JS library at runtime, not server-rendered. Developer-specified ARIA attributes on inputs (via `AdditionalAttributes` in Blazor or HTML attributes in MVC) take precedence and are not overwritten.

**For third-party library authors and advanced scenarios**, any validated element exposes standard Constraint Validation API properties:

```javascript
const input = document.querySelector('input[name="Email"]');
input.validity.valid;          // false
input.validity.customError;    // true
input.validationMessage;       // "The Email field is required."
input.willValidate;            // true
```

Third-party libraries can listen for the standard `invalid` event, read `ValidityState`, and build custom UI without depending on our library's internals.

### Scenario 9: Custom validation attributes with client-side support

Custom `ValidationAttribute` subclasses can have client-side validation in both Blazor and MVC. The pattern has two parts: a **server-side adapter** (different API per framework) that emits `data-val-*` attributes, and a **JavaScript provider** (shared, same API for both frameworks) that runs the validation logic in the browser.

#### JavaScript provider registration (shared by Blazor and MVC)

Regardless of server framework, custom client-side validators are registered the same way:

```javascript
window.__aspnetValidation.addProvider('notequalto', (value, element, params) => {
    const otherName = params.other.replace('*.', '');
    const form = element.closest('form');
    const otherField = form?.querySelector(`[name$=".${otherName}"], [name="${otherName}"]`);
    return !otherField || value !== otherField.value;
});
```

The provider function receives the field value, the DOM element, and the parsed parameters from `data-val-notequalto-*` attributes. It returns `true` (valid), `false` (invalid, uses the default error message from the `data-val-notequalto` attribute), or a `string` (invalid, with a custom error message).

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

#### Server-side: MVC adapter

In MVC, the attribute emits `data-val-*` attributes via the existing `IClientModelValidator` interface. This is unchanged from current MVC:

```csharp
public class NotEqualToAttribute : ValidationAttribute, IClientModelValidator
{
    public string OtherProperty { get; }
    public NotEqualToAttribute(string otherProperty) => OtherProperty = otherProperty;

    protected override ValidationResult? IsValid(object? value, ValidationContext context) { /* server logic */ }

    public void AddValidation(ClientModelValidationContext context)
    {
        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-notequalto", GetErrorMessage(context));
        MergeAttribute(context.Attributes, "data-val-notequalto-other", "*." + OtherProperty);
    }
}
```

#### Dual-framework support (shared libraries)

For library authors who need the same attribute to work in both Blazor and MVC apps, the attribute can implement both interfaces:

```csharp
public class NotEqualToAttribute : ValidationAttribute,
    IClientValidationAdapter,    // Blazor
    IClientModelValidator        // MVC
{
    public string OtherProperty { get; }
    public NotEqualToAttribute(string otherProperty) => OtherProperty = otherProperty;

    protected override ValidationResult? IsValid(object? value, ValidationContext context) { /* ... */ }

    // Blazor path
    public void AddClientValidation(in ClientValidationContext context, string errorMessage)
    {
        context.MergeAttribute("data-val", "true");
        context.MergeAttribute("data-val-notequalto", errorMessage);
        context.MergeAttribute("data-val-notequalto-other", "*." + OtherProperty);
    }

    // MVC path
    public void AddValidation(ClientModelValidationContext context)
    {
        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-notequalto", GetErrorMessage(context));
        MergeAttribute(context.Attributes, "data-val-notequalto-other", "*." + OtherProperty);
    }
}
```

Both paths emit the same `data-val-*` attributes, so the same JavaScript provider works for both.

### Scenario 10: Drop-in replacement for jQuery unobtrusive validation in MVC

An existing ASP.NET Core MVC app removes the jQuery validation stack and replaces it with the new library. **No C# changes required.**

**Before:**
```html
<script src="~/lib/jquery/jquery.min.js"></script>
<script src="~/lib/jquery-validation/jquery.validate.min.js"></script>
<script src="~/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js"></script>
```

**After:**
```html
<script src="~/lib/aspnet-validation/aspnet-validation.js"></script>
```

**What happens:**
- The library auto-detects MVC mode and scans the DOM on page load.
- All `data-val-*` attributes generated by MVC tag helpers (`asp-for`, `asp-validation-for`, `asp-validation-summary`) are consumed identically.
- The same CSS classes are toggled (`input-validation-error`, `field-validation-error`, `validation-summary-errors`).
- `data-valmsg-replace="true|false"` is respected.
- `formnovalidate` on submit buttons is honored.
- `[Remote]` attribute validation works out of the box (the MVC wiring registers the remote provider automatically).

**Size reduction:** 118 KB (jQuery + validation libs, minified) → ≤ 4 KB (Brotli-compressed).

> **Distribution:** We build the JS library and ensure MVC compatibility. The MVC team owns distribution, integration, and testing for MVC scenarios. MVC apps reference the standalone `aspnet-validation.js` file (delivery mechanism TBD by MVC team).

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

**After (new library — one JS registration):**
```javascript
window.__aspnetValidation.addProvider('notequalto', (value, element, params) => {
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

MVC apps that dynamically inject form content (e.g., via AJAX partial views) use `$.validator.unobtrusive.parse()` to wire validation on new elements. The replacement is:

```javascript
// Before
$.validator.unobtrusive.parse('#dynamic-container');

// After
window.__aspnetValidation.parse('#dynamic-container');
// Also accepts an Element or ParentNode:
window.__aspnetValidation.parse(document.getElementById('dynamic-container'));
```

### Scenario 11: MVC remote validation

In MVC mode, the `[Remote]` attribute works out of the box:

```csharp
public class RegisterModel
{
    [Remote(action: "VerifyEmail", controller: "Account")]
    public string Email { get; set; }
}
```

The library sends a `fetch()` request to the specified URL, supports GET (default) and POST, caches results per-element to avoid redundant requests, and interprets the response (`true`/`"true"` = valid, `false` = invalid, any other string = custom error message). On network error, the field is treated as valid to avoid blocking the user.

> ⚠️ **Blazor SSR:** Remote validation is **not supported**. Using `[Remote]` on a model in a Blazor SSR form throws `NotSupportedException` at render time. Custom async JavaScript providers in Blazor mode throw at registration time.

## Open questions

### Open Question 1: Interactive mode detection mechanism

**Context:** The C# service layer needs to detect whether a form is rendering in a static SSR context vs. an interactive context to decide whether to emit `data-val-*` attributes. This must work correctly for apps with islands of interactivity — a single page may contain both static SSR forms and interactive forms, and each must behave independently.

**Research findings:** The Blazor component model provides several signals for detecting SSR vs. interactive context:

| Signal | SSR (static) | Interactive | Prerendering of interactive component |
|---|---|---|---|
| `ComponentBase.AssignedRenderMode` | `null` | `InteractiveServerRenderMode` / `InteractiveWebAssemblyRenderMode` / `InteractiveAutoRenderMode` | Same as interactive (the mode instance has `Prerender = true`) |
| `ComponentBase.RendererInfo.IsInteractive` | `false` | `true` | `false` (during prerender pass), `true` (after activation) |
| `EditForm`'s cascaded `FormMappingContext` | non-null (SSR form handling) | `null` | non-null (during prerender pass) |

**Key nuances for islands of interactivity:**
- Render modes do not cascade globally — they create **render mode boundaries**. An SSR page can contain `<EditForm @rendermode="InteractiveServer">`, and all children inside that boundary (including `InputText`, `ValidationMessage`, etc.) see `AssignedRenderMode = InteractiveServerRenderMode`.
- A component cannot have *both* a caller-specified and a class-level `@rendermode` — Blazor throws at runtime if both are set.
- During **prerendering** of an interactive component, `AssignedRenderMode` is already set to the interactive mode, but `RendererInfo.IsInteractive` is `false`. After the circuit/WebAssembly activates, `RendererInfo.IsInteractive` becomes `true` and the component re-renders.

**Recommended approach — C# layer (emit `data-val-*` only for SSR):**

The check should go in `InputBase<T>.MergeClientValidationAttributes()` (or its caller), gating on whether the component is rendering in a static SSR context. The cleanest signal is:

```
Do NOT emit data-val-* if AssignedRenderMode is not null
```

This covers all interactive cases including prerendering: when an interactive component prerenders, it will soon be replaced by the live interactive version, so emitting `data-val-*` during the brief prerender pass would be wasteful and potentially conflicting. `AssignedRenderMode == null` precisely means "this component is purely static SSR, with no interactive activation coming."

This approach works correctly for islands:
- A static `<EditForm>` on an SSR page → `AssignedRenderMode` is `null` on `InputBase` children → `data-val-*` emitted ✓
- An `<EditForm @rendermode="InteractiveServer">` on the same page → `AssignedRenderMode` is `InteractiveServerRenderMode` on `InputBase` children → `data-val-*` NOT emitted ✓
- A global `@rendermode InteractiveServer` on a page → all components see the mode → no `data-val-*` emitted ✓

The same check should apply in `ValidationMessage<T>` and `ValidationSummary` to decide whether to render `data-valmsg-for` / `data-valmsg-summary` or the standard interactive rendering.

Existing precedent: `FocusOnNavigate` uses exactly this pattern — `if (AssignedRenderMode is not null) return;` to skip SSR-specific rendering in interactive mode.

**JS layer — no special detection needed:**

If the C# layer correctly gates `data-val-*` emission, the JS library naturally ignores interactive forms because they have no `data-val="true"` elements to discover. The JS scans for `[data-val="true"]` — if there are none, it does nothing. This is the simplest and most robust approach: the C# layer is the single point of control.

**Decision needed:** Confirm `AssignedRenderMode is not null` as the gating condition, or discuss alternatives if prerendering of interactive components needs different treatment.

## Assumptions

- **Browser support:** The Constraint Validation API is supported in all modern browsers (Chrome, Firefox, Safari, Edge). No IE11 support is needed.
- **`data-val-*` protocol stability:** The `data-val-*` attribute protocol used by MVC's unobtrusive validation is stable and will not change in a breaking way.
- **Enhanced navigation event contract:** Blazor's `enhancedload` event will continue to fire after DOM patching, and streaming rendering completion fires the same event.
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
