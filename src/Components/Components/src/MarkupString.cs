// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// A string value that can be rendered as markup such as HTML.
/// </summary>
[TypeConverter(typeof(MarkupStringTypeConverter))]
public readonly struct MarkupString
{
    /// <summary>
    /// Constructs an instance of <see cref="MarkupString"/>.
    /// </summary>
    /// <param name="value">The value for the new instance.</param>
    public MarkupString(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the value of the <see cref="MarkupString"/>.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Casts a <see cref="string"/> to a <see cref="MarkupString"/>.
    /// </summary>
    /// <param name="value">The <see cref="string"/> value.</param>
    public static explicit operator MarkupString(string value)
        => new MarkupString(value);

    /// <inheritdoc />
    public override string ToString()
        => Value ?? string.Empty;

    private class MarkupStringTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
            => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is string markup)
            {
                return (MarkupString)markup;
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override bool CanConvertTo(ITypeDescriptorContext? context, [NotNullWhen(true)] Type? destinationType)
            => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is MarkupString markup)
            {
                return markup.Value ?? "";
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
