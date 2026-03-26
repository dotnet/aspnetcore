# Summary

This document proposes adding **first-party, zero-dependency client-side form validation** to **ASP.NET Core** that works for both **Blazor Web Apps** (static SSR + enhanced navigation) and **MVC / Razor Pages**, closing a long-standing gap where Blazor static-SSR forms require a SignalR circuit for client validation and MVC relies on jQuery + jquery-validate + jquery-validation-unobtrusive (118 KB combined).

The feature delivers a **standalone JavaScript validation library** (target: ≤ 4 KB Brotli-compressed, no dependencies) that understands the existing `data-val-*` attribute protocol and uses the browser-native **Constraint Validation API** as its validation state mechanism. For Blazor, a new **C# service layer** enables existing input components to automatically emit `data-val-*` attributes from `DataAnnotations`. For MVC, the library is a **drop-in replacement** for the jQuery validation stack—same attributes, same CSS classes, no C# changes. The library ships as a standalone file that can be referenced by any ASP.NET Core app.

## Goals

- **Enable client-side validation for Blazor SSR without a circuit.** Today, Blazor Web Apps rendered via static SSR have no client-side validation—every validation check requires a full server round-trip. This feature adds immediate, in-browser validation feedback using standard `DataAnnotations`, without requiring Blazor Server interactivity or WebAssembly. Developers add a `<ClientSideValidator />` component inside their `<EditForm>` and call `builder.Services.AddClientSideValidation()` in `Program.cs`; all existing `<InputText>`, `<InputNumber>`, etc. components then automatically emit `data-val-*` attributes and the bundled JS library validates on the client.

- **Provide a zero-dependency JavaScript validation library (≤ 4 KB Brotli).** The current MVC client-side validation stack depends on jQuery (87 KB), jquery-validate (23 KB), and jquery-validation-unobtrusive (8 KB). This feature delivers a standalone JavaScript library with no external dependencies that understands the same `data-val-*` attribute protocol. It uses the browser-native Constraint Validation API (`setCustomValidity`, `ValidityState`, `checkValidity`) as the validation state mechanism and extensibility surface, enabling third-party libraries to read standardized validity state without coupling to our internals.

- **Be a drop-in replacement for jQuery unobtrusive validation in ASP.NET Core MVC / Razor Pages.** Existing ASP.NET Core MVC apps can swap out the three jQuery-based script references for a single `<script>` tag. No C# code changes are required—the library reads the same `data-val-*` attributes that MVC tag helpers already emit and uses the same CSS class names (`input-validation-error`, `field-validation-error`, `validation-summary-errors`, etc.) and `data-valmsg-for` / `data-valmsg-summary` conventions.

- **Work across the Blazor enhanced navigation lifecycle.** Blazor's enhanced navigation patches the DOM without full page loads. The validation library automatically re-scans for new or changed forms after each navigation, handles DOM element reuse, and intercepts form submission so validation runs before the enhanced navigation fetch. No developer action is required beyond the initial setup.

- **Support extensibility for custom validators.** Developers can register custom validation providers on the JavaScript side and custom adapters on the C# side, enabling the same custom-attribute-to-client-rule pipeline that MVC provides but with fewer layers of indirection.

## Non-goals

- **Full parity with the jQuery unobtrusive validation rule ecosystem.** The feature targets a focused subset of validators aligned with core `System.ComponentModel.DataAnnotations` attributes (Required, StringLength, MinLength, MaxLength, Range, RegularExpression, Email, Url, Phone, CreditCard, Compare, FileExtensions). Niche MVC-specific validators and any third-party jquery-validation plugins are not replicated. Custom validators can be added via the extensibility API.

- **Remote / async server-backed validation in Blazor SSR.** MVC's `[Remote]` attribute pattern (where the server is queried via AJAX during client-side validation) is supported only when used from MVC apps. In Blazor SSR, `RemoteAttribute` is explicitly unsupported and will throw `NotSupportedException`. Async form submission in Blazor requires a public `Blazor.submitForm()` API that does not yet exist; remote validation for Blazor is deferred to a future release. See [Open Question 1](#open-question-1-blazor-async-form-submission-api).

