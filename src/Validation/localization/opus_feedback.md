# Community Feedback on Validation Localization — Implications for Microsoft.Extensions.Validation Design

## Table of Contents

1. [Summary of Community Pain Points](#summary-of-community-pain-points)
2. [Key GitHub Issues & Discussion Themes](#key-github-issues--discussion-themes)
3. [Community Workarounds & Third-Party Solutions](#community-workarounds--third-party-solutions)
4. [FluentValidation as a Competitive Benchmark](#fluentvalidation-as-a-competitive-benchmark)
5. [Blazor-Specific Frustrations](#blazor-specific-frustrations)
6. [Broader Ecosystem Feedback](#broader-ecosystem-feedback)
7. [Design Implications for Microsoft.Extensions.Validation](#design-implications-for-microsoftextensionsvalidation)

---

## Summary of Community Pain Points

Based on analysis of GitHub issues, StackOverflow discussions, blog posts, and community libraries, the following pain points are consistently raised about validation localization in ASP.NET Core. They are ordered by frequency and severity of community frustration:

### 1. **Cannot Override Default English Messages Without Annotating Every Property** (Most Requested)

The #1 frustration across all discussions. Users want to write `[Required]` and get a localized error message automatically, without having to write `[Required(ErrorMessage = "SomeKey")]` on every single property. The current MVC design requires `ErrorMessage` to be explicitly set for `IStringLocalizer` to be invoked — if `ErrorMessage` is null/empty (i.e., using the attribute default), localization is bypassed entirely and the built-in English message from `System.ComponentModel.Annotations.SR.resources` is used directly.

**Sources**: [#4848](https://github.com/dotnet/aspnetcore/issues/4848) (+53 👍), [#33073](https://github.com/dotnet/aspnetcore/issues/33073) (+42 👍), [#59916](https://github.com/dotnet/aspnetcore/issues/59916) (+1), [dotnet/runtime#24084](https://github.com/dotnet/runtime/issues/24084) (+12 👍)

**Key quote** (issue #4848 — @tbolon, 2017):
> "There is no way to just adapt the generic DataAnnotation validation messages to your language without having to replace all your data annotations."

**Key quote** (issue #33073 — @sdudnic, 2021):
> "DataAnnotation should be translated in a similar way another localized strings are, depend on the current culture, be available in localized .json or .res files, without the need for each developer to translate by itself the same 'Is required' string."

**Key quote** (issue #33073 — @ianbrian, 2021, +9 👍):
> "Localization should be baked in. I've been spending ages replacing hard-coded strings in Identity razor pages. Millions of developers must have done that. What a waste of time."

### 2. **Inconsistency Between MVC, Blazor, and Minimal APIs**

Different frameworks have different localization capabilities. MVC has `AddDataAnnotationsLocalization()` with `DataAnnotationLocalizerProvider`. Blazor has no equivalent — the `DataAnnotationsValidator` doesn't support `IStringLocalizer`. Minimal APIs (with the new `Microsoft.Extensions.Validation`) also have no localization story. Users who share model classes across frameworks are forced to maintain duplicate models or use the lowest-common-denominator approach (`ErrorMessageResourceType` + compiled `.resx`).

**Sources**: [#12158](https://github.com/dotnet/aspnetcore/issues/12158) (+27 👍), [#29804](https://github.com/dotnet/aspnetcore/issues/29804) (+2)

**Key quote** (issue #29804 — @liuliang-wt, 2021):
> "Currently, the logic of handling localization of DataAnnotations is different in Blazor and ASP.NET Core. [...] I can't share the model and resource files to Blazor, I have to write it again."

**Key quote** (issue #12158 — @daniel-scatigno, 2022, +9 👍):
> "With .NET Core came the new 'way' of translating strings, using the IStringLocalizer is the new sensation!! But it came incomplete, it works within MVC and it half works with Blazor. It's a mess in my opinion!"

### 3. **Custom ValidationAttributes and Non-Standard Attributes Don't Get Localized**

The MVC localization pipeline only works for the 9 built-in adapter-supported attributes. Custom `ValidationAttribute` subclasses and BCL attributes without adapters (like `EnumDataTypeAttribute`) bypass the `IStringLocalizer` pipeline entirely — `DataAnnotationLocalizerProvider` is simply never called.

**Source**: [#4853](https://github.com/dotnet/aspnetcore/issues/4853)

**Key quote** (issue #4853 — @drauch, 2017):

> "For EnumDataType and all of our self-implemented ValidationAttributes it looks like the DataAnnotationLocalizerProvider is not called when generating the error message."

https://github.com/dotnet/aspnetcore/issues/4853#issuecomment-332895914

### 4. **Model Binding Error Messages Are a Separate Localization Problem**

Error messages from model binding (e.g., "The value '' is invalid for X") come from `ModelBindingMessageProvider`, which is entirely separate from DataAnnotations localization. Users have to configure both systems independently, and JSON deserialization errors (from Newtonsoft.Json or System.Text.Json) add yet another separate unlocalizable error source.

**Source**: [#33073 comment by @progmars](https://github.com/dotnet/aspnetcore/issues/33073#issuecomment-850229407) (+3 👍)

**Key quote** (@progmars, 2021):
> "Also, the entire model validation message pipeline should be improved. [...] the workaround becomes very ugly. Just look at this convoluted piece of code I had to use as a workaround to have fully translated binding errors."

### 5. **The `ErrorMessageResourceType` Approach Is Too Verbose and Inflexible**

The built-in `.resx`-based localization via `ErrorMessageResourceType` + `ErrorMessageResourceName` requires compiled resource types, a default `.resx` file without language suffix, and attributes become extremely verbose. Users find it impractical for large models.

**Key example from issue #29804**:
```csharp
// What users want:
[Required(ErrorMessage = "Validate.{0}required")]

// What they're forced to write for Blazor:
[Required(ErrorMessageResourceName = "Validate__0_required",
          ErrorMessageResourceType = typeof(Resources.SharedResource))]
```

### 6. **Localization Doesn't Work with Positional Records**

DataAnnotations localization breaks when validation attributes are placed on positional record constructor parameters — the localizer is not invoked for constructor-defined properties. This is a symptom of the metadata pipeline not handling the constructor-parameter-to-property mapping correctly for localization.

**Source**: [#39551](https://github.com/dotnet/aspnetcore/issues/39551) (+1)

### 7. **No Built-In Translation Packages for Common Languages**

Unlike .NET Framework (which shipped satellite assemblies like `Microsoft.AspNet.Mvc.fr`), .NET Core / ASP.NET Core provides no community or official translation packages for default validation messages. Every non-English project must create their own translations from scratch.

**Source**: [#59916](https://github.com/dotnet/aspnetcore/issues/59916), [dotnet/runtime#24084](https://github.com/dotnet/runtime/issues/24084) (closed as won't fix)

**Key quote** (issue #59916 — @fredericDelaporte, 2025):
> "This is a regression compared to the situation with MVC in .NET Framework, in which data-annotations generated by default HTML helpers were having packages per language providing default localization."

### 8. **ProblemDetails Title Not Localizable (Minimal APIs)**

When validation errors are returned as `HttpValidationProblemDetails`, the `title` field ("One or more validation errors occurred.") cannot be localized through the validation pipeline — it requires separate `ProblemDetailsService` configuration.

**Source**: [#61534](https://github.com/dotnet/aspnetcore/issues/61534) (+2 👍)

---

## Key GitHub Issues & Discussion Themes

| Issue | Title | 👍 | Status | Key Theme |
|-------|-------|---:|--------|-----------|
| [#4848](https://github.com/dotnet/aspnetcore/issues/4848) | Ability to translate all DataAnnotations without specifying ErrorMessage | 53 | Open (Backlog) | Global default message override |
| [#33073](https://github.com/dotnet/aspnetcore/issues/33073) | Localize standard DataAnnotations in ASP.NET Core MVC | 42 | Closed | Built-in localization for defaults |
| [#12158](https://github.com/dotnet/aspnetcore/issues/12158) | [Blazor] Data annotations localization support | 27 | Open (11.0-p2) | Blazor localization parity with MVC |
| [#8573](https://github.com/dotnet/aspnetcore/issues/8573) | Design a better validation experience for MVC | 84 | Open (Backlog) | Complete validation redesign |
| [#4853](https://github.com/dotnet/aspnetcore/issues/4853) | DataAnnotationLocalizerProvider not called for custom attrs | 0 | Open (Backlog) | Custom attribute localization gap |
| [#29804](https://github.com/dotnet/aspnetcore/issues/29804) | Blazor use same localization logic as ASP.NET Core | 2 | Open (Backlog) | Cross-framework consistency |
| [#39551](https://github.com/dotnet/aspnetcore/issues/39551) | Localization doesn't work with positional records | 1 | Open (Backlog) | Records support |
| [#59916](https://github.com/dotnet/aspnetcore/issues/59916) | Default localization for tag helper data-annotations | 1 | Open (Backlog) | Built-in translations |
| [#61534](https://github.com/dotnet/aspnetcore/issues/61534) | Customize default validation message (ProblemDetails title) | 2 | Closed | Response-level localization |
| [runtime#24084](https://github.com/dotnet/runtime/issues/24084) | Localization for default error messages in System.ComponentModel.Annotations | 12 | Closed (won't fix) | BCL-level localization |

---

## Community Workarounds & Third-Party Solutions

### 1. Custom `IValidationMetadataProvider` (Most Common MVC Workaround)

The most widely recommended workaround on StackOverflow. Users implement `IValidationMetadataProvider` to inject `ErrorMessage` values onto attributes that don't have one set, using the attribute's default message as a lookup key for `IStringLocalizer`.

**Pattern**:
```csharp
public class LocalizableValidationMetadataProvider : IValidationMetadataProvider
{
    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        foreach (var attr in context.ValidationMetadata.ValidatorMetadata.OfType<ValidationAttribute>())
        {
            if (string.IsNullOrEmpty(attr.ErrorMessage) && attr.ErrorMessageResourceType == null)
            {
                // Set ErrorMessage to a localizable key based on attribute type
                attr.ErrorMessage = $"{attr.GetType().Name}_ValidationError";
            }
        }
    }
}
```

**Pros**: Works globally, no per-property annotation needed.
**Cons**: Mutates shared attribute instances (thread safety concern), requires knowing all attribute types, doesn't integrate with the adapter pipeline.

**Source**: [StackOverflow: How to provide localized validation messages](https://stackoverflow.com/questions/40788092/how-to-provide-localized-validation-messages-for-validation-attributes)

### 2. XLocalizer Library

Community library ([github.com/LazZiya/XLocalizer](https://github.com/LazZiya/XLocalizer)) that provides a comprehensive localization solution including:
- Automatic localization of all DataAnnotation, ModelBinding, and Identity error messages
- Configurable via startup code or JSON
- Auto-adding of missing localization keys
- Online translation support

**Pattern** (from XLocalizer):
```csharp
services.AddRazorPages()
    .AddXLocalizer<...>(ops =>
    {
        ops.ValidationErrors = new ValidationErrors
        {
            RequiredAttribute_ValidationError = "The {0} field is required.",
            CompareAttribute_MustMatch = "'{0}' and '{1}' do not match.",
            // ...
        };
    });
```

### 3. `Toolbelt.Blazor.LocalizedDataAnnotationsValidator`

Community Blazor component ([github.com/jsakamoto/Toolbelt.Blazor.LocalizedDataAnnotationsValidator](https://github.com/jsakamoto/Toolbelt.Blazor.LocalizedDataAnnotationsValidator/)) that replaces the standard `<DataAnnotationsValidator />` with a localization-aware version that uses `IStringLocalizer`.

### 4. Custom `IValidationAttributeAdapterProvider` Replacement

Some users replace the built-in `ValidationAttributeAdapterProvider` to intercept all attribute types (not just the 9 built-in ones) and apply localization. This is complex and requires reimplementing all adapters.

### 5. Custom `JsonInputFormatter` for JSON Binding Errors

Users like @progmars ([#33073 comment](https://github.com/dotnet/aspnetcore/issues/33073#issuecomment-850229407)) have had to create custom `JsonInputFormatter` implementations to intercept and localize JSON deserialization errors, because neither Newtonsoft.Json nor System.Text.Json errors go through the DataAnnotations localization pipeline.

---

## FluentValidation as a Competitive Benchmark

FluentValidation is frequently cited as the alternative that "gets localization right." Its architecture provides several features that DataAnnotations lacks, and that our new design should learn from:

### What FluentValidation Does Well

1. **Built-in translations for 25+ languages**: FluentValidation ships translated default messages for common validators in many languages. No per-project work needed.

2. **`ILanguageManager` abstraction**: A single, pluggable interface controls all message resolution:
   ```csharp
   public interface ILanguageManager
   {
       string GetString(string key, CultureInfo culture = null);
       bool Enabled { get; set; }
       CultureInfo Culture { get; set; }
   }
   ```
   Users can extend `LanguageManager` to add/override translations globally, or swap in a completely custom implementation.

3. **Culture fallback**: Automatically falls back from specific culture (`fr-CA`) → neutral culture (`fr`) → English, ensuring messages always resolve.

4. **Per-rule message override**: `WithMessage()` on individual rules, which can accept lambdas for dynamic messages.

5. **Global message override**: `ValidatorOptions.Global.LanguageManager` provides app-wide control.

6. **Clean separation**: Validators are separate classes, not attributes. This makes DI injection of `IStringLocalizer` and other services natural.

### What FluentValidation Gets Wrong (or Doesn't Address)

1. **No source generation**: All validation is runtime/reflection-based. No compile-time safety.
2. **No client-side validation story**: No built-in mechanism for client-side metadata extraction.
3. **Separate class per model**: More boilerplate for simple validation scenarios vs. attribute annotations.
4. **Message format differences**: Uses `{PropertyName}` placeholders instead of `{0}` positional placeholders, making messages incompatible with `ValidationAttribute.FormatErrorMessage`.

### Implications for Our Design

The FluentValidation `ILanguageManager` pattern suggests that our design should provide:
- **A global, pluggable message resolution mechanism** that doesn't require per-attribute/per-property annotation
- **Built-in support for the "no ErrorMessage set" case** — intercept the default English message and route it through localization
- **Culture-aware fallback** behavior built into the pipeline
- **Easy per-attribute-type message override** without having to annotate every property

---

## Blazor-Specific Frustrations

Blazor's validation localization gap has been an open issue since 2019 ([#12158](https://github.com/dotnet/aspnetcore/issues/12158)) and is the most emotionally charged of all the feedback:

### Key Complaints

1. **5+ years without resolution** (2019→2025): The issue has been moved between milestone plans (Backlog → .NET 8 Planning → Backlog → .NET 9 Planning → Backlog → .NET 11) repeatedly, generating community frustration.

2. **Blocks non-English Blazor adoption**: Multiple users report that DataAnnotations localization is a hard blocker for using Blazor in production for non-English applications.

   **Key quote** (issue #12158 — @davhdavh, 2023):
   > "This absolutely blocks Blazor from being used for small projects that need to support a language different than en-US."

   **Key quote** (@mduu, 2024, +7 👍):
   > "Really? This basic functionality is now laying in your backlog for five years..."

3. **`IStringLocalizer` injection into `DataAnnotationsValidator` is the expected approach**: Multiple commenters independently arrive at the same design — inject `IStringLocalizer` into the validator component and pass it through the validation pipeline.

4. **Blazor Server vs. WASM scoping**: For Blazor Server, localization services are scoped per-circuit (user). But `ValidationAttribute`s are shared/cached, creating the static-vs-scoped mismatch. Issue #12158 describes elaborate workarounds using `Func<ILocalizationService>` delegates and static providers.

5. **Different localization mechanism than MVC**: MVC uses `AddDataAnnotationsLocalization()` with `IStringLocalizer`. Blazor's `DataAnnotationsValidator` has no equivalent. Users who share models between MVC and Blazor must annotate twice or use the verbose `ErrorMessageResourceType` approach.

### The .NET 11 Promise

The assignee @oroztocil [commented in January 2026](https://github.com/dotnet/aspnetcore/issues/12158#issuecomment-3755105435):
> "We plan to implement better localization support for DataAnnotations-based validation for .NET 11 (along with several other validation improvements). Feature requests and use case descriptions are highly appreciated."

This is the commitment our design must fulfill.

---

## Broader Ecosystem Feedback

### StackOverflow Themes

Across hundreds of StackOverflow questions about DataAnnotations localization, the most common themes are:

1. **"How do I localize [Required] without setting ErrorMessage?"** — The single most asked question. No satisfying answer exists within the framework.

2. **"How do I use a shared resource file for all validation messages?"** — Users want one `.resx` file for all models, not per-model resources. The `DataAnnotationLocalizerProvider` delegate enables this, but only if `ErrorMessage` is set.

3. **"Why doesn't my custom ValidationAttribute get localized?"** — The adapter pipeline only covers 9 built-in attributes. Everything else gets the raw `FormatErrorMessage()` output.

4. **"How do I localize display names?"** — `[Display(Name = "Key", ResourceType = typeof(R))]` works but requires compiled resource types. `IStringLocalizer`-based display name localization is only available through `ModelMetadata.GetDisplayName()` in MVC.

### Blog Post & Tutorial Patterns

Common blog post patterns for validation localization workarounds:

1. **The "Shared Resource" pattern**: Create a dummy `SharedResource` class, create per-culture `.resx` files, configure `DataAnnotationLocalizerProvider` to use it. Requires `ErrorMessage` on every attribute.

2. **The "Metadata Provider" pattern**: Implement `IValidationMetadataProvider` to inject `ErrorMessage` keys globally. Most DRY approach but involves attribute mutation.

3. **The "Custom Adapter Provider" pattern**: Replace `IValidationAttributeAdapterProvider` to support custom attributes. Most correct but most complex.

4. **The "Abandon DataAnnotations" pattern**: Switch to FluentValidation. Increasingly common recommendation for non-trivial localization needs.

---

## Design Implications for Microsoft.Extensions.Validation

Based on the community feedback above, here are concrete design implications and new ideas for the localization design in `Microsoft.Extensions.Validation`:

### Implication 1: The "Default Message Override" Use Case Must Be First-Class

**Community signal**: Issues #4848 (53 👍), #33073 (42 👍), runtime#24084 (12 👍)

The most requested feature is the ability to localize validation messages **without** setting `ErrorMessage` on every attribute. This means the validation pipeline must be able to intercept the default English message template (e.g., `"The {0} field is required."`) and look it up in a localizer, *even when `ErrorMessage` is not explicitly set on the attribute*.

**Impact on DQ3 (How to localize — template vs formatted?)**: This strongly argues for a **pre-format interception** approach (Option A or C in the design doc). The pipeline must intercept the error message template **before** `FormatErrorMessage()` is called, substitute it with a localized version, and then let formatting proceed. Post-format localization (Option B) cannot work here because the formatted English message is not a useful lookup key.

**Specific design idea**: The validation pipeline should obtain the attribute's **default error message template** (via reflection on the internal `ErrorMessageString` property, or by knowing the default for each built-in attribute type), use that as the localization lookup key, and substitute the localized template before formatting. This is what FluentValidation's `ILanguageManager` does — it knows the default message for each validator type and provides translations keyed by validator type + culture.

**Approach A — Template registry**: Maintain a mapping of `ValidationAttribute` type → default error message template. The source generator can emit this. At validation time, look up the attribute type, get the default template, pass it through the localizer, and if a translation is found, use it instead of the built-in default. This avoids any attribute mutation.

**Approach B — Well-known key convention**: Define a convention where the default message for `RequiredAttribute` is looked up with key `"RequiredAttribute_ValidationError"` (or the English template itself `"The {0} field is required."`). Users provide translations for these well-known keys. The pipeline checks the localizer for a translation before falling back to the attribute's built-in message.

### Implication 2: Localization Must Work Uniformly for All Attribute Types

**Community signal**: Issue #4853, StackOverflow questions about custom attributes

The MVC adapter system only localizes 9 built-in attribute types. Custom `ValidationAttribute` subclasses are silently excluded from localization. The new design must not have this limitation.

**Impact on DQ4 (Adapter abstraction?)**: This reinforces the recommendation to **avoid** per-attribute adapters. Instead, localization should work at a level above individual attributes — intercepting the error message for **any** `ValidationAttribute`, regardless of type. A delegate or callback on `ValidationOptions` that receives the attribute type, the error message template, and the format arguments is the right abstraction.

**Specific design idea**: The localization callback should receive enough context to customize per-attribute-type:
```csharp
Func<ValidationAttribute attribute, string errorMessageTemplate,
     string displayName, object? value, string>? ErrorMessageLocalizer
```
This lets users provide translations keyed by attribute type if desired, or use the template string itself as a key.

### Implication 3: Cross-Framework Consistency Is Critical

**Community signal**: Issues #12158, #29804

The new package must provide **one** localization mechanism that works identically in Minimal APIs, Blazor, and any future framework integration. Users share model classes across frameworks. The localization configuration on `ValidationOptions` must be the single source of truth.

**Impact on DQ13 (Where should localization integration live?)**: This argues for localization hooks being on `ValidationOptions` (Option A or B in the design doc) — not on framework-specific integration layers. Framework integrations should set up the `ValidationOptions` localization, but the hook shape must be identical.

**Specific design idea**: Provide an `AddValidationLocalization()` extension method that:
1. Registers `IStringLocalizerFactory` if not already registered
2. Configures `ValidationOptions.ErrorMessageLocalizer` to use `IStringLocalizer`
3. Works in any host (MVC, Minimal APIs, Blazor, console apps)

### Implication 4: Display Name Localization Needs a Story

**Community signal**: StackOverflow questions, issue #28118, #29804

Display names (the `{0}` argument in error messages) must be localizable. The source generator currently emits string literals. Users expect `[Display(Name = "Key", ResourceType = typeof(R))]` to work, and they expect `IStringLocalizer` to be able to resolve display names.

**Impact on DQ5**: Both generator-level `ResourceType` resolution (Option A) and runtime callback (Option B) are needed. The callback should be the primary mechanism since it works with `IStringLocalizer`, and the generator can optimize the `ResourceType` case.

### Implication 5: Consider Shipping Built-In Translation Packages

**Community signal**: Issues #59916, runtime#24084, FluentValidation comparison

FluentValidation ships translations for 25+ languages. The .NET Framework shipped satellite assemblies. The community strongly expects that the default validation messages should be available in common languages out of the box. While this may be out of scope for v1 of `Microsoft.Extensions.Validation`, the design should make it **easy** for:

1. A community NuGet package to provide translations (by registering well-known keys in the localizer)
2. Microsoft to ship official translation packages in the future (by having a standard set of localization keys)

**Specific design idea**: Define and document a standard set of localization keys for all BCL `ValidationAttribute` default messages:

| Key | Default English Value |
|-----|----------------------|
| `RequiredAttribute_ValidationError` | `The {0} field is required.` |
| `RangeAttribute_ValidationError` | `The field {0} must be between {1} and {2}.` |
| `StringLengthAttribute_ValidationError` | `The field {0} must be a string with a maximum length of {1}.` |
| `MinLengthAttribute_ValidationError` | `The field {0} must be a string or array type with a minimum length of '{1}'.` |
| `MaxLengthAttribute_ValidationError` | `The field {0} must be a string or array type with a maximum length of '{1}'.` |
| `RegularExpressionAttribute_ValidationError` | `The field {0} must match the regular expression '{1}'.` |
| `CompareAttribute_MustMatch` | `'{0}' and '{1}' do not match.` |
| `EmailAddressAttribute_Invalid` | `The {0} field is not a valid e-mail address.` |
| `PhoneAttribute_Invalid` | `The {0} field is not a valid phone number.` |
| `CreditCardAttribute_Invalid` | `The {0} field is not a valid credit card number.` |
| `UrlAttribute_Invalid` | `The {0} field is not a valid fully-qualified http, https, or ftp URL.` |
| `FileExtensionsAttribute_Invalid` | `The {0} field only accepts files with the following extensions: {1}` |

This enables community translation packages (a NuGet package containing `.resx` files with these keys for `fr`, `de`, `es`, `ja`, etc.) and also enables the "no ErrorMessage set" scenario — the pipeline uses the attribute type to look up the key, checks the localizer, and falls back to the English default.

### Implication 6: The Error Message Transformation Hook Must Be Flexible Enough for Multiple Use Cases

**Community signal**: Across all issues

Users want to use the localization hook for different purposes:
1. **Translation**: Replace English defaults with localized equivalents
2. **Customization**: Replace default messages with app-specific wording (even in English)
3. **Standardization**: Enforce consistent error message patterns across the app

The hook must support all three. A `Func` that receives the attribute, the template, and the display name, and returns the final message is sufficient. The hook should be called for **every** validation error, not just when `ErrorMessage` is set.

### Implication 7: The "Attribute Mutation" Problem Must Be Avoided

**Community signal**: Known issue in the `IValidationMetadataProvider` workaround, MVC adapter thread-safety

Several MVC workarounds involve mutating `ValidationAttribute.ErrorMessage` before validation. This is thread-unsafe because attributes are shared across requests (they're cached by the metadata system). The new validation package uses a `ConcurrentDictionary` cache for attributes, making mutation even more dangerous.

**Impact on DQ3**: This rules out Option A (pre-format by mutating `ErrorMessage` on the attribute) for the new package. The localization must happen **alongside** the attribute, not **on** the attribute. Reading the attribute's `ErrorMessage` / default template, localizing it, and then formatting — without mutating the attribute instance — is the correct approach.

**Specific design idea**: The `ValidatablePropertyInfo.ValidateAsync()` override in the generated code should:

1. Read `attribute.ErrorMessage` (or resolve the default template by attribute type) — **read-only**
   1. Pass the template through the localization callback

2. If a localized template is returned, call `string.Format(localizedTemplate, displayName, ...args)` directly instead of calling `attribute.GetValidationResult()`
3. If no localization is configured, fall back to `attribute.GetValidationResult()` as today

This avoids any attribute mutation while enabling pre-format localization.

### Implication 8: The Design Should Support Non-IStringLocalizer Scenarios

**Community signal**: Issue #12158 (database-backed localization), Blazor WASM constraints

Not all users use `IStringLocalizer`. Some use database-backed localization services, custom localization frameworks, or simple dictionary lookups. The localization hook must be a delegate/func, not an `IStringLocalizer` dependency. Framework integrations should provide convenience methods that wire `IStringLocalizer` into the delegate.

**Impact on DQ1**: This supports Option B (framework-agnostic delegate, no dependency on `Microsoft.Extensions.Localization.Abstractions`). The core package should define:
```csharp
public Func<string errorMessageTemplate, Type attributeType,
            string displayName, string>? ErrorMessageLocalizer { get; set; }
```
And a separate extension (in the framework integration layer or a thin add-on package) should provide the `IStringLocalizer` wiring.

### Implication 9: The ProblemDetails/Response-Level Localization Is a Separate Concern

**Community signal**: Issue #61534

Localizing the `ProblemDetails.Title` field is a response-formatting concern, not a validation concern. The validation package should focus on localizing the error messages themselves. The `ProblemDetailsService.CustomizeProblemDetails` API already provides a hook for response-level customization. Document this clearly.

### Implication 10: Records and Source-Generated Types Need Special Attention

**Community signal**: Issue #39551

Positional records place validation attributes on constructor parameters, not properties. The source generator must correctly handle this case for localization — resolving display names and error messages from parameter-level attributes, not just property-level attributes. The existing MVC bug (#39551) should not be reproduced in the new package.

---

## Summary: Priority-Ordered Feature Requests from the Community

Based on the feedback analysis, here are the features the community most wants, in priority order:

| # | Feature | Signal Strength | Design Doc Reference |
|---|---------|----------------|---------------------|
| 1 | Override default English messages without per-property annotation | Very High (95+ 👍) | DQ3, DQ4, DQ14 |
| 2 | Single localization mechanism across MVC, Blazor, Minimal APIs | Very High (30+ 👍) | DQ1, DQ2, DQ13 |
| 3 | Localization works for ALL attribute types, not just built-in 9 | High | DQ4 |
| 4 | Display name localization via `IStringLocalizer` | High | DQ5 |
| 5 | Standard localization keys for community translation packages | Medium-High | DQ16 (new idea) |
| 6 | No attribute mutation for thread safety | Medium | DQ3 |
| 7 | Support for non-IStringLocalizer localization backends | Medium | DQ1 |
| 8 | Records/positional parameter support | Medium | DQ16 |
| 9 | Built-in translations for common languages | Medium (long-term) | New idea |
| 10 | ProblemDetails title localization guidance | Low (documented) | Separate concern |
