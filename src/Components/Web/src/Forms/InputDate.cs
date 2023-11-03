// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// An input component for editing date values.
/// The supported types for the date value are:
/// <list type="bullet">
/// <item><see cref="DateTime"/></item>
/// <item><see cref="DateTimeOffset"/></item>
/// <item><see cref="DateOnly"/></item>
/// <item><see cref="TimeOnly"/></item>
/// </list>
/// </summary>
public class InputDate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue> : InputBase<TValue>
{
    private const string DateFormat = "yyyy-MM-dd";                     // Compatible with HTML 'date' inputs
    private const string DateTimeLocalFormat = "yyyy-MM-ddTHH:mm:ss";   // Compatible with HTML 'datetime-local' inputs
    private const string MonthFormat = "yyyy-MM";                       // Compatible with HTML 'month' inputs
    private const string TimeFormat = "HH:mm:ss";                       // Compatible with HTML 'time' inputs

    private string _typeAttributeValue = default!;
    private string _format = default!;
    private string _parsingErrorMessage = default!;

    /// <summary>
    /// Gets or sets the type of HTML input to be rendered.
    /// </summary>
    [Parameter] public InputDateType Type { get; set; } = InputDateType.Date;

    /// <summary>
    /// Gets or sets the error message used when displaying an a parsing error.
    /// </summary>
    [Parameter] public string ParsingErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the associated <see cref="ElementReference"/>.
    /// <para>
    /// May be <see langword="null"/> if accessed before the component is rendered.
    /// </para>
    /// </summary>
    [DisallowNull] public ElementReference? Element { get; protected set; }

    /// <summary>
    /// Constructs an instance of <see cref="InputDate{TValue}"/>
    /// </summary>
    public InputDate()
    {
        var type = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);

        if (type != typeof(DateTime) &&
            type != typeof(DateTimeOffset) &&
            type != typeof(DateOnly) &&
            type != typeof(TimeOnly))
        {
            throw new InvalidOperationException($"Unsupported {GetType()} type param '{type}'.");
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        (_typeAttributeValue, _format, var formatDescription) = Type switch
        {
            InputDateType.Date => ("date", DateFormat, "date"),
            InputDateType.DateTimeLocal => ("datetime-local", DateTimeLocalFormat, "date and time"),
            InputDateType.Month => ("month", MonthFormat, "year and month"),
            InputDateType.Time => ("time", TimeFormat, "time"),
            _ => throw new InvalidOperationException($"Unsupported {nameof(InputDateType)} '{Type}'.")
        };

        _parsingErrorMessage = string.IsNullOrEmpty(ParsingErrorMessage)
            ? $"The {{0}} field must be a {formatDescription}."
            : ParsingErrorMessage;
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "input");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "type", _typeAttributeValue);
        builder.AddAttributeIfNotNullOrEmpty(3, "name", NameAttributeValue);
        builder.AddAttribute(4, "class", CssClass);
        builder.AddAttribute(5, "value", CurrentValueAsString);
        builder.AddAttribute(6, "onchange", EventCallback.Factory.CreateBinder<string?>(this, __value => CurrentValueAsString = __value, CurrentValueAsString));
        builder.SetUpdatesAttributeName("value");
        builder.AddElementReferenceCapture(7, __inputReference => Element = __inputReference);
        builder.CloseElement();
    }

    /// <inheritdoc />
    protected override string FormatValueAsString(TValue? value)
        => value switch
        {
            DateTime dateTimeValue => BindConverter.FormatValue(dateTimeValue, _format, CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffsetValue => BindConverter.FormatValue(dateTimeOffsetValue, _format, CultureInfo.InvariantCulture),
            DateOnly dateOnlyValue => BindConverter.FormatValue(dateOnlyValue, _format, CultureInfo.InvariantCulture),
            TimeOnly timeOnlyValue => BindConverter.FormatValue(timeOnlyValue, _format, CultureInfo.InvariantCulture),
            _ => string.Empty, // Handles null for Nullable<DateTime>, etc.
        };

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
            validationErrorMessage = string.Format(CultureInfo.InvariantCulture, _parsingErrorMessage, DisplayName ?? FieldIdentifier.FieldName);
            return false;
        }
    }
}
