// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms;

/* This is almost equivalent to a .razor file containing:
 *
 *    @inherits InputBase<TValue>
 *    <input type="hidden" @bind="CurrentValue" id="@Id" class="@CssClass" />
 *
 * The only reason it's not implemented as a .razor file is that we don't presently have the ability to compile those
 * files within this project. Developers building their own input components should use Razor syntax.
 */

/// <summary>
/// An input component for editing <see cref="TValue"/> values.
/// </summary>
public class InputHidden<TValue> : InputBase<TValue>
{
    /// <summary>
    /// Gets or sets the associated <see cref="ElementReference"/>.
    /// <para>
    /// May be <see langword="null"/> if accessed before the component is rendered.
    /// </para>
    /// </summary>
    [DisallowNull] public ElementReference? Element { get; protected set; }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "input");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "type", "hidden");
        builder.AddAttributeIfNotNullOrEmpty(3, "name", NameAttributeValue);
        builder.AddAttributeIfNotNullOrEmpty(4, "class", CssClass);
        builder.AddAttribute(5, "value", BindConverter.FormatValue(CurrentValue));
        builder.AddAttribute(6, "onchange", EventCallback.Factory.CreateBinder<TValue>(this, __value => CurrentValue = __value, CurrentValue));
        builder.SetUpdatesAttributeName("value");
        builder.AddElementReferenceCapture(7, __inputReference => Element = __inputReference);
        builder.CloseElement();
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, out TValue? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        if (BindConverter.TryConvertTo<TValue>(value, CultureInfo.CurrentCulture, out var parsedValue))
        {
            result = parsedValue;
            validationErrorMessage = null;
            return true;
        }
        else
        {
            result = default;
            validationErrorMessage = $"{FieldIdentifier.FieldName} field is not valid.";
            return false;
        }
    }
}