// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms;

/* This is exactly equivalent to a .razor file containing:
 *
 *    @inherits InputBase<bool>
 *    <input type="checkbox" @bind="CurrentValue" id="@Id" class="@CssClass" />
 *
 * The only reason it's not implemented as a .razor file is that we don't presently have the ability to compile those
 * files within this project. Developers building their own input components should use Razor syntax.
 */

/// <summary>
/// An input component for editing <see cref="bool"/> values.
/// </summary>
public class InputCheckbox : InputBase<bool>
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
        builder.AddAttribute(2, "type", "checkbox");
        builder.AddAttributeIfNotNullOrEmpty(3, "name", NameAttributeValue);
        builder.AddAttribute(4, "class", CssClass);
        builder.AddAttribute(5, "checked", BindConverter.FormatValue(CurrentValue));
        // Include the "value" attribute so that when this is posted by a form, "true"
        // is included in the form fields. That's how <input type="checkbox"> works normally.
        // It sends the "on" value when the checkbox is checked, and nothing otherwise.
        builder.AddAttribute(6, "value", bool.TrueString);

        builder.AddAttribute(7, "onchange", EventCallback.Factory.CreateBinder<bool>(this, __value => CurrentValue = __value, CurrentValue));
        builder.SetUpdatesAttributeName("checked");
        builder.AddElementReferenceCapture(8, __inputReference => Element = __inputReference);
        builder.CloseElement();
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, out bool result, [NotNullWhen(false)] out string? validationErrorMessage)
        => throw new NotSupportedException($"This component does not parse string inputs. Bind to the '{nameof(CurrentValue)}' property, not '{nameof(CurrentValueAsString)}'.");
}
