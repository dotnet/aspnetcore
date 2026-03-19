# Summary

This document proposes adding first-class localization support for validation error messages and display names to `Microsoft.Extensions.Validation`, and transitively to Blazor form validation and Minimal API model validation, to address the current lack of localization and customization support for validation messages.

Internationalized applications need to present validation errors in the user's language, and even English-only applications may need to customize the built-in validation messages. Currently, `System.ComponentModel.DataAnnotations` only offers a limited, verbose, static-property-based mechanism (`ErrorMessageResourceType`/`ErrorMessageResourceName`) that requires explicit annotation on every attribute instance and cannot load translations from databases, JSON files, or other dynamic sources. `Microsoft.Extensions.Validation`, the validation infrastructure powering Blazor and Minimal APIs since .NET 10, inherits these limitations and provides no additional localization capabilities.

The proposed solution adds generic delegate-based extensibility points to the core `Microsoft.Extensions.Validation` package, and provides a default `IStringLocalizer`-based localization implementation in a new `Microsoft.Extensions.Validation.Localization` package that integrates with `Microsoft.Extensions.Localization` and achieves feature parity with ASP.NET Core MVC's localization while offering more flexibility and broader framework support.

## Goals

- **Localize validation error messages at runtime.** `System.ComponentModel.DataAnnotations` provides only one localization mechanism for error messages: static property lookup via `ErrorMessageResourceType`/`ErrorMessageResourceName` on each attribute instance. This approach is verbose (each attribute must be individually annotated), requires recompilation to change translations, and cannot load translations from databases, JSON files, or other external sources. Furthermore, `ValidationAttribute.FormatErrorMessage(string name)` has a fixed signature — it only accepts the display name and uses its own internally-resolved template — so there is no way to inject a localized template at call time. This limitation is the root cause of the complexity in ASP.NET Core MVC's validation attribute adapter system. We want to enable applications to localize or replace validation error messages at runtime without requiring per-attribute annotations and without being limited to static property lookup.

