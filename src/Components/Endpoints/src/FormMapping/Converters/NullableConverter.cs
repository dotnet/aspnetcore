// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal sealed class NullableConverter<T>(FormDataConverter<T> nonNullableConverter) : FormDataConverter<T?>, ISingleValueConverter<T?> where T : struct
{
    private readonly FormDataConverter<T> _nonNullableConverter = nonNullableConverter;

    public bool CanConvertSingleValue() => _nonNullableConverter is ISingleValueConverter<T> singleValueConverter &&
        singleValueConverter.CanConvertSingleValue();

    public bool TryConvertValue(ref FormDataReader reader, string value, out T? result)
    {
        if (string.IsNullOrEmpty(value))
        {
            // Form post sends empty string for a form field that does not have a value,
            // in case of nullable value types, that should be treated as null and
            // should not be parsed for its underlying type
            result = null;
            return true;
        }

        var converter = (ISingleValueConverter<T>)_nonNullableConverter;

        if (converter.TryConvertValue(ref reader, value, out var converted))
        {
            result = converted;
            return true;
        }
        else
        {
            result = null;
            return false;
        }
    }

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    internal override bool TryRead(ref FormDataReader reader, Type type, FormDataMapperOptions options, out T? result, out bool found)
    {
        // Donot call non-nullable converter's TryRead method, it will fail to parse empty
        // string. Call the TryConvertValue method above (similar to ParsableConverter) so
        // that it can handle the empty string correctly
        found = reader.TryGetValue(out var value);
        if (!found)
        {
            result = default;
            return true;
        }
        else
        {
            return TryConvertValue(ref reader, value!, out result!);
        }
    }
}