- **Replacing browser-native validation UI.** The library suppresses native browser validation tooltips and manages its own error display via CSS classes and message target elements. However, it does not attempt to provide a rich validation UI framework (tooltips, animations, etc.). Richer UI is left to application CSS and/or third-party libraries that can read the Constraint Validation API state.

- **Source generator or compile-time validation metadata.** All C# attribute discovery is reflection-based (with per-field caching). A source-generator approach for trimming-friendly or AOT-compatible metadata emission is a potential future optimization but is not part of this feature. The reflection-based approach is not trimming-safe or Native AOT compatible; applications using PublishTrimmed or PublishAot may need the future source-generator approach.

- **Custom element for validation targets.** The original proposal included an optional custom element using `ElementInternals` for message rendering. This has been deferred in favor of the simpler `data-valmsg-for` span-based approach that is already compatible with both MVC and Blazor conventions.

- **Legacy ASP.NET MVC 5 support.** The drop-in replacement targets ASP.NET Core MVC only. While the `data-val-*` protocol is largely the same across ASP.NET MVC 5 and ASP.NET Core MVC, differences in anti-forgery token handling, remote validation URL generation, and other details mean that legacy MVC 5 apps are not a tested or supported scenario.

## Proposed solution

The solution consists of two main parts that can be consumed independently:

1. **A standalone JavaScript validation library** (`aspnet-validation.js`) that scans the DOM for `data-val-*` attributes, validates form fields on user interaction and submission, displays error messages, and integrates with the Constraint Validation API. The library auto-detects whether it's running in a Blazor or MVC context and adapts accordingly (e.g., re-scanning after Blazor enhanced navigation, or providing a `parse()` API for MVC dynamic content). It ships as a static web asset and can be referenced by any ASP.NET Core app.

2. **A Blazor C# service layer** that enables existing Blazor input components (`InputText`, `InputNumber`, `InputSelect`, etc.) to automatically emit `data-val-*` attributes derived from `DataAnnotations` on the model. A new `<ClientSideValidator />` component activates this behavior per-form, following the same opt-in pattern as `<DataAnnotationsValidator />`.

Client-side validation is a **progressive enhancement**: forms are fully functional before the script loads, and server-side validation always remains authoritative. The client library improves UX by providing immediate feedback without a round-trip.

### Scenario 1: Basic Blazor SSR form with client-side validation

The simplest and most common scenario. A statically-rendered Blazor form gets immediate client-side validation without enabling interactive render modes.

**Program.cs — one-time service registration:**
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents();
builder.Services.AddClientSideValidation();

var app = builder.Build();
app.MapRazorComponents<App>();
app.Run();
```

**Register.razor — opt-in per form:**
```razor
<EditForm Model="model" Enhance FormName="register">
    <DataAnnotationsValidator />
    <ClientSideValidator />
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
- `AddClientSideValidation()` registers the validation service. `<ClientSideValidator />` activates it for this form and emits the JS library `<script>` tag.
- All `InputBase<T>`-derived components (`InputText`, `InputNumber`, `InputSelect`, `InputDate`, `InputTextArea`, `InputCheckbox`, `InputRadioGroup`) automatically emit `data-val-*` attributes based on the model's `DataAnnotations`.
- `<ValidationMessage>` renders a `<span data-valmsg-for="...">` target for per-field errors.
- `<ValidationSummary>` renders a `<div data-valmsg-summary="true">` container for the error list.
- The JS library scans the DOM, finds the annotated elements, and wires validation.
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

**Error messages** are resolved using `ValidationAttribute.FormatErrorMessage()` with the display name derived from `[Display(Name = "...")]`, `[DisplayName("...")]`, or the property name as fallback. Localized error messages work through the standard DataAnnotations localization mechanisms (resource files, `IStringLocalizer`). Messages are resolved on the server at render time and embedded in the `data-val-*` attributes.

**Nested models** are fully supported. A property like `model.Address.Street` produces validation attributes with the correct dotted-path field name, and `<ValidationMessage For="@(() => model.Address.Street)" />` renders the matching `data-valmsg-for` target.

### Scenario 3: Validation timing and UX behavior

The library uses a smart validation timing strategy to avoid poor "everything is red immediately" UX:

