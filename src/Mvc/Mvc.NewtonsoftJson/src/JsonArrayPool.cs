// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Mvc.NewtonsoftJson;

internal sealed class JsonArrayPool<T> : IArrayPool<T>
{
    private readonly ArrayPool<T> _inner;

    public JsonArrayPool(ArrayPool<T> inner)
    {
        ArgumentNullException.ThrowIfNull(inner);

        _inner = inner;
    }

    public T[] Rent(int minimumLength)
    {
        return _inner.Rent(minimumLength);
    }

    public void Return(T[]? array)
    {
        ArgumentNullException.ThrowIfNull(array);

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            _inner.Return(array, array.Length);
        }
        else
        {
            _inner.Return(array);
        }
    }
}
