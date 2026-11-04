// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Shared;

#nullable enable

internal static partial class ObjectDisposedThrowHelper
{
    /// <summary>Throws an <see cref="ObjectDisposedException"/> if the specified <paramref name="condition"/> is <see langword="true"/>.</summary>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="instance">The object whose type's full name should be included in any resulting <see cref="ObjectDisposedException"/>.</param>
    /// <exception cref="ObjectDisposedException">The <paramref name="condition"/> is <see langword="true"/>.</exception>
    public static void ThrowIf([DoesNotReturnIf(true)] bool condition, object instance)
    {
#if !NET7_0_OR_GREATER
        if (condition)
        {
            ThrowObjectDisposedException(instance);
        }
#else
        ObjectDisposedException.ThrowIf(condition, instance);
#endif
    }

    /// <summary>Throws an <see cref="ObjectDisposedException"/> if the specified <paramref name="condition"/> is <see langword="true"/>.</summary>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="type">The type whose full name should be included in any resulting <see cref="ObjectDisposedException"/>.</param>
    /// <exception cref="ObjectDisposedException">The <paramref name="condition"/> is <see langword="true"/>.</exception>
    public static void ThrowIf([DoesNotReturnIf(true)] bool condition, Type type)
    {
#if !NET7_0_OR_GREATER
        if (condition)
        {
            ThrowObjectDisposedException(type);
        }
#else
        ObjectDisposedException.ThrowIf(condition, type);
#endif
    }

#if !NET7_0_OR_GREATER
    [DoesNotReturn]
    private static void ThrowObjectDisposedException(object? instance)
    {
        throw new ObjectDisposedException(instance?.GetType().FullName);
    }

    [DoesNotReturn]
    private static void ThrowObjectDisposedException(Type? type)
    {
        throw new ObjectDisposedException(type?.FullName);
    }
#endif
}
