This document proposes a design for adding first-class localization support for validation error messages and display names to `Microsoft.Extensions.Validation` and transitively to Blazor and Minimal APIs that consume the package.

The proposed design:

- Adds generic localization extensibility to `Microsoft.Extensions.Validation`.
- Provides a default implementation based on integration with `Microsoft.Extensions.Localization`.
- Provides feature parity with ASP.NET Core MVC, while allowing more flexibility to address its remaining pain points.

**The document is intended for preliminary design discussion. Formal proposals intended for API review will be posted later.**

## 1. Background and motivation

Internationalized applications need to present validation errors in the user's language. Furthermore, even applications using English might want to customize the built-in validation messages. However, there is currently only a limited support for customization and localization of validation error messages and display names of the validated data members.

### 1.1. System.ComponentModel.DataAnnotations

`System.ComponentModel.DataAnnotations` is the standard .NET API for declarative validation. The only localization mechanism it provides out of the box is static property localization via `ErrorMessageResourceType`/`ErrorMessageResourceName` (and the analogous `DisplayAttribute.ResourceType`). To use it, each attribute instance must be annotated with explicit resource type and name references, e.g.:

```csharp
[Display(ResourceType = typeof(MyResources), Name = nameof(MyResources.CustomerName))]
[Required(ErrorMessageResourceType = typeof(MyResources), ErrorMessageResourceName = nameof(MyResources.RequiredError))]
public string Name { get; set; }
```

The resource type is typically generated from resource files during compile-time as a static class that handles run-time lookup based on the value of `CultureInfo.CurrentUICulture`.

This approach has significant limitations:

- **Verbose** — each attribute instance must be annotated individually.
- **Requires recompilation** — adding or changing translations requires recompiling the application.
- **Not suitable for runtime data sources** — resource keys and types must be known at compile time, making it impractical to load translations from databases, JSON files, or other dynamic sources.

Alternatively, if the user specifies `ErrorMessage` on a validation attribute (or `Name` on a `DisplayAttribute`), this value is used as-is with no run-time localization.

```csharp
[Required(ErrorMessage = "Le nom d'utilisateur est requis")]
public string? Username { get; set; }
```

If neither set of properties is specified, the default attribute message is used. For built-in attributes, this means using the non-configurable English messages defined in the BCL.

The `DataAnnotations` API does not expose any mechanism for globally intercepting or replacing the error message generation process. Its constraints have historically caused problems for localization:

