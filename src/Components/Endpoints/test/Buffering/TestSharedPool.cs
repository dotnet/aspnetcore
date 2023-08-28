// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.AspNetCore.Components.Endpoints.Rendering;

internal class TestSharedPool<T> : ArrayPool<T>
{
    public List<T[]> UnreturnedValues { get; } = new();

    public override T[] Rent(int minimumLength)
    {
        var result = new T[minimumLength];
        UnreturnedValues.Add(result);
        return result;
    }

    public override void Return(T[] array, bool clearArray = false)
    {
        if (!UnreturnedValues.Remove(array))
        {
            throw new InvalidOperationException("Tried to return a value not previously rented.");
        }
    }
}
