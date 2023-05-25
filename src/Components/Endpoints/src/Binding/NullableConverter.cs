// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;

internal class NullableConverterFactory : IFormDataConverterFactory
{
    public static bool CanConvert(Type type, FormDataSerializerOptions options)
    {
        var underlyingType = Nullable.GetUnderlyingType(type);
        return underlyingType != null && options.HasConverter(underlyingType);
    }

    public static FormDataConverter CreateConverter(Type type, FormDataSerializerOptions options)
    {
        var underlyingType = Nullable.GetUnderlyingType(type);
        Debug.Assert(underlyingType != null);
        var underlyingConverter = options.ResolveConverter(underlyingType);
        Debug.Assert(underlyingConverter != null);
        var expectedConverterType = typeof(NullableConverter<>).MakeGenericType(underlyingType);
        Debug.Assert(expectedConverterType != null);
        return Activator.CreateInstance(expectedConverterType, underlyingConverter) as FormDataConverter ??
            throw new InvalidOperationException($"Unable to create converter for type '{type}'.");
    }
}

internal class NullableConverter<T> : FormDataConverter<T?> where T : struct
{
    private readonly FormDataConverter<T> _nonNullableConverter;

    public NullableConverter(FormDataConverter<T> nonNullableConverter)
    {
        _nonNullableConverter = nonNullableConverter;
    }

    internal override bool TryRead(ref FormDataReader context, Type type, FormDataSerializerOptions options, out T? result, out bool found)
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
