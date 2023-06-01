// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;

internal sealed class NullableConverter<T> : FormDataConverter<T?> where T : struct
{
    private readonly FormDataConverter<T> _nonNullableConverter;

    public NullableConverter(FormDataConverter<T> nonNullableConverter)
    {
        _nonNullableConverter = nonNullableConverter;
    }

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
