// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// A dropdown selection component.
/// </summary>
public class InputSelect<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue> : InputBase<TValue>
{
    private readonly bool _isMultipleSelect;

    /// <summary>
    /// Constructs an instance of <see cref="InputSelect{TValue}"/>.
    /// </summary>
    public InputSelect()
    {
        _isMultipleSelect = typeof(TValue).IsArray;
    }

    /// <summary>
    /// Gets or sets the child content to be rendering inside the select element.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the <c>select</c> <see cref="ElementReference"/>.
    /// <para>
    /// May be <see langword="null"/> if accessed before the component is rendered.
    /// </para>
    /// </summary>
    [DisallowNull] public ElementReference? Element { get; protected set; }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "select");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttributeIfNotNullOrEmpty(2, "name", NameAttributeValue);
        builder.AddAttributeIfNotNullOrEmpty(3, "class", CssClass);
        builder.AddAttribute(4, "multiple", _isMultipleSelect);

        if (_isMultipleSelect)
        {
            builder.AddAttribute(5, "value", BindConverter.FormatValue(CurrentValue)?.ToString());
            builder.AddAttribute(6, "onchange", EventCallback.Factory.CreateBinder<string?[]?>(this, SetCurrentValueAsStringArray, default));
            builder.SetUpdatesAttributeName("value");
        }
        else
        {
            builder.AddAttribute(7, "value", CurrentValueAsString);
            builder.AddAttribute(8, "onchange", EventCallback.Factory.CreateBinder<string?>(this, __value => CurrentValueAsString = __value, default));
            builder.SetUpdatesAttributeName("value");
        }

        builder.AddElementReferenceCapture(9, __selectReference => Element = __selectReference);
        builder.AddContent(10, ChildContent);
        builder.CloseElement();
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? validationErrorMessage)
        => this.TryParseSelectableValueFromString(value, out result, out validationErrorMessage);

    /// <inheritdoc />
    protected override string? FormatValueAsString(TValue? value)
    {
        // We special-case bool values because BindConverter reserves bool conversion for conditional attributes.
        if (typeof(TValue) == typeof(bool))
        {
            return (bool)(object)value! ? "true" : "false";
        }
        else if (typeof(TValue) == typeof(bool?))
        {
            return value is not null && (bool)(object)value ? "true" : "false";
        }

        return base.FormatValueAsString(value);
    }

    private void SetCurrentValueAsStringArray(string?[]? value)
    {
        CurrentValue = BindConverter.TryConvertTo<TValue>(value, CultureInfo.CurrentCulture, out var result)
            ? result
            : default;
    }
}
