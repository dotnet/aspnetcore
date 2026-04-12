# Blazor Client-Side Validation: Final Implementation Plan

This document contains the complete, ready-to-implement plan with full code listings for every changed file.

## Architecture

```
AddRazorComponents()
  → registers IClientValidationService as singleton

DataAnnotationsValidator.OnInitialized()
  → injects IClientValidationService
  → if EnableClientValidation && AssignedRenderMode is null (SSR)
  → stores service in EditContext.Properties[typeof(IClientValidationService)]

InputBase<T>.SetParametersAsync()
  → after UpdateAdditionalValidationAttributes()
  → calls MergeClientValidationAttributes()
  → reads service from EditContext.Properties
  → calls service.GetValidationAttributes(FieldIdentifier)
  → merges data-val-* into AdditionalAttributes

ValidationMessage<T>.BuildRenderTree()
  → checks EditContext.Properties for IClientValidationService
  → if present: <span data-valmsg-for="..." data-valmsg-replace="true" class="field-validation-valid">
  → if absent: existing <div class="validation-message"> per message

ValidationSummary.BuildRenderTree()
  → checks EditContext.Properties for IClientValidationService
  → if present: <div data-valmsg-summary="true" class="validation-summary-valid"><ul>...</ul></div>
  → if absent: existing <ul class="validation-errors"> only when messages exist
```

## Step 1: New file — IClientValidationService.cs

**Location:** `src/Components/Web/src/Forms/IClientValidationService.cs`

```csharp
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Provides client-side validation HTML attributes (<c>data-val-*</c>) for form fields
/// based on their <see cref="System.ComponentModel.DataAnnotations.ValidationAttribute"/>s.
/// </summary>
public interface IClientValidationService
{
    /// <summary>
    /// Gets the <c>data-val-*</c> HTML attributes for a form field.
    /// Returns <see langword="null"/> if no validation attributes apply.
    /// </summary>
    IReadOnlyDictionary<string, object>? GetValidationAttributes(FieldIdentifier fieldIdentifier);
}
```

## Step 2: New file — IClientValidationAdapter.cs

**Location:** `src/Components/Web/src/Forms/IClientValidationAdapter.cs`

```csharp
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Implemented by <see cref="System.ComponentModel.DataAnnotations.ValidationAttribute"/> subclasses
/// that support client-side validation by emitting <c>data-val-*</c> HTML attributes.
/// </summary>
public interface IClientValidationAdapter
{
    /// <summary>
    /// Adds client-side validation attributes to the rendering context.
    /// </summary>
    void AddClientValidation(in ClientValidationContext context);
}

/// <summary>
/// Context for adding client-side validation HTML attributes.
/// </summary>
public readonly ref struct ClientValidationContext
{
    private readonly Dictionary<string, object> _attributes;

    internal ClientValidationContext(Dictionary<string, object> attributes, string errorMessage)
    {
        _attributes = attributes;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// The formatted error message for the validation attribute.
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// Adds an HTML attribute. Uses first-wins semantics: if the key already exists, the call is ignored.
    /// </summary>
    public void MergeAttribute(string key, string value)
    {
        _attributes.TryAdd(key, value);
    }
}
```

## Step 3: New file — DefaultClientValidationService.cs

**Location:** `src/Components/Forms/src/DefaultClientValidationService.cs`

