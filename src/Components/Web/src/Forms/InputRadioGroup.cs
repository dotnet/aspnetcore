// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Groups child <see cref="InputRadio{TValue}"/> components.
/// </summary>
public class InputRadioGroup<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue> : InputBase<TValue>, IInputRadioValueProvider
{
    private readonly string _defaultGroupName = Guid.NewGuid().ToString("N");
    private InputRadioContext? _context;

    /// <summary>
    /// Gets or sets the child content to be rendering inside the <see cref="InputRadioGroup{TValue}"/>.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the name of the group.
    /// </summary>
    [Parameter] public string? Name { get; set; }

    [CascadingParameter] private InputRadioContext? CascadedContext { get; set; }

    object? IInputRadioValueProvider.CurrentValue => CurrentValue;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        // On the first render, we can instantiate the InputRadioContext
        if (_context is null)
        {
            var changeEventCallback = EventCallback.Factory.CreateBinder<string?>(this, __value => CurrentValueAsString = __value, CurrentValueAsString);
            _context = new InputRadioContext(this, CascadedContext, changeEventCallback);
        }
        else if (_context.ParentContext != CascadedContext)
        {
            // This should never be possible in any known usage pattern, but if it happens, we want to know
            throw new InvalidOperationException("An InputRadioGroup cannot change context after creation");
        }

        // Mutate the InputRadioContext instance in place. Since this is a non-fixed cascading parameter, the descendant
        // InputRadio/InputRadioGroup components will get notified to re-render and will see the new values.
        if (!string.IsNullOrEmpty(Name))
        {
            // Prefer the explicitly-specified group name over anything else.
            _context.GroupName = Name;
        }
        else if (!string.IsNullOrEmpty(NameAttributeValue))
        {
            // If the user specifies a "name" attribute, or we're using "name" as a form field identifier, use that.
            _context.GroupName = NameAttributeValue;
        }
        else
        {
            // Otherwise, just use a GUID to disambiguate this group's radio inputs from any others on the page.
            _context.GroupName = _defaultGroupName;
        }

        _context.FieldClass = EditContext?.FieldCssClass(FieldIdentifier);

        // Pass client validation attributes to child InputRadio components via shared context.
        // Unlike MVC (which uses FormContext to emit data-val-* on only the first radio button),
        // Blazor's component model renders children independently. We achieve the same first-only
        // behavior by mutating the shared context: the first InputRadio reads the attributes,
        // renders them, and clears the property so subsequent radios in the group get nothing.
        _context.ClientValidationAttributes = ExtractClientValidationAttributes();
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        Debug.Assert(_context != null);

        // Note that we must not set IsFixed=true on the CascadingValue, because the mutations to _context
        // are what cause the descendant InputRadio components to re-render themselves
        builder.OpenComponent<CascadingValue<InputRadioContext>>(0);
        builder.AddComponentParameter(2, "Value", _context);
        builder.AddComponentParameter(3, "ChildContent", ChildContent);
        builder.CloseComponent();
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? validationErrorMessage)
        => this.TryParseSelectableValueFromString(value, out result, out validationErrorMessage);

    /// <summary>
    /// Extracts data-val-* client validation attributes from AdditionalAttributes so they
    /// can be passed to child InputRadio components via InputRadioContext. InputRadioGroup
    /// itself doesn't render an HTML element, so it can't carry these attributes directly.
    /// </summary>
    private IReadOnlyDictionary<string, object>? ExtractClientValidationAttributes()
    {
        if (AdditionalAttributes is null)
        {
            return null;
        }

        Dictionary<string, object>? result = null;
        foreach (var (key, value) in AdditionalAttributes)
        {
            // Match "data-val" exactly and "data-val-*" (with dash), but not "data-value" or other unrelated attributes.
            if (string.Equals(key, "data-val", StringComparison.OrdinalIgnoreCase)
                || key.StartsWith("data-val-", StringComparison.OrdinalIgnoreCase))
            {
                result ??= new();
                result[key] = value;
            }
        }

        return result;
    }
}