- **Localize display names at runtime.** `DisplayAttribute.Name` currently returns a fixed string unless `ResourceType` is set, and the `Microsoft.Extensions.Validation` source generator currently ignores `DisplayAttribute.ResourceType` entirely (see [#65647](https://github.com/dotnet/aspnetcore/issues/65647)). This means display names in validation messages cannot be localized through any mechanism. We want to provide the ability to resolve localized display names at runtime, analogous to error message localization.

- **Provide a default `IStringLocalizer`-based implementation.** `Microsoft.Extensions.Localization` provides the standard runtime localization API in ASP.NET Core through `IStringLocalizer` and `IStringLocalizerFactory`, with a default resource-file-based implementation and support for custom implementations (database-backed, JSON-backed, etc.). There is currently no standard integration between this localization system and the validation infrastructure. We want to provide a ready-to-use implementation that integrates `Microsoft.Extensions.Validation` with `Microsoft.Extensions.Localization`, supporting both per-type and shared resource file approaches, and allowing users to plug in any `IStringLocalizerFactory` implementation.

- **Support localization of custom validation attributes.** In ASP.NET Core MVC, custom `ValidationAttribute` implementations have no adapter registered by default, so localization is silently skipped. Users must write an adapter class for each custom attribute type and register a custom `IValidationAttributeAdapterProvider` with limited composability (see [#4853](https://github.com/dotnet/aspnetcore/issues/4853)). We want to provide a simpler mechanism for custom validation attributes with multi-placeholder message templates to participate in the localization pipeline, either through self-describing formatting or external formatter registration.

- **Enable convention-based localization key selection.** Currently, the only way to localize built-in attribute messages (like `[Required]`) is to set `ErrorMessage` on every attribute instance, pointing to a resource key. There is no mechanism to globally translate built-in messages without modifying model classes (see [#4848](https://github.com/dotnet/aspnetcore/issues/4848), [#33073](https://github.com/dotnet/aspnetcore/issues/33073)). We want to support an optional convention-based approach where resource keys can be automatically derived for attributes that don't have an explicit `ErrorMessage`, enabling "zero-touch" localization of standard attributes.

## Non-goals

- **Localization of `IValidatableObject.Validate` messages.** The `IValidatableObject.Validate` method produces `ValidationResult` objects with messages that are expected to be already localized by the implementation. The `Validate` method receives a `ValidationContext` argument through which it can access DI to resolve `IStringLocalizer` or other localization services. Because the application code fully controls message generation in `Validate`, the framework does not intercept or transform these messages.

- **Asynchronous localization.** The `IStringLocalizerFactory` and `IStringLocalizer` APIs are fully synchronous. While `Microsoft.Extensions.Validation` supports asynchronous validation, the Blazor integration currently depends on the validation task completing synchronously (see [#7680](https://github.com/dotnet/aspnetcore/issues/7680)). Even if this constraint is relaxed in the future, both sync and async code paths should produce the same error messages, so adding async localization would add complexity without clear benefit.

## Proposed solution

The localization support is implemented at the level of the shared `Microsoft.Extensions.Validation` infrastructure, rather than in each individual framework integration (Minimal APIs, Blazor). This approach avoids logic duplication and provides a consistent experience regardless of the hosting framework.

The core extensibility consists of two new configurable delegate properties on `ValidationOptions`: `DisplayNameProvider` and `ErrorMessageProvider`. When set, the validation pipeline invokes these delegates to resolve localized display names and error messages. The delegates receive a readonly struct context containing the declaring type, the relevant attribute, the service provider, and (for error messages) the already-resolved display name. If the delegates are `null` or return `null`, the validation pipeline falls back to the default behavior. This design keeps the core validation library decoupled from any specific localization framework — it never resolves services directly.

The default `IStringLocalizer`-based implementation is provided in a new `Microsoft.Extensions.Validation.Localization` package, which depends on `Microsoft.Extensions.Validation` and `Microsoft.Extensions.Localization`. This package provides `AddValidationLocalization()` extension methods that wire up the delegates, register built-in attribute formatters, and configure the `IStringLocalizer` creation strategy. The package dependency graph is:

```text
Microsoft.Extensions.Validation
├── System.ComponentModel.Annotations
└── Microsoft.Extensions.Options

Microsoft.Extensions.Validation.Localization
├── Microsoft.Extensions.Validation
└── Microsoft.Extensions.Localization
    └── Microsoft.Extensions.Localization.Abstractions
```

To handle the `DataAnnotations` limitation where `ValidationAttribute.FormatErrorMessage` cannot accept a localized template, the localization package introduces the `IValidationAttributeFormatter` interface. This provides a framework-independent alternative to MVC's adapter system: formatters know how to substitute attribute-specific arguments (e.g., min/max values for `RangeAttribute`) into a localized message template. Built-in formatters for all standard multi-placeholder attributes are registered automatically.

The localization delegates are designed to be invocable outside of validation execution. This supports future features such as client-side validation code generation in Blazor SSR, where localized messages need to be resolved at render time before any validation occurs.

### Enable default localization

The simplest way to enable localization is to call `AddValidationLocalization()` after `AddValidation()`. This registers `IStringLocalizer`-based localization using the per-type resource file lookup strategy (e.g., `Resources/Models.Customer.fr.resx`).

```csharp
builder.Services.AddValidation();
builder.Services.AddValidationLocalization();
```

### Use a shared resource file

A common pattern is to use a single shared resource file for all localized validation strings. The generic `AddValidationLocalization<TResource>()` overload identifies the shared resource via the type parameter.

```csharp
builder.Services.AddValidation();
builder.Services.AddValidationLocalization<ValidationMessages>();
```

### Configure `IStringLocalizer` creation

The `IStringLocalizer` creation strategy can be controlled via the `LocalizerProvider` property on `ValidationLocalizationOptions`. This delegate receives the declaring type and the `IStringLocalizerFactory`, and returns the `IStringLocalizer` to use.

```csharp
builder.Services.AddValidationLocalization(options =>
{
    options.LocalizerProvider = (type, factory) => /* Custom logic */;
});
```

`AddValidationLocalization<TResource>()` is a convenience shorthand equivalent to setting `LocalizerProvider` to `(_, factory) => factory.Create(typeof(TResource))`.

### Use custom `IStringLocalizer` implementations

The localization pipeline uses whichever `IStringLocalizerFactory` is registered in DI. Registering a custom implementation (e.g., database-backed) replaces the default resource file-based localizer.

```csharp
builder.Services.AddValidation();
builder.Services.AddValidationLocalization();
builder.Services.AddSingleton<IStringLocalizerFactory, MyStringLocalizerFactory>();
```

### Map localization keys with `ErrorMessageKeyProvider`

By default, the localization pipeline uses the attribute's `ErrorMessage` value as the lookup key and skips localization when `ErrorMessage` is not set. `ErrorMessageKeyProvider` provides a fallback mechanism for deriving keys automatically, enabling convention-based localization without explicit `ErrorMessage` on every attribute.

```csharp
builder.Services.AddValidationLocalization(options =>
{
    options.ErrorMessageKeyProvider = (context) => $"{context.Attribute.GetType().Name}_Error";
});
```

### Localize custom validation attributes

Built-in attributes with default message templates are handled automatically by `AddValidationLocalization()`. For custom attributes with message templates using additional positional arguments beyond the display name, there are two options.

*Self-formatting* — implement `IValidationAttributeFormatter` directly on the attribute class:

```csharp
class CustomAttribute : ValidationAttribute, IValidationAttributeFormatter
{
    public string CustomProperty { get; }

    public string FormatErrorMessage(CultureInfo culture, string messageTemplate, string displayName)
        => string.Format(culture, messageTemplate, displayName, CustomProperty);
}
```

*External formatter registration* — register a formatter factory when you don't control the attribute source:

```csharp
builder.Services.AddValidationAttributeFormatter<CustomAttribute>(
    attribute => new CustomAttributeFormatter(attribute));
```

### Fully customize the localization logic

The `DisplayNameProvider` and `ErrorMessageProvider` delegates on `ValidationOptions` are the lowest-level extensibility points. They enable full customization without using `Microsoft.Extensions.Validation.Localization` or `IStringLocalizer`, useful for integrating existing translation libraries or proprietary localization services.

```csharp
builder.Services.AddSingleton<ILocalizationService, MyLocalizationService>();
builder.Services.AddValidation(options =>
{
    options.DisplayNameProvider = context =>
    {
        var service = context.Services.GetRequiredService<ILocalizationService>();
        return service.GetDisplayName(context.DeclaringType, context.Name, CultureInfo.CurrentUICulture);
    };

    options.ErrorMessageProvider = context => { /* Custom logic */ };
});
```

### Combine message providers

Because providers are delegates, they can be intercepted and composed for scenarios such as logging missing translations, providing localization overrides, or implementing layered fallback strategies.

```csharp
builder.Services.PostConfigure<ValidationOptions>(options =>
{
    var originalProvider = options.ErrorMessageProvider;

    options.ErrorMessageProvider = (context) =>
    {
        // If the user specified a custom message, process it with the standard pipeline.
        if (!string.IsNullOrEmpty(context.Attribute.ErrorMessage))
        {
            return originalProvider?.Invoke(context);
        }

        // Otherwise, retrieve/construct the message based on other context data.
        // ...
    };
});
```

See the [example library](https://github.com/dotnet/aspnetcore/tree/oroztocil/validation-localization/src/Validation/samples/StandardAttributeLocalization) in the prototype implementation for a composable localization package for built-in validation attributes.

## Assumptions

- The runtime culture for localization is determined by `CultureInfo.CurrentUICulture`, consistent with `IStringLocalizer` and `System.ComponentModel.DataAnnotations` conventions.
- When `DisplayAttribute.ResourceType` is set, the static resource accessor is expected to produce the correct localized name already. The new localization mechanism described here is not invoked in this case to avoid double-localization.
- When `ValidationAttribute.ErrorMessageResourceType` is set, the static resource accessor is expected to produce the correct localized mesage already. The new localization mechanism described here is not invoked in this case to avoid double-localization.
- Without enabling localization (leaving `DisplayNameProvider` and `ErrorMessageProvider` as `null`), the behavior of `Microsoft.Extensions.Validation` is completely unchanged.
- No new reflection is introduced in the default localization path, maintaining trimming and AOT compatibility.
- Users can resolve any services they need via the `IServiceProvider` instance provided in the delegate context structs, keeping the core validation library decoupled from DI.

## References

- [dotnet/aspnetcore#4848](https://github.com/dotnet/aspnetcore/issues/4848) — Localize built-in validation messages without per-attribute `ErrorMessage`
- [dotnet/aspnetcore#33073](https://github.com/dotnet/aspnetcore/issues/33073) — Default localization for validation attributes
- [dotnet/aspnetcore#4853](https://github.com/dotnet/aspnetcore/issues/4853) — Custom validation attribute localization pain points in MVC
- [dotnet/runtime#24084](https://github.com/dotnet/runtime/issues/24084) — DataAnnotations localization limitations
- [dotnet/aspnetcore#46349](https://github.com/dotnet/aspnetcore/issues/46349) — Microsoft.Extensions.Validation introduction
- [dotnet/aspnetcore#65647](https://github.com/dotnet/aspnetcore/issues/65647) — Source generator ignores `DisplayAttribute.ResourceType`
- [dotnet/aspnetcore#7680](https://github.com/dotnet/aspnetcore/issues/7680) — Async validation in Blazor
- [dotnet/aspnetcore#65460](https://github.com/dotnet/aspnetcore/pull/65460) — Prototype implementation
- [Standard attribute localization sample](https://github.com/dotnet/aspnetcore/tree/oroztocil/validation-localization/src/Validation/samples/StandardAttributeLocalization) — Example composable localization package
- [.NET Design Guidelines: Events and Callbacks](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/events-and-callbacks) — Guidance on delegate vs. named delegate design