| User action | Behavior |
|---|---|
| **Typing** (`input` event) | Only **clears** existing errors. Does **not** show new errors while the user is actively typing. |
| **Leaving a field** (`change`/`blur` event) | **Shows or clears** errors. This is the primary trigger for displaying validation errors. |
| **Submitting the form** | **Validates all fields.** Blocks submission if any field is invalid. Updates the validation summary. |

Once a field has been marked invalid, subsequent keystrokes will clear the error in real-time as soon as the input becomes valid—providing immediate feedback as the user corrects the issue.

### Scenario 4: Enhanced navigation and streaming (Blazor)

Forms that appear after a Blazor enhanced navigation or streaming rendering update are **automatically wired for validation** with no developer action required.

**How it works for the developer:** The `<ClientSideValidator />` component renders the `<script>` tag once. The library subscribes to Blazor's `enhancedload` event and re-scans the DOM after each navigation or streaming update. New form fields are discovered and wired automatically. Existing fields whose validation attributes changed (e.g., because the server re-rendered with different rules) are detected and re-registered.

**Multiple forms on a page** are supported. Each form is validated independently—submitting one form does not trigger validation on another.

### Scenario 5: Opt-in and opt-out

**Blazor opt-in:** Client-side validation is enabled per-form by placing `<ClientSideValidator />` inside `<EditForm>`. Forms without it behave exactly as they do today. The feature is completely additive.

**MVC opt-in:** Include the `aspnet-validation.js` script. The library auto-scans all forms on page load.

**Per-button opt-out:** The standard `formnovalidate` HTML attribute skips validation for that submit action:
```html
<button type="submit">Save</button>                           <!-- validates -->
<button type="submit" formnovalidate>Save as Draft</button>   <!-- skips validation -->
```

**Per-field opt-out:** Fields without `DataAnnotations` attributes will not have `data-val` emitted and are ignored by the library.

**Script tag control (Blazor):** `<ClientSideValidator IncludeScript="false" />` suppresses the automatic `<script>` tag, letting the developer place it in a layout or use a custom bundler:

```razor
<!-- In a layout or _Host.cshtml -->
<script src="_content/Microsoft.AspNetCore.Components.Web/aspnet-validation.js"></script>

<!-- In individual forms — no duplicate script tags -->
<EditForm Model="model" Enhance FormName="profile">
    <DataAnnotationsValidator />
    <ClientSideValidator IncludeScript="false" />
    ...
</EditForm>
```

### Scenario 6: Suppressed browser validation and Constraint Validation API extensibility

The library sets `novalidate` on all participating forms at runtime (after the JS loads), preventing browsers from showing their native validation tooltips. Instead, the library uses `setCustomValidity()` to set validation state on each element, which means the browser-standard Constraint Validation API is populated with the current validation state.

**For third-party library authors and advanced scenarios**, this means any validated element exposes standard properties:

```javascript
const input = document.querySelector('input[name="Email"]');
input.validity.valid;          // false
input.validity.customError;    // true
input.validationMessage;       // "The Email field is required."
input.willValidate;            // true
```

Third-party libraries can listen for the standard `invalid` event, read `ValidityState`, and build custom UI without depending on our library's internals.

### Scenario 7: Custom validation attributes with client-side support

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

### Scenario 8: Drop-in replacement for jQuery unobtrusive validation in MVC

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

> **Distribution:** The library will be available as a static web asset in the ASP.NET Core shared framework. MVC apps that don't use Blazor can reference it via `_content/Microsoft.AspNetCore.Components.Web/aspnet-validation.js` or copy the standalone file into their `wwwroot`. See [Open Question 2](#open-question-2-mvc-script-distribution) for the final distribution mechanism.

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

### Scenario 9: MVC remote validation

In MVC mode, the `[Remote]` attribute works out of the box:

```csharp
public class RegisterModel
{
    [Remote(action: "VerifyEmail", controller: "Account")]
    public string Email { get; set; }
}
```

The library sends a `fetch()` request to the specified URL, supports GET (default) and POST, caches results per-element to avoid redundant requests, and interprets the response (`true`/`"true"` = valid, `false` = invalid, any other string = custom error message). On network error, the field is treated as valid to avoid blocking the user.

