// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// An hidden input component for storing <see cref="string"/> values.
/// </summary>
public class InputHidden : InputBase<string?>
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
        builder.AddAttribute(1, "type", "hidden");
        builder.AddMultipleAttributes(2, AdditionalAttributes);
        builder.AddAttributeIfNotNullOrEmpty(3, "id", IdAttributeValue);
        builder.AddAttributeIfNotNullOrEmpty(4, "name", NameAttributeValue);
        builder.AddAttributeIfNotNullOrEmpty(5, "class", CssClass);
        builder.AddAttribute(6, "value", CurrentValueAsString);
        builder.AddAttribute(7, "onchange", EventCallback.Factory.CreateBinder<string?>(this, __value => CurrentValueAsString = __value, CurrentValueAsString));
        builder.SetUpdatesAttributeName("value");
        builder.AddElementReferenceCapture(8, __inputReference => Element = __inputReference);
        builder.CloseElement();
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, out string? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = value;
        validationErrorMessage = null;
        return true;
    }
}
