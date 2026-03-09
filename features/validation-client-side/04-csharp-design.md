# C# Server-Side Design for Client-Side Validation Attribute Emission

## 1. Problem Statement

Blazor SSR forms rendered with `<EditForm>` and `<InputText>`, `<InputNumber>`, etc. do **not** emit the `data-val-*` HTML attributes needed for client-side validation. The JS validation prototype (built in Step 5) requires these attributes to discover validation rules at runtime.

MVC solves this via `InputTagHelper` → `DefaultHtmlGenerator` → `ValidationHtmlAttributeProvider` → validation attribute adapters. This pipeline is tightly coupled to MVC's `ModelMetadata`, `ViewContext`, and `TagBuilder` and **cannot be reused** by Blazor.

This document designs a Blazor-native mechanism to emit `data-val-*` attributes from `InputBase<T>` components, compatible with the JS validation library protocol.

---

## 2. Design Decisions Summary

| Decision | Choice | Rationale |
|---|---|---|
| Integration point | Cascaded service consumed by `InputBase<T>` | Keeps InputBase clean; service provides metadata on demand |
| Opt-in mechanism | `<ClientSideValidator />` component | Follows existing `<DataAnnotationsValidator />` pattern |
| ValidationMessage / Summary | Modify when client validation service is cascaded | Backwards-compatible; no change without the service |
| Custom validator extensibility | Blazor-specific `IClientValidationAdapter` + provider | DI-first design, no MVC dependency |
| Metadata discovery | Source generator when available, reflection fallback | AOT-friendly with graceful degradation |
| Error message localization | `ValidationOptions.ErrorMessageProvider` / `DisplayNameProvider` delegates | Designed for this use case per [#65539](https://github.com/dotnet/aspnetcore/issues/65539) |
| Script emission | Auto-emit `<script>` tag by default, opt-out parameter | Minimal ceremony for common case |

---

## 3. Architecture Overview

```
┌──────────────────────────────────────────────────────────────────────┐
│                          EditForm                                    │
│  ┌────────────────────────────────────────────────────────────────┐  │
│  │              <ClientSideValidator />                           │  │
│  │  - Cascades IClientValidationService                          │  │
│  │  - Optionally emits <script> tag                              │  │
│  │  - Resolves adapters from DI                                  │  │
│  └────────────────────────────────────────────────────────────────┘  │
│                              │                                       │
│                    cascades IClientValidationService                  │
│                              │                                       │
│  ┌───────────────┐  ┌───────────────┐  ┌──────────────────────┐     │
│  │  InputText    │  │  InputNumber  │  │  InputSelect         │     │
│  │  (InputBase)  │  │  (InputBase)  │  │  (InputBase)         │     │
│  │  queries      │  │  queries      │  │  queries             │     │
│  │  service for  │  │  service for  │  │  service for         │     │
│  │  data-val-*   │  │  data-val-*   │  │  data-val-*          │     │
│  └───────────────┘  └───────────────┘  └──────────────────────┘     │
│                                                                      │
│  ┌─────────────────────────┐  ┌──────────────────────────────────┐  │
│  │  ValidationMessage<T>   │  │  ValidationSummary               │  │
│  │  adds data-valmsg-for   │  │  adds data-valmsg-summary="true" │  │
│  └─────────────────────────┘  └──────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────┘
                              │
              IClientValidationService
                              │
         ┌────────────────────┼────────────────────┐
         │                    │                    │
  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐
  │ PropertyInfo │  │ Source-Gen   │  │ Adapter          │
  │ Reflection   │  │ Metadata     │  │ Registry         │
  │ (fallback)   │  │ (preferred)  │  │ (maps attrs to   │
  │              │  │              │  │  data-val-*)     │
  └──────────────┘  └──────────────┘  └──────────────────┘
```

---

## 4. MVC Pipeline Analysis (Reference)

### 4.1 How MVC Emits data-val-* Attributes

MVC's pipeline for a single `<input asp-for="Model.Name">`:

1. **`InputTagHelper.ProcessAsync()`** — calls `Generator.GenerateTextInput()`
2. **`DefaultHtmlGenerator.AddValidationAttributes()`** — retrieves `IValidationHtmlAttributeProvider`
3. **`DefaultValidationHtmlAttributeProvider.AddValidationAttributes()`** — gets `IClientModelValidator` list from cache
4. **`DataAnnotationsClientModelValidatorProvider.CreateValidators()`** — for each `ValidationAttribute`, gets an adapter via `IValidationAttributeAdapterProvider`
5. **`ValidationAttributeAdapterProvider.GetAttributeAdapter()`** — type-switch mapping:
   - `RequiredAttribute` → `RequiredAttributeAdapter`
   - `RangeAttribute` → `RangeAttributeAdapter`
   - `StringLengthAttribute` → `StringLengthAttributeAdapter`
   - `EmailAddressAttribute` → `DataTypeAttributeAdapter("data-val-email")`
   - etc.
6. **Each adapter's `AddValidation()`** — calls `MergeAttribute()` to write into the HTML attributes dictionary:
   ```csharp
   MergeAttribute(context.Attributes, "data-val", "true");
   MergeAttribute(context.Attributes, "data-val-required", GetErrorMessage(context));
   ```

### 4.2 Key Differences: MVC vs Blazor

| Aspect | MVC | Blazor |
|---|---|---|
| Rendering | Tag Helpers + Razor templates | `RenderTreeBuilder` in C# components |
| Metadata | `ModelMetadata` (rich, pre-computed) | `FieldIdentifier` (model ref + field name string) |
| Attribute access | `ModelMetadata.ValidatorMetadata` | Must use reflection or source generator |
| HTML attributes | `IDictionary<string, string>` on `TagBuilder` | `AdditionalAttributes` + `builder.AddAttribute()` |
| Validation components | Tag helpers on `<input>`, `<span>` | Separate `<ValidationMessage>`, `<ValidationSummary>` |
| Context available | `ViewContext`, `ActionContext`, `HttpContext` | `EditContext`, cascading parameters |
| Display names | `ModelMetadata.GetDisplayName()` | `ExpressionMemberAccessor.GetDisplayName()` or source gen |
| Localization | `IStringLocalizer` via adapter constructor | `ValidationOptions.ErrorMessageProvider` delegate |

### 4.3 What Can Be Reused

- **Protocol**: The `data-val-*` attribute naming convention is the same — our JS library already parses it
- **Adapter concept**: The pattern of mapping `ValidationAttribute` → `data-val-*` attributes translates directly
- **Attribute names**: Same `data-val-required`, `data-val-range-min`, `data-val-length-max`, etc.

### 4.4 What Cannot Be Reused

- **Classes**: `ClientModelValidationContext`, `AttributeAdapterBase`, `ValidationAttributeAdapter` — all depend on `ModelMetadata`, `ActionContext`
- **Provider**: `ValidationAttributeAdapterProvider` — returns MVC-specific `IAttributeAdapter`
- **Localization**: MVC adapters take `IStringLocalizer` in constructor; we use `ValidationOptions` delegates

---

## 5. Detailed Component Design

### 5.1 `IClientValidationService` — Core Service Interface

```csharp
namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Service that provides client-side validation HTML attributes for form fields.
/// Cascaded by <see cref="ClientSideValidator"/> to enable automatic
/// <c>data-val-*</c> attribute emission on input components.
/// </summary>
public interface IClientValidationService
{
    /// <summary>
    /// Gets the <c>data-val-*</c> HTML attributes for the specified field.
    /// </summary>
    /// <param name="fieldIdentifier">The field to get validation attributes for.</param>
    /// <returns>
    /// A dictionary of HTML attribute name/value pairs (e.g., <c>data-val-required</c> → error message),
    /// or an empty dictionary if no client validation rules apply.
    /// </returns>
    IReadOnlyDictionary<string, string> GetValidationAttributes(FieldIdentifier fieldIdentifier);
}
```

**Responsibilities:**
- Discovers `ValidationAttribute`s on the property identified by `FieldIdentifier`
- Maps each attribute to `data-val-*` key/value pairs via adapters
- Resolves localized error messages and display names
- Caches results per (Type, FieldName) pair

**Location:** `src/Components/Forms/src/IClientValidationService.cs`

### 5.2 `ClientValidationContext` — Adapter Context

```csharp
namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Context passed to <see cref="IClientValidationAdapter"/> implementations
/// to emit <c>data-val-*</c> HTML attributes.
/// </summary>
public sealed class ClientValidationContext
{
    /// <summary>
    /// Gets the HTML attributes dictionary. Adapters add <c>data-val-*</c>
    /// entries to this dictionary.
    /// </summary>
    public IDictionary<string, string> Attributes { get; }

    /// <summary>
    /// Gets the <see cref="ValidationAttribute"/> being adapted.
    /// </summary>
    public ValidationAttribute ValidationAttribute { get; }

    /// <summary>
    /// Gets the display name for the field (already localized if a
    /// <see cref="ValidationOptions.DisplayNameProvider"/> is configured).
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the type that declares the property being validated.
    /// </summary>
    public Type DeclaringType { get; }

    /// <summary>
    /// Gets the property name being validated.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Gets the application's <see cref="IServiceProvider"/>.
    /// </summary>
    public IServiceProvider Services { get; }

    /// <summary>
    /// Adds an attribute to the dictionary if the key does not already exist.
    /// Analogous to MVC's <c>MergeAttribute</c>.
    /// </summary>
    public bool MergeAttribute(string key, string value)
    {
        if (Attributes.ContainsKey(key))
        {
            return false;
        }

        Attributes[key] = value;
        return true;
    }
}
```

**Compared to MVC's `ClientModelValidationContext`:**
- No dependency on `ActionContext`, `ModelMetadata`, `IModelMetadataProvider`
- Includes `DisplayName` (pre-resolved) and `Services` for custom adapters that need DI
- `MergeAttribute` helper method is on the context itself rather than a base class

**Location:** `src/Components/Forms/src/ClientValidationContext.cs`

### 5.3 `IClientValidationAdapter` — Adapter Interface

```csharp
namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Defines a mapping from a <see cref="ValidationAttribute"/> to
/// <c>data-val-*</c> HTML attributes for client-side validation.
/// </summary>
public interface IClientValidationAdapter
{
    /// <summary>
    /// Adds <c>data-val-*</c> attributes to the <paramref name="context"/>'s
    /// attribute dictionary for the given validation attribute.
    /// </summary>
    /// <param name="context">
    /// The <see cref="ClientValidationContext"/> containing the attribute
    /// dictionary and metadata.
    /// </param>
    void AddClientValidation(ClientValidationContext context);

    /// <summary>
    /// Gets the localized error message for this validation rule.
    /// </summary>
    /// <param name="context">
    /// The <see cref="ClientValidationContext"/> containing metadata
    /// needed for message formatting.
    /// </param>
    /// <returns>The formatted, localized error message.</returns>
    string GetErrorMessage(ClientValidationContext context);
}
```

**Location:** `src/Components/Forms/src/IClientValidationAdapter.cs`

### 5.4 `IClientValidationAdapterProvider` — Adapter Factory

```csharp
namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Factory that creates <see cref="IClientValidationAdapter"/> instances
/// for <see cref="ValidationAttribute"/>s.
/// </summary>
public interface IClientValidationAdapterProvider
{
    /// <summary>
    /// Returns an <see cref="IClientValidationAdapter"/> for the given
    /// <paramref name="attribute"/>, or <see langword="null"/> if no adapter
    /// exists for that attribute type.
    /// </summary>
    /// <param name="attribute">The validation attribute to adapt.</param>
    /// <returns>An adapter, or <see langword="null"/>.</returns>
    IClientValidationAdapter? GetAdapter(ValidationAttribute attribute);
}
```

**Location:** `src/Components/Forms/src/IClientValidationAdapterProvider.cs`

### 5.5 Built-In Adapters

The following adapters are registered by default, mirroring MVC's `ValidationAttributeAdapterProvider`:

| ValidationAttribute | Adapter Class | data-val-* Attributes |
|---|---|---|
| `RequiredAttribute` | `RequiredClientAdapter` | `data-val-required` |
| `StringLengthAttribute` | `StringLengthClientAdapter` | `data-val-length`, `-max`, `-min` |
| `MinLengthAttribute` | `MinLengthClientAdapter` | `data-val-minlength`, `-min` |
| `MaxLengthAttribute` | `MaxLengthClientAdapter` | `data-val-maxlength`, `-max` |
| `RangeAttribute` | `RangeClientAdapter` | `data-val-range`, `-min`, `-max` |
| `RegularExpressionAttribute` | `RegexClientAdapter` | `data-val-regex`, `-pattern` |
| `EmailAddressAttribute` | `DataTypeClientAdapter` | `data-val-email` |
| `UrlAttribute` | `DataTypeClientAdapter` | `data-val-url` |
| `CreditCardAttribute` | `DataTypeClientAdapter` | `data-val-creditcard` |
| `PhoneAttribute` | `DataTypeClientAdapter` | `data-val-phone` |
| `CompareAttribute` | `CompareClientAdapter` | `data-val-equalto`, `-other` |

**Example — `RequiredClientAdapter`:**

```csharp
namespace Microsoft.AspNetCore.Components.Forms.ClientValidation;

internal sealed class RequiredClientAdapter : IClientValidationAdapter
{
    private readonly RequiredAttribute _attribute;

    public RequiredClientAdapter(RequiredAttribute attribute)
    {
        _attribute = attribute;
    }

    public void AddClientValidation(ClientValidationContext context)
    {
        context.MergeAttribute("data-val", "true");
        context.MergeAttribute("data-val-required", GetErrorMessage(context));
    }

    public string GetErrorMessage(ClientValidationContext context)
    {
        return ResolveErrorMessage(context, _attribute, context.DisplayName);
    }
}
```

**Example — `RangeClientAdapter`:**

```csharp
internal sealed class RangeClientAdapter : IClientValidationAdapter
{
    private readonly RangeAttribute _attribute;

    public RangeClientAdapter(RangeAttribute attribute)
    {
        _attribute = attribute;
        // Trigger conversion (same trick as MVC's RangeAttributeAdapter)
        attribute.IsValid(3);
    }

    public void AddClientValidation(ClientValidationContext context)
    {
        context.MergeAttribute("data-val", "true");
        context.MergeAttribute("data-val-range", GetErrorMessage(context));
        context.MergeAttribute("data-val-range-min",
            Convert.ToString(_attribute.Minimum, CultureInfo.InvariantCulture)!);
        context.MergeAttribute("data-val-range-max",
            Convert.ToString(_attribute.Maximum, CultureInfo.InvariantCulture)!);
    }

    public string GetErrorMessage(ClientValidationContext context)
    {
        return ResolveErrorMessage(context, _attribute,
            context.DisplayName, _attribute.Minimum, _attribute.Maximum);
    }
}
```

**Location:** `src/Components/Forms/src/ClientValidation/` (one file per adapter, or a single `BuiltInAdapters.cs`)

### 5.6 `DefaultClientValidationAdapterProvider`

```csharp
namespace Microsoft.AspNetCore.Components.Forms.ClientValidation;

/// <summary>
/// Default implementation that maps built-in <see cref="ValidationAttribute"/>
/// types to <see cref="IClientValidationAdapter"/> instances.
/// Falls back to registered <see cref="IClientValidationAdapterProvider"/>
/// services for custom attributes.
/// </summary>
internal sealed class DefaultClientValidationAdapterProvider : IClientValidationAdapterProvider
{
    private readonly IEnumerable<IClientValidationAdapterProvider> _customProviders;

    public DefaultClientValidationAdapterProvider(
        IEnumerable<IClientValidationAdapterProvider> customProviders)
    {
        _customProviders = customProviders;
    }

    public IClientValidationAdapter? GetAdapter(ValidationAttribute attribute)
    {
        // Built-in mapping (same order as MVC's ValidationAttributeAdapterProvider)
        var adapter = attribute switch
        {
            RequiredAttribute a => new RequiredClientAdapter(a),
            StringLengthAttribute a => new StringLengthClientAdapter(a),
            RangeAttribute a => new RangeClientAdapter(a),
            MinLengthAttribute a => new MinLengthClientAdapter(a),
            MaxLengthAttribute a => new MaxLengthClientAdapter(a),
            RegularExpressionAttribute a => new RegexClientAdapter(a),
            EmailAddressAttribute a => new DataTypeClientAdapter(a, "data-val-email"),
            UrlAttribute a => new DataTypeClientAdapter(a, "data-val-url"),
            CreditCardAttribute a => new DataTypeClientAdapter(a, "data-val-creditcard"),
            PhoneAttribute a => new DataTypeClientAdapter(a, "data-val-phone"),
            CompareAttribute a => new CompareClientAdapter(a),
            _ => null
        };

        if (adapter is not null)
        {
            return adapter;
        }

        // Try custom providers (registered via DI)
        foreach (var provider in _customProviders)
        {
            adapter = provider.GetAdapter(attribute);
            if (adapter is not null)
            {
                return adapter;
            }
        }

        return null;
    }
}
```

**Location:** `src/Components/Forms/src/ClientValidation/DefaultClientValidationAdapterProvider.cs`

### 5.7 `DefaultClientValidationService` — Service Implementation

```csharp
namespace Microsoft.AspNetCore.Components.Forms.ClientValidation;

internal sealed class DefaultClientValidationService : IClientValidationService
{
    private readonly IClientValidationAdapterProvider _adapterProvider;
    private readonly ValidationOptions? _validationOptions;
    private readonly IServiceProvider _serviceProvider;

    // Cache: (DeclaringType, FieldName) → dictionary of data-val-* attributes
    private readonly ConcurrentDictionary<(Type, string), IReadOnlyDictionary<string, string>> _cache = new();

    public DefaultClientValidationService(
        IClientValidationAdapterProvider adapterProvider,
        IServiceProvider serviceProvider,
        IOptions<ValidationOptions>? validationOptions = null)
    {
        _adapterProvider = adapterProvider;
        _serviceProvider = serviceProvider;
        _validationOptions = validationOptions?.Value;
    }

    public IReadOnlyDictionary<string, string> GetValidationAttributes(FieldIdentifier fieldIdentifier)
    {
        var key = (fieldIdentifier.Model.GetType(), fieldIdentifier.FieldName);
        return _cache.GetOrAdd(key, static (k, state) =>
            state.self.ComputeAttributes(k.Item1, k.Item2),
            (self: this, dummy: 0));
    }

    private IReadOnlyDictionary<string, string> ComputeAttributes(Type modelType, string fieldName)
    {
        var validationAttributes = GetValidationAttributesForProperty(modelType, fieldName);
        if (validationAttributes.Length == 0)
        {
            return ImmutableDictionary<string, string>.Empty;
        }

        var displayName = ResolveDisplayName(modelType, fieldName);
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var context = new ClientValidationContext
        {
            Attributes = attributes,
            DisplayName = displayName,
            DeclaringType = modelType,
            PropertyName = fieldName,
            Services = _serviceProvider
        };

        foreach (var validationAttribute in validationAttributes)
        {
            var adapter = _adapterProvider.GetAdapter(validationAttribute);
            if (adapter is not null)
            {
                context.ValidationAttribute = validationAttribute;
                adapter.AddClientValidation(context);
            }
        }

        return attributes;
    }

    /// <summary>
    /// Gets ValidationAttributes for a property, using the source generator path
    /// if available, falling back to reflection.
    /// </summary>
    private ValidationAttribute[] GetValidationAttributesForProperty(Type modelType, string fieldName)
    {
        // Path 1: Source generator (via ValidationOptions.Resolvers)
        if (_validationOptions is not null &&
            _validationOptions.TryGetValidatableTypeInfo(modelType, out var typeInfo) &&
            typeInfo is ValidatableTypeInfo validatableType)
        {
            var propInfo = validatableType.Members
                .OfType<ValidatablePropertyInfo>()
                .FirstOrDefault(p => p.Name == fieldName);

            if (propInfo is not null)
            {
                // GetValidationAttributes() is protected — need internal access
                // or a public method. See Section 6.3 for options.
                return propInfo.GetValidationAttributesPublic();
            }
        }

        // Path 2: Reflection fallback (same as DataAnnotationsValidator)
        return GetValidationAttributesByReflection(modelType, fieldName);
    }

    private static readonly ConcurrentDictionary<(Type, string), ValidationAttribute[]> _reflectionCache = new();

    private static ValidationAttribute[] GetValidationAttributesByReflection(Type modelType, string fieldName)
    {
        return _reflectionCache.GetOrAdd((modelType, fieldName), static key =>
        {
            var property = key.Item1.GetProperty(key.Item2);
            if (property is null)
            {
                return [];
            }

            return property.GetCustomAttributes<ValidationAttribute>(inherit: true).ToArray();
        });
    }

    private string ResolveDisplayName(Type modelType, string fieldName)
    {
        // Try ValidationOptions.DisplayNameProvider (localization-aware)
        if (_validationOptions?.DisplayNameProvider is { } displayNameProvider)
        {
            var context = new DisplayNameProviderContext
            {
                DeclaringType = modelType,
                Name = fieldName,
                Services = _serviceProvider
            };

            var localizedName = displayNameProvider(context);
            if (localizedName is not null)
            {
                return localizedName;
            }
        }

        // Fallback: Check DisplayAttribute on the property
        var property = modelType.GetProperty(fieldName);
        if (property is not null)
        {
            var displayAttr = property.GetCustomAttribute<DisplayAttribute>();
            if (displayAttr?.Name is not null)
            {
                return displayAttr.GetName() ?? fieldName;
            }

            var displayNameAttr = property.GetCustomAttribute<DisplayNameAttribute>();
            if (displayNameAttr is not null)
            {
                return displayNameAttr.DisplayName;
            }
        }

        return fieldName;
    }
}
```

**Location:** `src/Components/Forms/src/ClientValidation/DefaultClientValidationService.cs`

### 5.8 Error Message Resolution

Adapters resolve error messages using a shared helper that integrates with the localization infrastructure from [#65539](https://github.com/dotnet/aspnetcore/issues/65539):

```csharp
namespace Microsoft.AspNetCore.Components.Forms.ClientValidation;

internal static class ErrorMessageResolver
{
    /// <summary>
    /// Resolves the error message for a validation attribute, using the
    /// <see cref="ValidationOptions.ErrorMessageProvider"/> if configured,
    /// otherwise falling back to <see cref="ValidationAttribute.FormatErrorMessage"/>.
    /// </summary>
    public static string Resolve(
        ClientValidationContext context,
        ValidationAttribute attribute,
        params object[] formatArgs)
    {
        // Path 1: Use ErrorMessageProvider from ValidationOptions (localization-aware)
        var validationOptions = context.Services.GetService<IOptions<ValidationOptions>>()?.Value;

        if (validationOptions?.ErrorMessageProvider is { } errorMessageProvider)
        {
            var providerContext = new ErrorMessageProviderContext
            {
                Attribute = attribute,
                DisplayName = context.DisplayName,
                DeclaringType = context.DeclaringType,
                Services = context.Services,
            };

            var localizedMessage = errorMessageProvider(providerContext);
            if (localizedMessage is not null)
            {
                return localizedMessage;
            }
        }

        // Path 2: Default — use the attribute's own formatting
        return attribute.FormatErrorMessage(context.DisplayName);
    }
}
```

**Key insight from [#65539](https://github.com/dotnet/aspnetcore/issues/65539):** The localization proposal explicitly states that `ErrorMessageProviderContext` excludes validation-execution types so that "localized and formatted messages are retrievable *outside* of validation execution" — precisely our use case. The `ErrorMessageProvider` returns a **fully formatted** message (not a template), handling attribute-specific placeholders internally via `IValidationAttributeFormatter`.

### 5.9 `<ClientSideValidator />` Component

```csharp
namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Enables client-side validation for the enclosing <see cref="EditForm"/>.
/// Cascades an <see cref="IClientValidationService"/> that causes input
/// components to emit <c>data-val-*</c> HTML attributes, and optionally
/// renders the validation script tag.
/// </summary>
/// <example>
/// <code>
/// &lt;EditForm Model="Contact" FormName="contact" Enhance&gt;
///     &lt;ClientSideValidator /&gt;
///     &lt;InputText @bind-Value="Contact.Name" /&gt;
///     &lt;ValidationMessage For="() =&gt; Contact.Name" /&gt;
///     &lt;button type="submit"&gt;Submit&lt;/button&gt;
/// &lt;/EditForm&gt;
/// </code>
/// </example>
public sealed class ClientSideValidator : ComponentBase, IDisposable
{
    private IClientValidationService? _service;

    /// <summary>
    /// Gets or sets whether to automatically render a <c>&lt;script&gt;</c>
    /// tag referencing the validation library. Defaults to <see langword="true"/>.
    /// </summary>
    [Parameter]
    public bool IncludeScript { get; set; } = true;

    [CascadingParameter]
    private EditContext CurrentEditContext { get; set; } = default!;

    [Inject]
    private IClientValidationAdapterProvider AdapterProvider { get; set; } = default!;

    [Inject]
    private IServiceProvider ServiceProvider { get; set; } = default!;

    [Inject]
    private IOptions<ValidationOptions>? ValidationOptions { get; set; }

    protected override void OnInitialized()
    {
        if (CurrentEditContext is null)
        {
            throw new InvalidOperationException(
                $"{nameof(ClientSideValidator)} requires a cascading parameter of type " +
                $"{nameof(EditContext)}. Use it inside an {nameof(EditForm)}.");
        }

        _service = new DefaultClientValidationService(
            AdapterProvider,
            ServiceProvider,
            ValidationOptions);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // Cascade IClientValidationService to all descendants
        builder.OpenComponent<CascadingValue<IClientValidationService>>(0);
        builder.AddComponentParameter(1, "Value", _service);
        builder.AddComponentParameter(2, "IsFixed", true);
        builder.AddComponentParameter(3, "ChildContent",
            (RenderFragment)(childBuilder => { }));
        builder.CloseComponent();

        // Optionally emit the script tag
        if (IncludeScript)
        {
            builder.OpenElement(4, "script");
            builder.AddAttribute(5, "src", "_content/Microsoft.AspNetCore.Components.Web/aspnet-validation.js");
            builder.CloseElement();
        }
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
```

**Note on ChildContent:** The `<ClientSideValidator />` doesn't wrap child content — it only cascades the service and optionally emits a script tag. The cascading value is available to sibling components because `CascadingValue` in Blazor cascades to all descendants of the component's parent scope (the `EditForm`).

**Alternative implementation:** If the cascading pattern requires wrapping children, the component would need to accept `ChildContent` and render it inside the `CascadingValue`. However, Blazor's existing `DataAnnotationsValidator` doesn't wrap children and still participates in the EditForm's scope. The `ClientSideValidator` would follow the same pattern, but the `IClientValidationService` cascade needs to reach `InputBase` descendants. This may require the `EditForm` itself to participate — see Section 6.1.

**Location:** `src/Components/Web/src/Forms/ClientSideValidator.cs`

### 5.10 Changes to `InputBase<T>`

`InputBase<T>` needs to consume the cascaded `IClientValidationService` and add `data-val-*` attributes during rendering.

```csharp
// In InputBase<T>:

[CascadingParameter]
private IClientValidationService? ClientValidationService { get; set; }

/// <summary>
/// Returns the <c>data-val-*</c> HTML attributes for the current field,
/// or <see langword="null"/> if client-side validation is not enabled.
/// </summary>
protected IReadOnlyDictionary<string, string>? ClientValidationAttributes
{
    get
    {
        if (ClientValidationService is null)
        {
            return null;
        }

        return ClientValidationService.GetValidationAttributes(FieldIdentifier);
    }
}
```

**Rendering approach — two options:**

**Option A: Add in BuildRenderTree via override (each subclass):**
Each subclass (InputText, InputNumber, etc.) adds the attributes explicitly:

```csharp
// In InputText.BuildRenderTree:
protected override void BuildRenderTree(RenderTreeBuilder builder)
{
    builder.OpenElement(0, "input");
    builder.AddMultipleAttributes(1, AdditionalAttributes);
    // NEW: Add client validation attributes
    if (ClientValidationAttributes is { } valAttrs)
    {
        foreach (var kvp in valAttrs)
        {
            builder.AddAttribute(2, kvp.Key, kvp.Value);
        }
    }
    builder.AddAttributeIfNotNullOrEmpty(3, "id", IdAttributeValue);
    builder.AddAttributeIfNotNullOrEmpty(4, "name", NameAttributeValue);
    builder.AddAttributeIfNotNullOrEmpty(5, "class", CssClass);
    // ... rest unchanged
}
```

**Option B: Merge into AdditionalAttributes (in base class):**
Inject `data-val-*` attributes into `AdditionalAttributes` so subclasses don't need changes:

```csharp
// In InputBase<T>:
private IReadOnlyDictionary<string, object>? GetMergedAdditionalAttributes()
{
    var valAttrs = ClientValidationAttributes;
    if (valAttrs is null || valAttrs.Count == 0)
    {
        return AdditionalAttributes;
    }

    var merged = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    if (AdditionalAttributes is not null)
    {
        foreach (var kvp in AdditionalAttributes)
        {
            merged[kvp.Key] = kvp.Value;
        }
    }

    foreach (var kvp in valAttrs)
    {
        // Don't overwrite explicit attributes
        merged.TryAdd(kvp.Key, kvp.Value);
    }

    return merged;
}
```

**Recommendation: Option B** (merge into AdditionalAttributes). This requires zero changes to any subclass (`InputText`, `InputNumber`, `InputTextArea`, `InputSelect`, etc.) and works automatically for third-party components that inherit `InputBase<T>`. The `AdditionalAttributes` property getter (or a new `EffectiveAdditionalAttributes`) would return the merged dictionary.

### 5.11 Changes to `ValidationMessage<T>`

When `IClientValidationService` is cascaded, `ValidationMessage<T>` renders a container element with `data-valmsg-for` even when there are no server-side messages.

```csharp
// Modified BuildRenderTree in ValidationMessage<T>:
protected override void BuildRenderTree(RenderTreeBuilder builder)
{
    var hasClientValidation = ClientValidationService is not null;
    var messages = CurrentEditContext.GetValidationMessages(_fieldIdentifier);

    if (hasClientValidation)
    {
        // Always render container for JS library to find
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "data-valmsg-for", _fieldName);
        builder.AddAttribute(2, "class", "field-validation-valid");
        builder.AddMultipleAttributes(3, AdditionalAttributes);

        foreach (var message in messages)
        {
            builder.AddContent(4, message);
        }

        builder.CloseElement();
    }
    else
    {
        // Original behavior: render <div> per message
        foreach (var message in messages)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "validation-message");
            builder.AddMultipleAttributes(2, AdditionalAttributes);
            builder.AddContent(3, message);
            builder.CloseElement();
        }
    }
}
```

**Key changes when client validation is active:**
- Renders `<span>` (MVC convention) instead of multiple `<div>`s
- Adds `data-valmsg-for="fieldName"` for JS library discovery
- Always renders even if no messages (JS fills them in)
- `_fieldName` is computed from the `For` expression to match `InputBase.NameAttributeValue`

### 5.12 Changes to `ValidationSummary`

```csharp
// Modified BuildRenderTree in ValidationSummary:
protected override void BuildRenderTree(RenderTreeBuilder builder)
{
    var hasClientValidation = ClientValidationService is not null;

    if (hasClientValidation)
    {
        // Always render container for JS library
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "data-valmsg-summary", "true");
        builder.AddMultipleAttributes(2, AdditionalAttributes);

        builder.OpenElement(3, "ul");
        builder.AddAttribute(4, "class", "validation-errors");

        foreach (var error in GetValidationMessages())
        {
            builder.OpenElement(5, "li");
            builder.AddAttribute(6, "class", "validation-message");
            builder.AddContent(7, error);
            builder.CloseElement();
        }

        builder.CloseElement(); // ul
        builder.CloseElement(); // div
    }
    else
    {
        // Original behavior unchanged
        // ...
    }
}
```

---

## 6. Design Considerations and Open Questions

### 6.1 Cascading the Service

The current `DataAnnotationsValidator` sits inside `EditForm` but doesn't cascade anything to sibling components — it hooks into `EditContext` events. The `ClientSideValidator`, however, needs to cascade `IClientValidationService` to `InputBase<T>` descendants.

**Challenge:** A component can only cascade values to its *own* children, not to siblings. Since `ClientSideValidator` is a sibling of `InputText` etc. inside `EditForm`, a plain `CascadingValue` inside `ClientSideValidator` won't reach them.

**Solutions:**

1. **Store the service on `EditContext.Properties`:** `ClientSideValidator` stores the service in `EditContext.Properties[typeof(IClientValidationService)]`. `InputBase<T>` retrieves it from `EditContext.Properties` (it already has `EditContext` as a cascading parameter). No new cascading needed.

2. **Modify `EditForm` to cascade:** Add a cascading parameter slot on `EditForm` that `ClientSideValidator` sets via a callback. More invasive.

3. **Require `ClientSideValidator` to wrap form content:** Like `<ClientSideValidator><InputText .../></ClientSideValidator>`. Changes usage pattern.

**Recommendation: Option 1 (EditContext.Properties).** This is the least invasive approach, reuses the existing `EditContext` cascade, and follows the pattern already established by `DataAnnotationsValidator` (which also uses `EditContext` to communicate with the rest of the form). The `ClientSideValidator` stores the service, and `InputBase<T>` reads it:

```csharp
// In ClientSideValidator.OnInitialized():
CurrentEditContext.Properties[typeof(IClientValidationService)] = _service;

// In InputBase<T>:
private IClientValidationService? GetClientValidationService()
{
    if (EditContext?.Properties.TryGetValue(typeof(IClientValidationService), out var service) == true)
    {
        return service as IClientValidationService;
    }
    return null;
}
```

### 6.2 Name Attribute Matching

The `data-valmsg-for` value must exactly match the input's `name` attribute for the JS library to connect them. In Blazor SSR:

- `<InputText @bind-Value="Model.Name" />` renders `name="Model.Name"` (via `ExpressionFormatter.FormatLambda()`)
- `<ValidationMessage For="() => Model.Name" />` needs to compute the same expression path

Both `InputBase.NameAttributeValue` and `ValidationMessage.For` use `ValueExpression` / `For` lambda expressions processed through `ExpressionFormatter`. The names should match naturally since they reference the same model path. However, the `_fieldName` used for `data-valmsg-for` must use `ExpressionFormatter` (not `FieldIdentifier.FieldName` which is just `"Name"` without the model prefix).

### 6.3 Source Generator Access to ValidationAttributes

`ValidatablePropertyInfo.GetValidationAttributes()` is `protected`. To use it from `DefaultClientValidationService`:

**Options:**
1. Add a `public` method or property to `ValidatablePropertyInfo` (e.g., `public ValidationAttribute[] ValidationAttributes => GetValidationAttributes()`)
2. Use `InternalsVisibleTo` (Components.Forms → Validation)
3. Add an interface method to `IValidatableInfo`
4. Use reflection as fallback (already needed anyway)

**Recommendation:** Add a public property. The validation attributes are already discoverable via reflection, so exposing them through the source generator API is not a security concern — it's a convenience.

### 6.4 Caching Strategy

The `DefaultClientValidationService` caches per `(Type, FieldName)` pair, which means:
- First render computes and caches validation attributes
- Subsequent renders (including after enhanced navigation) hit the cache
- Cache is scoped to the service instance (per `ClientSideValidator` → per form)
- Different model instances of the same type share cached attribute metadata (correct, since attributes are on the type, not the instance)

### 6.5 Enhanced Navigation Compatibility

When enhanced navigation occurs:
1. The server re-renders the page with new HTML
2. `EditForm` and its children are re-rendered, including `ClientSideValidator`
3. New `data-val-*` attributes are emitted in the HTML
4. The JS library's `enhancedload` handler re-scans the DOM and picks up the new attributes
5. WeakMap state from old elements is auto-GC'd

No special C# code is needed for enhanced navigation — the re-render produces correct HTML, and the JS library handles the rest.

### 6.6 Relationship with DataAnnotationsValidator

`ClientSideValidator` and `DataAnnotationsValidator` serve complementary roles:

| Component | Purpose | When It Acts |
|---|---|---|
| `DataAnnotationsValidator` | Server-side validation on form submit | On `OnValidationRequested` / `OnFieldChanged` events |
| `ClientSideValidator` | Emit `data-val-*` for client-side validation | During render (attribute emission) |

Typical usage:
```razor
<EditForm Model="Contact" FormName="contact" Enhance>
    <DataAnnotationsValidator />
    <ClientSideValidator />
    <InputText @bind-Value="Contact.Name" />
    <ValidationMessage For="() => Contact.Name" />
    <button type="submit">Submit</button>
</EditForm>
```

Both can coexist. Server validation catches anything client validation misses (e.g., custom `IValidatableObject` logic, async checks).

### 6.7 MVC Compatibility Analysis

The design produces the same `data-val-*` protocol as MVC, so:
- The **same JS library** works for both MVC and Blazor forms
- MVC pages that already use jQuery Unobtrusive Validation can migrate to the new library
- Mixed apps (MVC + Blazor) can use a single validation script

The adapter interface names differ (`IClientValidationAdapter` vs `IClientModelValidator`) because:
- The Blazor adapters don't depend on `ModelMetadata`, `ActionContext`
- The context object is simpler and DI-friendly
- They represent a clean break from MVC's legacy abstractions

---

## 7. DI Registration

### 7.1 Extension Methods

```csharp
namespace Microsoft.Extensions.DependencyInjection;

public static class ClientValidationServiceCollectionExtensions
{
    /// <summary>
    /// Adds client-side validation services for Blazor forms.
    /// This registers the default adapter provider with built-in adapters
    /// for standard <see cref="ValidationAttribute"/> types.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.Services.AddClientSideValidation();
    /// </code>
    /// </example>
    public static IServiceCollection AddClientSideValidation(this IServiceCollection services)
    {
        services.TryAddSingleton<IClientValidationAdapterProvider,
            DefaultClientValidationAdapterProvider>();
        return services;
    }

    /// <summary>
    /// Registers a custom <see cref="IClientValidationAdapterProvider"/>
    /// that can supply adapters for custom validation attributes.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.Services.AddClientValidationAdapterProvider&lt;MyCustomAdapterProvider&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddClientValidationAdapterProvider<TProvider>(
        this IServiceCollection services)
        where TProvider : class, IClientValidationAdapterProvider
    {
        services.AddSingleton<IClientValidationAdapterProvider, TProvider>();
        return services;
    }
}
```

### 7.2 Custom Adapter Example

```csharp
// User's custom validation attribute
public class ClassicMovieAttribute : ValidationAttribute
{
    public int Year { get; set; }
    // ...
}

// User's custom adapter
public class ClassicMovieClientAdapter : IClientValidationAdapter
{
    private readonly ClassicMovieAttribute _attribute;

    public ClassicMovieClientAdapter(ClassicMovieAttribute attribute)
    {
        _attribute = attribute;
    }

    public void AddClientValidation(ClientValidationContext context)
    {
        context.MergeAttribute("data-val", "true");
        context.MergeAttribute("data-val-classicmovie", GetErrorMessage(context));
        context.MergeAttribute("data-val-classicmovie-year",
            _attribute.Year.ToString(CultureInfo.InvariantCulture));
    }

    public string GetErrorMessage(ClientValidationContext context)
        => ErrorMessageResolver.Resolve(context, _attribute, context.DisplayName, _attribute.Year);
}

// User's adapter provider
public class MyAdapterProvider : IClientValidationAdapterProvider
{
    public IClientValidationAdapter? GetAdapter(ValidationAttribute attribute)
    {
        if (attribute is ClassicMovieAttribute classicMovie)
        {
            return new ClassicMovieClientAdapter(classicMovie);
        }
        return null;
    }
}

// Registration in Program.cs
builder.Services.AddClientSideValidation();
builder.Services.AddClientValidationAdapterProvider<MyAdapterProvider>();
```

And on the JS side, register a matching provider:

```typescript
import { validationService } from './aspnet-validation';

validationService.engine.registerProvider({
    name: 'classicmovie',
    validate(el, directive) {
        const year = parseInt(directive.params.get('year') || '0', 10);
        // ... validation logic
        return null; // or error message
    }
});
```

---

## 8. Package / Assembly Layout

| Assembly | New Types | Notes |
|---|---|---|
| `Microsoft.AspNetCore.Components.Forms` | `IClientValidationService`, `IClientValidationAdapter`, `IClientValidationAdapterProvider`, `ClientValidationContext` | Core abstractions; no MVC dependency |
| `Microsoft.AspNetCore.Components.Forms` | `DefaultClientValidationService`, `DefaultClientValidationAdapterProvider`, built-in adapters, `ErrorMessageResolver` | Internal implementations |
| `Microsoft.AspNetCore.Components.Web` | `ClientSideValidator` | Component; references Forms |
| `Microsoft.AspNetCore.Components.Web` | Modified `InputBase<T>`, `InputText`, `ValidationMessage<T>`, `ValidationSummary` | Existing files, minimal changes |
| `Microsoft.Extensions.Validation` | (Possible) Public `ValidationAttributes` on `ValidatablePropertyInfo` | Enables source generator path |

---

## 9. Localization Integration

### 9.1 How It Works End-to-End

Given the localization proposal in [#65539](https://github.com/dotnet/aspnetcore/issues/65539):

1. Developer calls `builder.Services.AddValidation()` and `builder.Services.AddValidationLocalization()`
2. This sets `ValidationOptions.ErrorMessageProvider` and `ValidationOptions.DisplayNameProvider`
3. When `DefaultClientValidationService.ComputeAttributes()` runs during render:
   - It calls `DisplayNameProvider` to get the localized display name
   - Each adapter calls `ErrorMessageResolver.Resolve()` which calls `ErrorMessageProvider`
   - `ErrorMessageProvider` uses `IValidationAttributeFormatter` to format the template with attribute-specific args
   - The **fully formatted, localized message** is written into `data-val-*`
4. The JS library reads these pre-localized messages and displays them as-is

### 9.2 Without Localization

If `AddValidationLocalization()` is not called:
- `DisplayNameProvider` is null → falls back to `DisplayAttribute.GetName()` or property name
- `ErrorMessageProvider` is null → falls back to `ValidationAttribute.FormatErrorMessage(displayName)`
- Messages are in the default language (typically English built-in messages)

### 9.3 Culture Considerations

Since error messages are baked into the HTML at render time:
- Messages reflect `CultureInfo.CurrentUICulture` at the time of rendering
- If the user changes language, a page reload (or enhanced navigation) triggers re-render with new culture
- This matches MVC's behavior (MVC also bakes messages into HTML attributes at render time)

---

## 10. Testing Strategy

### 10.1 Unit Tests

- **Adapter tests:** Each built-in adapter emits the correct `data-val-*` attributes
- **Service tests:** `DefaultClientValidationService` discovers attributes via reflection and source generator
- **Display name tests:** Correct resolution from `DisplayAttribute`, `DisplayNameAttribute`, localization
- **Error message tests:** Correct resolution with and without localization configured
- **Custom adapter tests:** Custom providers are discovered and invoked
- **Cache tests:** Repeated calls return cached results

### 10.2 Component Tests (bUnit-style)

- **InputBase rendering:** `data-val-*` attributes appear in rendered HTML when `IClientValidationService` is cascaded
- **InputBase without service:** No `data-val-*` attributes (backwards-compatible)
- **ValidationMessage rendering:** `<span data-valmsg-for="...">` when service is present
- **ValidationMessage without service:** Original `<div>` behavior unchanged
- **ValidationSummary rendering:** `data-valmsg-summary="true"` when service is present
- **ClientSideValidator:** Cascades service, optionally emits script tag

### 10.3 Integration Tests

- **End-to-end SSR:** Render a Blazor SSR page with `<ClientSideValidator />`, verify HTML output contains all expected `data-val-*` attributes
- **Enhanced navigation:** Verify attributes persist after enhanced navigation
- **JS integration:** Full browser test (Playwright) verifying the JS library picks up attributes and validates

---

## 11. Implementation Order

### Phase 1: Core Infrastructure
1. `IClientValidationAdapter` interface
2. `IClientValidationAdapterProvider` interface
3. `ClientValidationContext` class
4. `IClientValidationService` interface
5. `ErrorMessageResolver` helper

### Phase 2: Built-In Adapters
6. `RequiredClientAdapter`
7. `StringLengthClientAdapter`
8. `MinLengthClientAdapter`, `MaxLengthClientAdapter`
9. `RangeClientAdapter`
10. `RegexClientAdapter`
11. `DataTypeClientAdapter` (email, url, creditcard, phone)
12. `CompareClientAdapter`
13. `DefaultClientValidationAdapterProvider`

### Phase 3: Service Implementation
14. `DefaultClientValidationService` (reflection path)
15. Source generator metadata access (if needed)
16. Caching layer

### Phase 4: Component Integration
17. `ClientSideValidator` component
18. `InputBase<T>` changes (consume service, merge attributes)
19. `ValidationMessage<T>` changes (`data-valmsg-for`)
20. `ValidationSummary` changes (`data-valmsg-summary`)

### Phase 5: Registration and Polish
21. `AddClientSideValidation()` extension method
22. Update BlazorSSR sample app (replace hardcoded `data-val-*` with `<ClientSideValidator />`)
23. Unit tests
24. Integration tests

---

## 12. Rendered HTML Example

Given this Razor markup:

```razor
<EditForm Model="Contact" FormName="contact" Enhance>
    <DataAnnotationsValidator />
    <ClientSideValidator />

    <InputText @bind-Value="Contact.Name" />
    <ValidationMessage For="() => Contact.Name" />

    <InputText @bind-Value="Contact.Email" />
    <ValidationMessage For="() => Contact.Email" />

    <button type="submit">Submit</button>
</EditForm>
```

With this model:

```csharp
public class ContactModel
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 2)]
    [Display(Name = "Full Name")]
    public string Name { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }
}
```

**Rendered HTML:**

```html
<form method="post" data-enhance>
    <script src="_content/Microsoft.AspNetCore.Components.Web/aspnet-validation.js"></script>

    <input id="Contact.Name"
           name="Contact.Name"
           class="valid"
           value=""
           data-val="true"
           data-val-required="Name is required"
           data-val-length="The field Full Name must be a string with a minimum length of 2 and a maximum length of 100."
           data-val-length-min="2"
           data-val-length-max="100" />

    <span data-valmsg-for="Contact.Name"
          class="field-validation-valid"></span>

    <input id="Contact.Email"
           name="Contact.Email"
           type="text"
           class="valid"
           value=""
           data-val="true"
           data-val-required="The Email field is required."
           data-val-email="The Email field is not a valid e-mail address." />

    <span data-valmsg-for="Contact.Email"
          class="field-validation-valid"></span>

    <button type="submit">Submit</button>
</form>
```

This HTML is identical in protocol to what MVC produces, and the JS validation library processes it the same way.

---

## 13. Design Decision Log

This section records the design choices that were considered during the creation of this document. For each decision, the considered variants are listed with brief context. The **selected** variant is marked.

### Decision 1: Integration Point for `data-val-*` Emission

**Context:** MVC uses tag helpers that call into `DefaultHtmlGenerator` during rendering. Blazor has no tag helpers — input components render via `BuildRenderTree`. The question is where the `data-val-*` attributes get injected into the render pipeline.

| # | Variant | Description |
|---|---------|-------------|
| 1 | **InputBase virtual method** | Add a virtual `AddValidationAttributes()` to `InputBase<T>` that runs during `BuildRenderTree`. Automatic for all built-in components; extensible via override. Third-party components inheriting InputBase get it for free. |
| 2 | **Cascaded service consumed by InputBase** ✅ | A service is made available to `InputBase<T>` (via `EditContext.Properties`). InputBase queries it during render for `data-val-*` attributes. Keeps InputBase simpler — it just consumes a dictionary. |
| 3 | **Standalone wrapper component** | A separate component wraps each input and emits the attributes. More decoupled but requires extra markup from the developer around every input. |

### Decision 2: Opt-In Mechanism

**Context:** Client-side validation should not change behavior for existing apps. Developers need an explicit way to enable it per form.

| # | Variant | Description |
|---|---------|-------------|
| 1 | **Separate component (`<ClientSideValidator />`)** ✅ | Placed inside `EditForm`, following the existing `<DataAnnotationsValidator />` pattern. Cascades the validation service. Keeps `EditForm` unchanged. |
| 2 | **Parameter on EditForm** (`ClientValidation="true"`) | Simpler developer UX but couples `EditForm` to client validation concerns and may require additional configuration parameters. |
| 3 | **Global DI registration only** | `services.AddClientSideValidation()` applies to all forms. Least ceremony but no per-form control. |

### Decision 3: ValidationMessage / ValidationSummary Changes

**Context:** The JS library needs placeholder elements with `data-valmsg-for` / `data-valmsg-summary` attributes to know where to display errors. Today, `ValidationMessage<T>` renders nothing when there are no server-side errors, and neither component emits `data-valmsg-*` attributes.

| # | Variant | Description |
|---|---------|-------------|
| 1 | **Modify when client validation is enabled** ✅ | When the cascaded service is present, `ValidationMessage` renders a `<span data-valmsg-for="...">` container (even if empty), and `ValidationSummary` adds `data-valmsg-summary="true"`. Without the service, behavior is completely unchanged. |
| 2 | **Always render placeholder container** | Always render a container element with `data-valmsg-for` regardless of whether client validation is enabled. Simpler logic but changes default HTML output for all users. |
| 3 | **Separate companion components** | Create new `ClientValidationMessage<T>` and `ClientValidationSummary` components. No changes to existing components but more API surface and potential developer confusion about which to use. |

### Decision 4: Custom Validator Extensibility

**Context:** Users with custom `ValidationAttribute` subclasses (e.g., `[ClassicMovie]`) need a way to emit corresponding `data-val-*` attributes. MVC uses `IClientModelValidator` (implemented on the attribute) or `IValidationAttributeAdapterProvider` (factory registered in DI).

| # | Variant | Description |
|---|---------|-------------|
| 1 | **Blazor-specific adapter interface with DI registration** ✅ | New `IClientValidationAdapter` and `IClientValidationAdapterProvider` interfaces. Users register custom providers via `services.AddClientValidationAdapterProvider<T>()`. Same spirit as MVC's adapter pattern but designed for Blazor's DI-first model, no MVC dependencies. |
| 2 | **Reuse MVC's `IClientModelValidator` interface** | The `ValidationAttribute` itself implements the interface. Familiar to MVC users but creates a coupling between validation attributes and the HTML protocol, and depends on MVC-specific types (`ClientModelValidationContext` with `ModelMetadata`). |
| 3 | **Attribute-based approach** | Decorate `ValidationAttribute` subclasses with `[ClientValidation("ruleName", ...)]` to declare the `data-val` mapping statically. Simple for common cases but limited for attributes with dynamic parameter values. |

### Decision 5: Validation Attribute Metadata Discovery

**Context:** To emit `data-val-*` attributes, the service needs to discover which `ValidationAttribute`s are on a model property. Two infrastructure paths exist: runtime reflection (`PropertyInfo.GetCustomAttributes`) and the new source generator (`ValidatableTypeInfo` / `ValidatablePropertyInfo` from `Microsoft.Extensions.Validation`).

| # | Variant | Description |
|---|---------|-------------|
| 1 | **Reflection with caching** | Use `PropertyInfo.GetCustomAttributes<ValidationAttribute>()` with a `ConcurrentDictionary` cache. Same approach as `DataAnnotationsValidator` and MVC. Simple, proven, works everywhere. |
| 2 | **Source generator infrastructure** | Use `ValidatableTypeInfo` / `ValidatablePropertyInfo` from `src/Validation/gen/`. More AOT-friendly, avoids runtime reflection, but the source generator is still maturing and `GetValidationAttributes()` is `protected` (needs API change). |
| 3 | **Support both: source generator preferred, reflection fallback** ✅ | Try the source generator path first (via `ValidationOptions.TryGetValidatableTypeInfo`); if not available, fall back to reflection with caching. Most flexible — AOT-friendly when source gen is present, universally compatible otherwise. |

### Decision 6: Error Message Localization Strategy

**Context:** `data-val-*` attributes contain pre-formatted error messages baked into the HTML at render time. These messages need to be localizable. A localization API is being designed separately in [#65539](https://github.com/dotnet/aspnetcore/issues/65539), with `ValidationOptions.ErrorMessageProvider` and `DisplayNameProvider` delegates explicitly designed to work outside of validation execution.

| # | Variant | Description |
|---|---------|-------------|
| 1 | **Use `ValidationOptions.ErrorMessageProvider` / `DisplayNameProvider`** ✅ | Adapters call `ErrorMessageProvider` (from [#65539](https://github.com/dotnet/aspnetcore/issues/65539)) to get fully formatted, localized messages. Falls back to `ValidationAttribute.FormatErrorMessage()` when no provider is configured. Designed exactly for this use case. |
| 2 | **Centralized localization in the service** | The service localizes all messages before passing them to adapters. Simpler for adapters but less flexible for custom message formatting. |
| 3 | **Localization delegate in the context** | The `ClientValidationContext` provides a `Func<string, string>` delegate. Lightweight but less discoverable and doesn't handle attribute-specific format arguments. |

### Decision 7: Validation Script Emission

**Context:** The JS validation library must be loaded on pages that use client-side validation. The question is whether this is the developer's responsibility or automated.

| # | Variant | Description |
|---|---------|-------------|
| 1 | **Separate script (developer adds manually)** | Developer adds `<script src="aspnet-validation.js">` to their layout, like `blazor.web.js`. Full control but easy to forget. |
| 2 | **`ClientSideValidator` emits script tag automatically** | The component renders a `<script>` tag in its `BuildRenderTree`. Zero-ceremony but less control. |
| 3 | **Both: auto-emit by default, opt-out available** ✅ | `<ClientSideValidator IncludeScript="true" />` (default). Developers can set `IncludeScript="false"` if they manage the script themselves or want to place it elsewhere. |
