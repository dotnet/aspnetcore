// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Buffers;

internal static class ArrayPoolExtensions
{
    /// <summary>
    /// Clears the specified range and returns the array to the pool.
    /// </summary>
    public static void Return<T>(this ArrayPool<T> pool, T[] array, int lengthToClear)
    {
        array.AsSpan(0, lengthToClear).Clear();
        pool.Return(array);
    }

    /// <summary>
    /// Clears the specified range if <typeparamref name="T"/> is a reference type or
    /// contains references and returns the array to the pool.
    /// </summary>
    /// <remarks>
    /// For .NET Framework, falls back to checking if <typeparamref name="T"/> is not a primitive type
    /// where <c>RuntimeHelpers.IsReferenceOrContainsReferences&lt;T&gt;()</c> is not available.
    /// </remarks>
    public static void ReturnAndClearReferences<T>(this ArrayPool<T> pool, T[] array, int lengthToClear)
    {
#if NET
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
#else
        if (!typeof(T).IsPrimitive)
#endif
        {
            array.AsSpan(0, lengthToClear).Clear();
        }

        pool.Return(array);
    }
}