```csharp
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace Microsoft.AspNetCore.Components.Forms;

internal sealed class DefaultClientValidationService : IClientValidationService
{
    private readonly ConcurrentDictionary<(Type ModelType, string FieldName), IReadOnlyDictionary<string, object>?> _cache = new();

    public IReadOnlyDictionary<string, object>? GetValidationAttributes(FieldIdentifier fieldIdentifier)
    {
        return _cache.GetOrAdd(
            (fieldIdentifier.Model.GetType(), fieldIdentifier.FieldName),
            static key => ComputeAttributes(key.ModelType, key.FieldName));
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070",
        Justification = "Model types are application code and are preserved by default.")]
    private static IReadOnlyDictionary<string, object>? ComputeAttributes(Type modelType, string fieldName)
    {
        var property = modelType.GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);
        if (property is null)
        {
            return null;
        }

        var validationAttributes = property.GetCustomAttributes<ValidationAttribute>(inherit: true);
        var attrs = new Dictionary<string, object>();
        var displayName = GetDisplayName(property) ?? fieldName;

        foreach (var attr in validationAttributes)
        {
            var errorMessage = attr.FormatErrorMessage(displayName);
            AddAttributesForValidationAttribute(attr, property, errorMessage, attrs);
        }

        if (attrs.Count == 0)
        {
            return null;
        }

        attrs.TryAdd("data-val", "true");
        return attrs;
    }

    private static void AddAttributesForValidationAttribute(
        ValidationAttribute attr,
        PropertyInfo property,
        string errorMessage,
        Dictionary<string, object> attrs)
    {
        switch (attr)
        {
            case RequiredAttribute:
                attrs.TryAdd("data-val-required", errorMessage);
                break;

            case StringLengthAttribute sla:
                attrs.TryAdd("data-val-length", errorMessage);
                if (sla.MaximumLength != int.MaxValue)
                {
                    attrs.TryAdd("data-val-length-max", sla.MaximumLength.ToString(CultureInfo.InvariantCulture));
                }
                if (sla.MinimumLength != 0)
                {
                    attrs.TryAdd("data-val-length-min", sla.MinimumLength.ToString(CultureInfo.InvariantCulture));
                }
                break;

            case MaxLengthAttribute mla:
                attrs.TryAdd("data-val-maxlength", errorMessage);
                attrs.TryAdd("data-val-maxlength-max", mla.Length.ToString(CultureInfo.InvariantCulture));
                break;

            case MinLengthAttribute mla:
                attrs.TryAdd("data-val-minlength", errorMessage);
                attrs.TryAdd("data-val-minlength-min", mla.Length.ToString(CultureInfo.InvariantCulture));
                break;

            case RangeAttribute ra:
                ra.IsValid(3); // Trigger internal conversion of Minimum/Maximum
                attrs.TryAdd("data-val-range", errorMessage);
                attrs.TryAdd("data-val-range-min", Convert.ToString(ra.Minimum, CultureInfo.InvariantCulture)!);
                attrs.TryAdd("data-val-range-max", Convert.ToString(ra.Maximum, CultureInfo.InvariantCulture)!);
                break;

            case RegularExpressionAttribute rea:
                attrs.TryAdd("data-val-regex", errorMessage);
                attrs.TryAdd("data-val-regex-pattern", rea.Pattern);
                break;

            case CompareAttribute ca:
                attrs.TryAdd("data-val-equalto", errorMessage);
                attrs.TryAdd("data-val-equalto-other", "*." + ca.OtherProperty);
                break;

            case EmailAddressAttribute:
                attrs.TryAdd("data-val-email", errorMessage);
                break;

            case UrlAttribute:
                attrs.TryAdd("data-val-url", errorMessage);
                break;

            case PhoneAttribute:
                attrs.TryAdd("data-val-phone", errorMessage);
                break;

            case CreditCardAttribute:
                attrs.TryAdd("data-val-creditcard", errorMessage);
                break;

            case FileExtensionsAttribute fea:
                attrs.TryAdd("data-val-fileextensions", errorMessage);
                var normalizedExtensions = fea.Extensions
                    .Replace(" ", string.Empty)
                    .Replace(".", string.Empty)
                    .ToLowerInvariant();
                var parsedExtensions = normalizedExtensions
                    .Split(',')
                    .Select(e => "." + e);
                attrs.TryAdd("data-val-fileextensions-extensions", string.Join(",", parsedExtensions));
                break;

            default:
                // Check for custom adapter on the attribute
                if (attr is IClientValidationAdapter adapter)
                {
                    var context = new ClientValidationContext(attrs, errorMessage);
                    adapter.AddClientValidation(in context);
                }
                break;
        }
    }

    private static string? GetDisplayName(PropertyInfo property)
    {
        var displayAttr = property.GetCustomAttribute<System.ComponentModel.DataAnnotations.DisplayAttribute>();
        if (displayAttr is not null)
        {
            return displayAttr.GetName();
        }

        var displayNameAttr = property.GetCustomAttribute<System.ComponentModel.DisplayNameAttribute>();
        if (displayNameAttr is not null)
        {
            return displayNameAttr.DisplayName;
        }

        return null;
    }
}
```

