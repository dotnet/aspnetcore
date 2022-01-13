// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// An input component for editing time values
/// Supported types are <see cref="TimeOnly"/> and <see cref="string"/>.
/// </summary>
public class InputTime<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue> : InputBase<TValue>
{
    /// <summary>
    /// Gets or sets the error message used when displaying an a parsing error.
    /// </summary>
    [Parameter] public string ParsingErrorMessage { get; set; } = "The {0} field must be time component.";

    /// <summary>
    /// Gets or sets the associated <see cref="ElementReference"/>.
    /// <para>
    /// May be <see langword="null"/> if accessed before the component is rendered.
    /// </para>
    /// </summary>
    [DisallowNull] public ElementReference? Element { get; protected set; }

    /// <summary>
    /// Constructs an instance of <see cref="InputTime{TValue}" />
    /// </summary>
    public InputTime()
    {
        var type = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
        if (type != typeof(TimeOnly) &&
            type != typeof(string))
        {
            throw new InvalidOperationException($"Unsupported {GetType()} type param '{type}'.");
        }
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "input");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "type", "time");
        builder.AddAttribute(3, "class", CssClass);
        builder.AddAttribute(4, "value", BindConverter.FormatValue(CurrentValueAsString));
        builder.AddAttribute(5, "onchange", EventCallback.Factory.CreateBinder<string?>(this, __value => CurrentValueAsString = __value, CurrentValueAsString));
        builder.AddElementReferenceCapture(6, __inputReference => Element = __inputReference);
        builder.CloseElement();
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        if (BindConverter.TryConvertTo(value, CultureInfo.InvariantCulture, out result))
        {
            Debug.Assert(result != null);
            validationErrorMessage = null;
            return true;
        }
        else
        {
            validationErrorMessage = string.Format(CultureInfo.InvariantCulture, ParsingErrorMessage, DisplayName ?? FieldIdentifier.FieldName);
            return false;
        }
    }
}
