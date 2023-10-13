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
    internal override bool TryRead(ref FormDataReader context, Type type, FormDataMapperOptions options, out T? result, out bool found)
    {
        if (!(_nonNullableConverter.TryRead(ref context, type, options, out var innerResult, out found) && found))
        {
            result = null;
            return false;
        }
        else
        {
            result = innerResult;
            return true;
        }
    }
}