## Step 4: Modify — DataAnnotationsValidator.cs

**Location:** `src/Components/Forms/src/DataAnnotationsValidator.cs`

Full file:

```csharp
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Adds Data Annotations validation support to an <see cref="EditContext"/>.
/// When rendering in a static SSR context, also activates client-side validation
/// by storing an <see cref="IClientValidationService"/> on the <see cref="EditContext.Properties"/>.
/// </summary>
public class DataAnnotationsValidator : ComponentBase, IDisposable
{
    private IDisposable? _subscriptions;
    private EditContext? _originalEditContext;

    [CascadingParameter] EditContext? CurrentEditContext { get; set; }

    [Inject] private IServiceProvider ServiceProvider { get; set; } = default!;

    [Inject] private IClientValidationService? ClientValidationService { get; set; }

    /// <summary>
    /// Gets or sets whether client-side validation attributes (<c>data-val-*</c>) should be emitted
    /// on input components within this form. Default is <see langword="true"/>.
    /// Set to <see langword="false"/> to disable client-side validation while keeping
    /// server-side DataAnnotations validation active.
    /// </summary>
    [Parameter] public bool EnableClientValidation { get; set; } = true;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (CurrentEditContext is null)
        {
            throw new InvalidOperationException($"{nameof(DataAnnotationsValidator)} requires a cascading " +
                $"parameter of type {nameof(EditContext)}. For example, you can use {nameof(DataAnnotationsValidator)} " +
                $"inside an EditForm.");
        }

        _subscriptions = CurrentEditContext.EnableDataAnnotationsValidation(ServiceProvider);
        _originalEditContext = CurrentEditContext;

        // Enable client-side validation only in static SSR context.
        // AssignedRenderMode is null when rendering statically (no interactive mode assigned).
        if (EnableClientValidation && ClientValidationService is not null && AssignedRenderMode is null)
        {
            CurrentEditContext.Properties[typeof(IClientValidationService)] = ClientValidationService;
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (CurrentEditContext != _originalEditContext)
        {
            throw new InvalidOperationException($"{GetType()} does not support changing the " +
                $"{nameof(EditContext)} dynamically.");
        }
    }

    /// <inheritdoc/>
    protected virtual void Dispose(bool disposing)
    {
    }

    void IDisposable.Dispose()
    {
        _subscriptions?.Dispose();
        _subscriptions = null;

        // Clean up the client validation service reference from EditContext
        CurrentEditContext?.Properties.Remove(typeof(IClientValidationService));

        Dispose(disposing: true);
    }
}
```

**PublicAPI.Unshipped.txt** additions for `Microsoft.AspNetCore.Components.Forms`:

```
Microsoft.AspNetCore.Components.Forms.DataAnnotationsValidator.EnableClientValidation.get -> bool
Microsoft.AspNetCore.Components.Forms.DataAnnotationsValidator.EnableClientValidation.set -> void
```

## Step 5: Modify — InputBase.cs

Add after `UpdateAdditionalValidationAttributes()` call (line 291):

```csharp
    UpdateAdditionalValidationAttributes();
    MergeClientValidationAttributes();

    // For derived components, retain the usual lifecycle with OnInit/OnParametersSet/etc.
    return base.SetParametersAsync(ParameterView.Empty);
```

