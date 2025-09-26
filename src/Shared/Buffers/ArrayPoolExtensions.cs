// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Buffers;

internal static class ArrayPoolExtensions
{
    public static void Return<T>(this ArrayPool<T> pool, T[] array, int lengthToClear)
    {
        array.AsSpan(0, lengthToClear).Clear();
        pool.Return(array);
    }
}