- **`FormatErrorMessage(string name)` cannot accept a localized template.** The method signature is fixed — it only receives the display name. There is no way to substitute a localized template at call time; the attribute always formats using its own built-in or `ErrorMessage`-set template. This is the reason why MVC had to implement the wrapper mechanism of attribute adapters (see below) and requires additional work for custom validation attributes.
- **Localizing built-in attributes requires explicit `ErrorMessage` on every instance.** To localize `[Required]`, the user must specify the custom message on every attribute instance, or create derived re-implementations of the existing attributes. This is a long-standing request: see [#4848](https://github.com/dotnet/aspnetcore/issues/4848), [#33073](https://github.com/dotnet/aspnetcore/issues/33073), [dotnet/runtime#24084](https://github.com/dotnet/runtime/issues/24084).

### 1.2. Microsoft.Extensions.Validation

`Microsoft.Extensions.Validation` ([introduced in .NET 10](https://github.com/dotnet/aspnetcore/issues/46349)) is a framework-independent validation infrastructure built on `System.ComponentModel.DataAnnotations`. It provides compile-time validatable type discovery with metadata source-generation, and a validation pipeline that handles complex types, properties and method parameters.

The package serves as the official solution for validation in:

- **Minimal APIs**  — integrated as an endpoint filter,
- **Blazor** — integrated for form validation via the built-in `DataAnnotationsValidator` component.

The package currently provides no localization or message customization features apart from those built into `ValidationAttribute` and `DisplayAttribute` (as described above). Error messages come directly from `ValidationAttribute.GetValidationResult()` and display names are resolved at source-generation time from `DisplayAttribute.Name` values.

Furthermore, the validation metadata source generator currently ignores the `DisplayAttribute.ResourceType` property, meaning that display names currently cannot be localized at all.

### 1.3. Microsoft.Extensions.Localization

`Microsoft.Extensions.Localization` provides the standard runtime localization API in ASP.NET Core. The key interfaces are:

- **`IStringLocalizer`** — looks up localized strings by key, based on the value of `CultureInfo.CurrentUICulture`,
- **`IStringLocalizerFactory`** — creates `IStringLocalizer` instances.

The package provides default implementation of both interfaces using resource files as the backing data source. Users can implement their own localizers for other sources such as databases, JSON files, etc.

There is no standard integration between `Microsoft.Extensions.Localization` and `System.ComponentModel.DataAnnotations` or `Microsoft.Extensions.Validation`.

### 1.4. ASP.NET Core MVC

ASP.NET Core MVC provides its own localization layer on top of `System.ComponentModel.DataAnnotations`, using `IStringLocalizer` and a system of validation attribute *adapters*.

This design is intended to work around the previously mentioned `DataAnnotations` limitation: `ValidationAttribute.FormatErrorMessage(string name)` only accepts the display name and uses its own internally retrieved message template, so there is no way to inject a localized template at call time. Many built-in attributes use additional positional placeholders — for example, `RangeAttribute` formats `"The {0} field must be between {1} and {2}."` where `{1}` and `{2}` are the minimum and maximum values. The adapters wrap each attribute type and handle localization by calling `IStringLocalizer` with the attribute's `ErrorMessage` as the lookup key, then formatting the localized template with the correct attribute-specific arguments.

The MVC validation pipeline retrieves adapters for specific attributes using a `IValidationAttributeAdapterProvider` factory. The default `ValidationAttributeAdapterProvider` implementation maps built-in attributes to their adapters and returns `null` for other attributes, which means the localizer is bypassed entirely.

The MVC adapter system has notable pain points:

- **MVC-specific** — the adapter system is tightly coupled to MVC's model metadata and model binding pipeline. It cannot be reused in Minimal APIs, Blazor, or non-web hosts.
- **No localization for custom attributes** — custom `ValidationAttribute` implementations have no adapter registered by default, so localization is silently skipped. Users must write an adapter class for each attribute type, then register a custom `IValidationAttributeAdapterProvider` with a limited composability with other providers. This has been a recurring complaint ([dotnet/aspnetcore#4853](https://github.com/dotnet/aspnetcore/issues/4853)).
- **No convention-based key selection** — the only way to localize built-in attributes is to set `ErrorMessage` on every attribute instance. There is no mechanism to globally translate built-in messages without modifying model classes ([dotnet/aspnetcore#4848](https://github.com/dotnet/aspnetcore/issues/4848)).

## 2. Requirements

The general goal is to enable localization for validation messages emitted by `Microsoft.Extensions.Validation`. The API and the default implementation should support the feature set of ASP.NET Core MVC, while  allowing more flexibility to cover the needs of various consumers of the general-purpose validation package.

### 2.1. Functional

- **Error message localization** — provide a general extensibility mechanism in `ValidationOptions` that allows applications to localize or replace validation error messages at runtime.
- **Display name localization** — provide an analogous extensibility mechanism for resolving display names at runtime.
- **Default `IStringLocalizer`-based implementation** — provide a ready-to-use implementation that integrates with `Microsoft.Extensions.Localization` to localize validation error messages and display names via `IStringLocalizer`, looking up `ValidationAttribute.ErrorMessage` (when set) or a convention-based key in the current UI culture. Users should be able to configure the `IStringLocalizer` creation strategy to support both per-type and shared resource file approaches. Users should be able to provide custom `IStringLocalizerFactory` implementations to support non-resource localization sources.
- **Convention-based key selection** — support convention-based resource key selection for attributes that do not have an explicit `ErrorMessage` set.
- **Static resource display name localization** — support static resource localization of display names with `DisplayAttribute.ResourceType` in the source generated metadata.
- **Custom attribute support** — allow custom validation attributes to participate in the localization pipeline with attribute-specific formatted messages.

### 2.2. Non-functional

- **Framework compatibility** — the localization infrastructure must work with both Minimal APIs (endpoint filter) and Blazor (`DataAnnotationsValidator` component).
- **Forward compatibility with client-side validation** — the design should support future client-side validation code generation for Blazor SSR. Specifically, it should be possible to resolve localized and formatted error messages and display names outside of validation execution, so that a code generator can emit client-side validation rules with the correct localized messages at render time.
- **Opt-in** — without enabling localization the behavior of `Microsoft.Extensions.Validation` is completely unchanged and uses the user-hardcoded or default messages as before.
- **Trimming and AOT compatibility** — no new reflection is introduced in the default localization path.

### 2.3. Out of scope

- Support for localization of messages from `ValidationResult` produced by `IValidatableObject.Validate`.
  - The messages are expected to be localized already. The `Validate` method can access DI via the `ValidationContext` argument to resolve `IStringLocalizer` or other services to perform localization.
  - Alternatively, we could implement an opt-in feature to enable treating these messages as keys for localization.
- Support async localization. Some localizer implementations might benefit from it. However, there are multiple reasons against:
  - The `IStringLocalizerFactory` and `IStringLocalizer` API is fully synchronous.
  - Typical data sources (local files, DBs) can be read synchronously in .NET.
  - While the validation API of `Microsoft.Extensions.Validation` is async, the Blazor integration (currently) depends on the validation task completing synchronously.
- Support for `DisplayNameAttribute`.
  - Only the newer `DisplayAttribute` is supported.
- Support for localization of display names without `DisplayAttribute`.
  - If a property/parameter/type does not have a `DisplayAttribute`, the validation pipeline uses the CLR name directly, without invoking display name localization.
- Shipping localized built-in attribute messages.
  - The proposed design allows implementing this as a package in a composable manner. A candidate for a Community Toolkit package?

## 3. Design proposal

The basic architecture of the localization support is proposed as follows:

- Localization is implemented at the level of the shared validation infrastructure of `Microsoft.Extensions.Validation`, rather than in each individual framework integration (Minimal APIs, Blazor) to avoid logic duplication and provide consistent experience to the users.
- The entrypoint into the validation infrastructure is the `ValidationOptions` class. The proposal adds generic extensibility point for localization and customization of validation messages in the form of configurable delegate properties in `ValidationOptions`.
- Other localization-related code, including the implementation of the `IStringLocalizer`-based localization and extension methods for service registration, is put into a new package `Microsoft.Extensions.Validation.Localization` to minimize the bundle size increase for applications that do not require validation localization. The dependency graph looks like this:

    ```text
    Microsoft.Extensions.Validation
    ├── System.ComponentModel.Annotations
    └── Microsoft.Extensions.Options

    Microsoft.Extensions.Validation.Localization
    ├── Microsoft.Extensions.Validation
    └── Microsoft.Extensions.Localization
    └── Microsoft.Extensions.Localization.Abstractions
    ```

### 3.1. Scenarios

Following walkthrough of common user scenarios introduces the proposed API.

#### Enable default localization

The simplest way to enable localization is to call `AddValidationLocalization()` after `AddValidation()`. This registers the default `IStringLocalizer`-based localization using the per-type resource file lookup strategy (e.g., `Resources/Models.Customer.fr.resx`).

```csharp
builder.Services.AddValidation();
builder.Services.AddValidationLocalization();
```

#### Use a shared resource file

A common pattern is to to use a single shared resource file for localized strings. To use this strategy, call the generic `AddValidationLocalization<TResource>()` overload. The type parameter `TResource` identifies the shared resource file.

```csharp
builder.Services.AddValidation();
builder.Services.AddValidationLocalization<ValidationMessages>();
```

#### Configure `IStringLocalizer` creation

The `IStringLocalizer`  creation strategy can be controlled via the `LocalizerProvider` property on `ValidationLocalizationOptions`. This delegate receives the declaring type and the `IStringLocalizerFactory`, and returns the `IStringLocalizer` to use.

```csharp
builder.Services.AddValidationLocalization(options =>
{
    options.LocalizerProvider = (type, factory) => /* Custom logic */;
});
```

Note that `AddValidationLocalization<TResource>()` is a convenience shorthand equivalent to calling the non-generic overload with `LocalizerProvider` set to `(_, factory) => factory.Create(typeof(TResource))`.

#### Use custom `IStringLocalizer` implementations

The localization pipeline enabled by `AddValidationLocalization` uses whichever `IStringLocalizerFactory` that it receives from the DI container. It ensures that it has an implementation available by calling the `AddLocalization` extension method from `Microsoft.Extensions.Localization` which registers the `ResourceManagerStringLocalizerFactory` implementation.

This can be overridden by registering a different implementation (e.g., database-backed one) which will be picked up by validation localization. 

```csharp
builder.Services.AddValidation();
builder.Services.AddValidationLocalization();
builder.Services.AddSingleton<IStringLocalizerFactory, MyStringLocalizerFactory>();
```

#### Map localization keys with `ErrorMessageKeyProvider`

By default, the localization pipeline uses the attribute's `ErrorMessage` value as the localization lookup key (e.g., `[Required(ErrorMessage = "SomeKey")]`) and skips localization when the `ErrorMessage` is not set. The `ErrorMessageKeyProvider` delegate gives the user an optional fallback mechanism for creating the localization key, enabling convention-based key selection without requiring explicit `ErrorMessage` on every attribute instance.

```csharp

builder.Services.AddValidationLocalization(options =>
{
    options.ErrorMessageKeyProvider = (context) => $"{context.Attribute.GetType().Name}_Error";
});
```

Note that this feature is intended for users who want to stay within the default `IStringLocalizer`-based localization pipeline but only add a key fallback mechanism. See the _Fully customize the localization logic scenario_ below for more complex customization scenarios.

#### Localize custom validation attributes

As explained in Section 1.4, the localization system needs to be able to properly format error message templates with attribute-specific arguments. Built-in attributes with the default message templates are handled automatically by `AddValidationLocalization()` — no additional configuration is needed.

For custom validation attributes with message templates using other positional arguments than the display name, there are two options for making them compatible with localization (see `IValidationAttributeFormatter` and `ValidationAttributeFormatterRegistry` in section 3.3 for details):

*Option 1 — Self-formatting:* Implement `IValidationAttributeFormatter` directly on the attribute class. The localization pipeline detects this automatically — no registration needed.

```csharp
class CustomAttribute : ValidationAttribute, IValidationAttributeFormatter
{
    public string CustomProperty { get; }

    public string FormatErrorMessage(CultureInfo culture, string messageTemplate, string displayName)
        => string.Format(culture, messageTemplate, displayName, CustomProperty);
}
```

*Option 2 — External formatter registration:* Register a formatter factory when you don't control the attribute source.

```csharp
builder.Services.AddValidationAttributeFormatter<CustomAttribute>(
    attribute => new CustomAttributeFormatter(attribute));
```

#### Fully customize the localization logic

The `DisplayNameProvider` and `ErrorMessageProvider` delegates on `ValidationOptions` are the most flexible and low-level extensibility points. They allow full customization of how validation messages and display names are resolved, without using `Microsoft.Extensions.Validation.Localization` or `IStringLocalizer` at all. This is useful for integrating existing translation libraries, proprietary localization services, or more complex lookup strategies that go beyond `IStringLocalizer`'s simple key-based lookup.

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

#### Combine message providers

Because providers are delegates, they can be intercepted and composed. This pattern enables scenarios such as:

- **Logging missing translations** — wrap the original provider and log when a localization lookup returns `null` (resource not found), making it easy to identify untranslated strings during development.
- **Localization overrides** — return a new error message, optionally using the original provider as a fallback.
- **Localization fallbacks** — first invoke the original provider, optionally provide a fallback message if the original returns `null`.

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

See the [example library](https://github.com/dotnet/aspnetcore/tree/oroztocil/validation-localization/src/Validation/samples/StandardAttributeLocalization) in the prototype implementation showing how a localization package for the built-in validation attributes could be implemented.

### 3.2. API overview

The public API changes consist of:

**`Microsoft.Extensions.Validation`:**

- New `DisplayNameProvider` and `ErrorMessageProvider` delegate properties on `ValidationOptions`. If set, these are used by the validation pipeline to create localized error messages.
- New `readonly struct` context types `DisplayNameProviderContext` and `ErrorMessageProviderContext` passed as input into the localization delegates.

**`New package: Microsoft.Extensions.Validation.Localization`:**

- `AddValidationLocalization()` and `AddValidationLocalization<TResource>()` extension methods for registering the default `IStringLocalizer`-based localization.
- `ValidationLocalizationOptions` configuration class for the default localization implementation.
- `IValidationAttributeFormatter` interface for formatting error message templates with attribute-specific arguments.
- `ValidationAttributeFormatterRegistry` keyed registry of attribute formatter factories, with built-in formatters for all standard attributes. Configurable via `AddValidationAttributeFormatter<TAttribute>()`.
- `AddValidationAttributeFormatter<TAttribute>()` extension method for registering custom attribute formatters.

### 3.3 API details

#### `ValidationOptions` — new properties

Two new delegate properties are added to `ValidationOptions` in the core `Microsoft.Extensions.Validation` package:

```csharp
namespace Microsoft.Extensions.Validation;

public class ValidationOptions
{
    public Func<DisplayNameProviderContext, string?>? DisplayNameProvider { get; set; }
    public Func<ErrorMessageProviderContext, string?>? ErrorMessageProvider { get; set; }
}
```

These are the primary extensibility points for the core validation pipeline. They are configured globally for the entire application via `IConfigureOptions<ValidationOptions>` or directly in the `AddValidation(options => ...)` callback. Per-invocation overrides are also possible using the properties' public setters. Typical consumers (such as the Blazor and Minimal API integrations) would use the globally configured values to provide predicatable and consistent experience.

The `DisplayNameProvider` delegate is only invoked when a `DisplayAttribute` with a non-null `Name` is present **and** `DisplayAttribute.ResourceType` is not set — when `ResourceType` is set, the static accessor already produces the correct localized string and the provider is skipped to avoid double-localization.

The `ErrorMessageProvider` delegate is only invoked when `ValidationAttribute.ErrorMessageResourceType` is `null`. When `ErrorMessageResourceType` is set, the attribute already handles its own localization and the provider is bypassed entirely to avoid double-localization. When invoked, the provider is expected to return a fully formatted localized message (not a template). It is responsible for substituting the display name and any attribute-specific arguments into the localized template. If it returns `null`, the attribute's default message from `ValidationAttribute.GetValidationResult()` is used.

*Alternative designs:*

- Delegates were chosen over DI-resolved interfaces (e.g., `IErrorMessageProvider`) to keep the core validation library decoupled from DI. The core library never resolves services — instead it receives the delegates as parameters from `ValidationOptions`. If the delegates are `null`, default messages are used and validation works without any DI configuration. The `IServiceProvider` reference (the `ValidationContext` instance) is passed *into* the delegates for the delegate implementation's convenience (e.g., to resolve `IStringLocalizer`), but the core library itself never uses it. This means the localization package (`Microsoft.Extensions.Validation.Localization`) is just one possible way to wire up the delegates — applications can set them manually, in tests, or from any other source.

- Having taken the delegate route, named delegate types (e.g., `delegate string? DisplayNameProvider(in DisplayNameProviderContext context)`) could be used instead of `Func<DisplayNameProviderContext, string?>`. Named delegates could provide better readability (this is subjective) and support `in` (pass-by-reference) parameters, which could slightly reduce the overhead of the context structs. However, `Func<>` was chosen for the sake of simplicity (as recommended by the [design guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/events-and-callbacks)).

#### `DisplayNameProviderContext`

```csharp
namespace Microsoft.Extensions.Validation.Localization;

public readonly struct DisplayNameProviderContext
{
    public Type? DeclaringType { get; init; }
    public required string Name { get; init; }
    public required IServiceProvider Services { get; init; }
}
```

`DisplayNameProviderContext` is passed to the `DisplayNameProvider` delegate. The delegate is only invoked when a `DisplayAttribute` with a non-null `Name` is present **and** `DisplayAttribute.ResourceType` is not set — when `ResourceType` is set, the static accessor already produces the correct localized string and the provider is skipped to avoid double-localization.

`Name` is the value of `DisplayAttribute.Name`, used as the localization lookup key.

`DeclaringType` allows the localizer to be scoped to the resource associated with the declaring type, enabling per-type resource files as an alternative to a single shared resource. It is `null` for top-level parameter validation in Minimal APIs because parameters do not have a declaring type in the same sense as properties.

`Services` provides access to the application's `IServiceProvider`, allowing the delegate to resolve localization services (e.g., `IStringLocalizerFactory`) or any other registered services.

#### `ErrorMessageProviderContext`

```csharp
namespace Microsoft.Extensions.Validation.Localization;

public readonly struct ErrorMessageProviderContext
{
    public required ValidationAttribute Attribute { get; init; }
    public required string DisplayName { get; init; }
    public Type? DeclaringType { get; init; }
    public required IServiceProvider Services { get; init; }
}
```

`ErrorMessageProviderContext` is passed to the `ErrorMessageProvider` delegate.

`Attribute` provides access to all attribute properties, enabling the provider to implement make decisions based on the attribute type or extract attribute-specific values for formatting. The default implementation in `Microsoft.Extensions.Validation.Localization` builds upon this with the `IValidationAttributeFormatter` abstraction to provide a structured way of formatting localized message templates of both built-in and custom attributes.

`DisplayName` is the output of `DisplayNameProvider` (already localized if configured), so the `ErrorMessageProvider` can use it directly when formatting the message. This ensures that both display name and error message localization are consistent.

`DeclaringType` allows the localizer to be scoped to the resource associated with the declaring type, enabling per-type resource files as an alternative to a single shared resource. It is `null` for top-level parameter validation in Minimal APIs because parameters do not have a declaring type in the same sense as properties.

`Services` provides access to the application's `IServiceProvider`, allowing the delegate to resolve localization services (e.g., `IStringLocalizerFactory`) or any other registered services.

*Alternative designs:*

- Both context structs intentionally exclude validation-execution-related types (such as `ValidationContext`, `ValidateContext`, or `ValidationResult`). This is a deliberate design choice: localized and formatted messages must be retrievable *outside* of validation execution. This facilitates adding features suchs as client-side validation code generation in Blazor SSR, where the code generator needs to emit validation rules with correctly localized messages at render time — before any validation has occurred.
- We could inject a pre-resolved `IStringLocalizerFactory` instance or even an `IStringLocalizer` instance as part of the delegate context (possibly instead of the `IServiceProvider` instance) for `ErrorMessageProvider` and `DisplayNameProvider`. This could simplify (and possibly optimize) the providers. However, it would reduce the flexibility of the solution and tie the validation pipeline to the `IStringLocalizer` interface (and consequently the `Microsoft.Extensions.Validation` package to `Microsoft.Extensions.Localization`).

#### `IValidationAttributeFormatter` and `ValidationAttributeFormatterRegistry`

As described in section 1.4, the `ValidationAttribute.FormatErrorMessage` method cannot accept a localized template, and MVC worked around this with adapter classes coupled to its model metadata pipeline. `IValidationAttributeFormatter` provides a simpler, framework-independent equivalent focused solely on message formatting:

```csharp
namespace Microsoft.Extensions.Validation.Localization;

public interface IValidationAttributeFormatter
{
    string FormatErrorMessage(CultureInfo culture, string messageTemplate, string displayName);
}
```

Formatters are registered in `ValidationAttributeFormatterRegistry`, a keyed registry where each attribute type maps to a factory that creates the appropriate formatter. Built-in formatters for all standard attributes with multi-placeholder templates (`RangeAttribute`, `StringLengthAttribute`, `CompareAttribute`, `MinLengthAttribute`, `MaxLengthAttribute`, `LengthAttribute`, `RegularExpressionAttribute`, `FileExtensionsAttribute`) are registered automatically by `AddValidationLocalization()`. Attributes that only use `{0}` (the display name) do not require a formatter — the localization pipeline handles them directly.

```csharp
namespace Microsoft.Extensions.Validation.Localization;

public sealed class ValidationAttributeFormatterRegistry
{
    public void AddFormatter<TAttribute>(Func<TAttribute, IValidationAttributeFormatter> factory)
        where TAttribute : ValidationAttribute;

    public IValidationAttributeFormatter? GetFormatter(ValidationAttribute attribute);
}
```

The resolution order of `GetFormatter` is:

1. If the attribute implements `IValidationAttributeFormatter` itself (self-formatting), it is returned directly.
2. If a factory is registered for the attribute's type, it is used to create a formatter.
3. Otherwise, `null` is returned. The localization pipeline falls back to formatting the template with only the display name.

#### `AddValidationLocalization` and `ValidationLocalizationOptions`

```csharp
namespace Microsoft.Extensions.DependencyInjection;

public static class ValidationLocalizationServiceCollectionExtensions
{
    public static IServiceCollection AddValidationLocalization(
        this IServiceCollection services,
        Action<ValidationLocalizationOptions>? configureOptions = null);

    public static IServiceCollection AddValidationLocalization<TResource>(
        this IServiceCollection services,
        Action<ValidationLocalizationOptions>? configureOptions = null);

    public static IServiceCollection AddValidationAttributeFormatter<TAttribute>(
        this IServiceCollection services,
        Func<TAttribute, IValidationAttributeFormatter> factory)
        where TAttribute : ValidationAttribute;
}
```

`AddValidationLocalization()` and `AddValidationLocalization<TResource>()` are the primary entry points for enabling `IStringLocalizer`-based localization. Both overloads:

1. Call `services.AddLocalization()` to register `IStringLocalizerFactory` (idempotent).
2. Register built-in attribute formatters in `ValidationAttributeFormatterRegistry` via the options pattern.
3. Register `ValidationLocalizationSetup` as an `IConfigureOptions<ValidationOptions>`, which sets `ValidationOptions.ErrorMessageProvider` and `ValidationOptions.DisplayNameProvider` (using `??=`, so user-set providers are not overwritten).

The `AddValidationLocalization<TResource>()` overload pre-configures `LocalizerProvider` to always use the resource file associated with `TResource`:

```csharp
options.LocalizerProvider = (_, factory) => factory.Create(typeof(TResource));
```

```csharp
namespace Microsoft.Extensions.Validation.Localization;

public sealed class ValidationLocalizationOptions
{
    public Func<Type, IStringLocalizerFactory, IStringLocalizer>? LocalizerProvider { get; set; }
    public Func<ErrorMessageProviderContext, string?>? ErrorMessageKeyProvider { get; set; }
}
```

`ValidationLocalizationOptions` exposes two configuration points:

- **`LocalizerProvider`** (`Func<Type, IStringLocalizerFactory, IStringLocalizer>?`) — controls which `IStringLocalizer` is used for a given declaring type. When `null` (the default), `IStringLocalizerFactory.Create(declaringType)` is used, which follows the standard per-type resource file naming convention.
- **`ErrorMessageKeyProvider`** (`Func<ErrorMessageProviderContext, string?>?`) — controls the resource key used to look up the error message template. When `null` (the default), only attributes with `ErrorMessage` set are localized. When configured, it is called as a fallback for attributes without an explicit `ErrorMessage`, enabling convention-based key selection (e.g., `context => $"{context.Attribute.GetType().Name}_ValidationError"`).

## 4. Implementation proposal

See the feature-complete prototype implementation in [dotnet/aspnetcore#65460](https://github.com/dotnet/aspnetcore/pull/65460).