Add new private method:

```csharp
private void MergeClientValidationAttributes()
{
    if (EditContext?.Properties.TryGetValue(typeof(IClientValidationService), out var serviceObj) != true
        || serviceObj is not IClientValidationService service)
    {
        return;
    }

    var validationAttributes = service.GetValidationAttributes(FieldIdentifier);
    if (validationAttributes is null)
    {
        return;
    }

    if (ConvertToDictionary(AdditionalAttributes, out var additionalAttributes))
    {
        AdditionalAttributes = additionalAttributes;
    }

    foreach (var (key, value) in validationAttributes)
    {
        additionalAttributes.TryAdd(key, value);
    }
}
```

Note: `MergeClientValidationAttributes()` only runs once per `SetParametersAsync` call. The cached
result from `DefaultClientValidationService` means reflection runs only once per model property.

**PublicAPI.Unshipped.txt** — no additions needed (method is private).

## Step 6: Modify — ValidationMessage.cs

Replace `BuildRenderTree`:

```csharp
/// <inheritdoc />
protected override void BuildRenderTree(RenderTreeBuilder builder)
{
    var hasClientValidation = CurrentEditContext.Properties.TryGetValue(
        typeof(IClientValidationService), out _);

    if (hasClientValidation)
    {
        // Render a single element with data-valmsg-for for the JS validation library.
        var fieldName = ExpressionFormatter.FormatLambda(For!);

        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "data-valmsg-for", fieldName);
        builder.AddAttribute(2, "data-valmsg-replace", "true");
        builder.AddAttribute(3, "class", "field-validation-valid");
        builder.AddMultipleAttributes(4, AdditionalAttributes);

        // Render server-side validation messages as initial content (e.g. after POST)
        foreach (var message in CurrentEditContext.GetValidationMessages(_fieldIdentifier))
        {
            builder.AddContent(5, message);
        }

        builder.CloseElement();
    }
    else
    {
        foreach (var message in CurrentEditContext.GetValidationMessages(_fieldIdentifier))
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

Add using at top of file:

```csharp
using Microsoft.AspNetCore.Components.Forms.Mapping;
```

Note: `ExpressionFormatter` is in the `Shared` project and is already available via project references.
The field name produced by `ExpressionFormatter.FormatLambda(For!)` matches the `name` attribute
generated by `InputBase.NameAttributeValue` because both use the same formatter. When `HtmlFieldPrefix`
is involved (nested forms), `ValidationMessage` would need the prefix too — but for the initial 
implementation, the `FormatLambda` output is correct for flat models.

## Step 7: Modify — ValidationSummary.cs

Replace `BuildRenderTree`:

```csharp
/// <inheritdoc />
protected override void BuildRenderTree(RenderTreeBuilder builder)
{
    var validationMessages = Model is null ?
        CurrentEditContext.GetValidationMessages() :
        CurrentEditContext.GetValidationMessages(new FieldIdentifier(Model, string.Empty));

    var hasClientValidation = CurrentEditContext.Properties.TryGetValue(
        typeof(IClientValidationService), out _);

    if (hasClientValidation)
    {
        // Always render the container for the JS validation library to find.
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "data-valmsg-summary", "true");
        builder.AddAttribute(2, "class", "validation-summary-valid");
        builder.AddMultipleAttributes(3, AdditionalAttributes);

        builder.OpenElement(4, "ul");

        foreach (var error in validationMessages)
        {
            builder.OpenElement(5, "li");
            builder.AddContent(6, error);
            builder.CloseElement();
        }

        builder.CloseElement(); // ul
        builder.CloseElement(); // div
    }
    else
    {
        var first = true;
        foreach (var error in validationMessages)
        {
            if (first)
            {
                first = false;
                builder.OpenElement(0, "ul");
                builder.AddAttribute(1, "class", "validation-errors");
                builder.AddMultipleAttributes(2, AdditionalAttributes);
            }

            builder.OpenElement(3, "li");
            builder.AddAttribute(4, "class", "validation-message");
            builder.AddContent(5, error);
            builder.CloseElement();
        }

        if (!first)
        {
            builder.CloseElement();
        }
    }
}
```

## Step 8: Modify — RazorComponentsServiceCollectionExtensions.cs

Add after the form handling services block (around line 90):

```csharp
// Client-side validation
services.TryAddSingleton<IClientValidationService, DefaultClientValidationService>();
```

Requires adding to the usings:

```csharp
using Microsoft.AspNetCore.Components.Forms;
```

**PublicAPI.Unshipped.txt** additions for the service interface and adapter (in `Microsoft.AspNetCore.Components.Web`):

```
Microsoft.AspNetCore.Components.Forms.IClientValidationService
Microsoft.AspNetCore.Components.Forms.IClientValidationService.GetValidationAttributes(Microsoft.AspNetCore.Components.Forms.FieldIdentifier fieldIdentifier) -> System.Collections.Generic.IReadOnlyDictionary<string!, object!>?
Microsoft.AspNetCore.Components.Forms.IClientValidationAdapter
Microsoft.AspNetCore.Components.Forms.IClientValidationAdapter.AddClientValidation(in Microsoft.AspNetCore.Components.Forms.ClientValidationContext context) -> void
Microsoft.AspNetCore.Components.Forms.ClientValidationContext
Microsoft.AspNetCore.Components.Forms.ClientValidationContext.ErrorMessage.get -> string!
Microsoft.AspNetCore.Components.Forms.ClientValidationContext.MergeAttribute(string! key, string! value) -> void
```

## File Summary

| # | File | Action | Project |
|---|------|--------|---------|
| 1 | `IClientValidationService.cs` | New | `Microsoft.AspNetCore.Components.Web` |
| 2 | `IClientValidationAdapter.cs` + `ClientValidationContext` | New | `Microsoft.AspNetCore.Components.Web` |
| 3 | `DefaultClientValidationService.cs` | New | `Microsoft.AspNetCore.Components.Forms` |
| 4 | `DataAnnotationsValidator.cs` | Modify | `Microsoft.AspNetCore.Components.Forms` |
| 5 | `InputBase.cs` | Modify (add `MergeClientValidationAttributes`) | `Microsoft.AspNetCore.Components.Web` |
| 6 | `ValidationMessage.cs` | Modify (conditional render) | `Microsoft.AspNetCore.Components.Web` |
| 7 | `ValidationSummary.cs` | Modify (conditional render) | `Microsoft.AspNetCore.Components.Web` |
| 8 | `RazorComponentsServiceCollectionExtensions.cs` | Modify (register service) | `Microsoft.AspNetCore.Components.Endpoints` |
| 9 | `PublicAPI.Unshipped.txt` (×3) | Modify | Forms, Web, Endpoints |

## Testing

### E2E Tests (already created)

`ClientValidationAttributeTest.cs` — 17 xUnit + Selenium tests:
- 10 tests for data-val-* attribute generation per DataAnnotation type
- 1 test for ValidationMessage rendering with data-valmsg-for
- 1 test for ValidationSummary rendering with data-valmsg-summary  
- 2 tests for opt-out (EnableClientValidation="false")
- 3 tests for interactive mode exclusion

### Unit Tests (to add)

`DefaultClientValidationServiceTest.cs`:
- Each of the 12 DataAnnotation types produces correct data-val-* attributes
- Error messages use FormatErrorMessage with display name
- [Display(Name="...")] is used when present
- Multiple attributes on same property all produce output
- Unknown attributes are skipped
- Custom IClientValidationAdapter attribute produces custom attributes
- Caching: same (type, field) returns same dictionary instance
- Null/missing property returns null
- data-val="true" is set when any validation attribute exists