> ⚠️ **Blazor SSR:** Remote validation is **not supported**. Using `[Remote]` on a model validated by `<ClientSideValidator />` throws `NotSupportedException` at render time. See [Open Question 1](#open-question-1-blazor-async-form-submission-api).

## Open questions

<a id="open-question-1-blazor-async-form-submission-api"></a>
### Open Question 1: Blazor async form submission API

**Context:** When validation includes async providers (e.g., remote validation), the submit handler must block the default submission, await validation, and then re-submit the form if valid. Re-submission via `requestSubmit()` may not reliably trigger Blazor's enhanced navigation.

**Impact:** For the initial release, all built-in Blazor validators are synchronous, so this does not affect any built-in scenario. It matters only for future remote validation support in Blazor and for developers who register custom async providers.

**Decision needed:** Whether to expose a public `Blazor.submitForm()` API, use a custom DOM event, or defer async provider support for Blazor entirely.

<a id="open-question-2-mvc-script-distribution"></a>
### Open Question 2: Script distribution for MVC apps

**Context:** The JS library ships as a static web asset in `Microsoft.AspNetCore.Components.Web`. MVC apps that don't reference this package need an alternative way to obtain the script.

**Options:**
1. Ship the JS file as part of the ASP.NET Core shared framework (available to all apps).
2. Publish as a standalone NuGet package (e.g., `Microsoft.AspNetCore.ClientValidation`).
3. Document manual file copy from the Components.Web package.

**Decision needed:** What is the distribution mechanism for MVC apps?

### Open Question 3: Script deduplication for multiple forms

**Context:** If multiple forms on a page each have `<ClientSideValidator />`, multiple `<script>` tags are emitted. The JS is idempotent, but duplicate loads are wasteful.

**Options:**
1. Keep current approach with guidance to use `IncludeScript="false"` and a single script tag in the layout.
2. Use `HeadContent` or a persistent component to deduplicate automatically.
3. Accept the duplication — browsers cache the file after the first load.

**Decision needed:** Whether to address deduplication in this release or document the workaround.

### Open Question 4: Accessibility (ARIA) attributes

**Context:** The library toggles CSS classes and updates message text, but does not explicitly set ARIA attributes (`aria-invalid`, `aria-describedby`, `aria-live`). Some browsers auto-set `aria-invalid` when `setCustomValidity()` is called, but this is not universal.

**Decision needed:** Should the library explicitly manage ARIA attributes for accessibility compliance?

### Open Question 5: Numeric type validation for MVC compatibility

**Context:** MVC includes a `NumericClientModelValidator` that adds `data-val-number` for `float`, `double`, and `decimal` properties without any explicit annotation. The JS library currently has no `number` provider. MVC apps that switch to this library would lose that validation.

**Decision needed:** Add a `number` provider for MVC compatibility, or treat this as an accepted gap in the initial release?

### Open Question 6: Form reset behavior

**Context:** The library does not currently listen for the `reset` event on forms. After `form.reset()` or a `<button type="reset">`, element values revert to defaults but validation CSS classes and error messages persist in the DOM.

**Decision needed:** Should the library clear all validation state on form reset, or is this an accepted limitation?

## Assumptions

- **Browser support:** The Constraint Validation API is supported in all modern browsers (Chrome, Firefox, Safari, Edge). No IE11 support is needed.
- **`data-val-*` protocol stability:** The `data-val-*` attribute protocol used by MVC's unobtrusive validation is stable and will not change in a breaking way.
- **Enhanced navigation event contract:** Blazor's `enhancedload` event will continue to fire after DOM patching, and streaming rendering completion fires the same event.
- **Progressive enhancement:** Client-side validation is a UX enhancement. Server-side validation always remains authoritative. There is a brief window between page load and script initialization where the browser's native validation (or no validation) is active; any submission during that window is handled by server-side validation.
- **`RemoteAttribute` is MVC-specific:** `[Remote]` belongs to the `Microsoft.AspNetCore.Mvc` namespace and is not expected on Blazor models.
- **Trimming and AOT:** The reflection-based C# service (`GetProperty`, `GetCustomAttributes`) is **not** trimming-safe or Native AOT compatible. This is an accepted limitation for the initial release, with a source-generator approach as a future path.

## References

- **GitHub issue:** [dotnet/aspnetcore#51040 — Create server rendered forms with client validation using Blazor without a circuit](https://github.com/dotnet/aspnetcore/issues/51040)
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
