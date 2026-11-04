// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Shared;

internal static partial class ArgumentOutOfRangeThrowHelper
{
#if !NET7_0_OR_GREATER
    [DoesNotReturn]
    private static void ThrowZero<T>(string? paramName, T value)
    {
        throw new ArgumentOutOfRangeException(paramName, value, $"'{paramName}' must be a non-zero value.");
    }

    [DoesNotReturn]
    private static void ThrowNegative<T>(string? paramName, T value)
    {
        throw new ArgumentOutOfRangeException(paramName, value, $"'{paramName}' must be a non-negative value.");
    }

    [DoesNotReturn]
    private static void ThrowNegativeOrZero<T>(string? paramName, T value)
    {
        throw new ArgumentOutOfRangeException(paramName, value, $"'{paramName}' must be a non-negative and non-zero value.");
    }

    [DoesNotReturn]
    private static void ThrowGreater<T>(string? paramName, T value, T other)
    {
        throw new ArgumentOutOfRangeException(paramName, value, $"'{paramName}' must be less than or equal to '{other}'.");
    }

    [DoesNotReturn]
    private static void ThrowGreaterEqual<T>(string? paramName, T value, T other)
    {
        throw new ArgumentOutOfRangeException(paramName, value, $"'{value}' must be less than '{other}'.");
    }

    [DoesNotReturn]
    private static void ThrowLess<T>(string? paramName, T value, T other)
    {
        throw new ArgumentOutOfRangeException(paramName, value, $"'{value}' must be greater than or equal to '{other}'.");
    }

    [DoesNotReturn]
    private static void ThrowLessEqual<T>(string? paramName, T value, T other)
    {
        throw new ArgumentOutOfRangeException(paramName, value, $"'{value}' must be greater than '{other}'.");
    }
#endif

    /// <summary>Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is zero.</summary>
    /// <param name="value">The argument to validate as non-zero.</param>
    /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
    public static void ThrowIfZero(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
#if !NET7_0_OR_GREATER
        if (value == 0)
        {
            ThrowZero(paramName, value);
        }
#else
        ArgumentOutOfRangeException.ThrowIfZero(value, paramName);
#endif
    }

    /// <summary>Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is negative.</summary>
    /// <param name="value">The argument to validate as non-negative.</param>
    /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
    public static void ThrowIfNegative(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
#if !NET7_0_OR_GREATER
        if (value < 0)
        {
            ThrowNegative(paramName, value);
        }
#else
        ArgumentOutOfRangeException.ThrowIfNegative(value, paramName);
#endif
    }

    /// <summary>Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is negative.</summary>
    /// <param name="value">The argument to validate as non-negative.</param>
    /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
    public static void ThrowIfNegative(long value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
#if !NET7_0_OR_GREATER
        if (value < 0)
        {
            ThrowNegative(paramName, value);
        }
#else
        ArgumentOutOfRangeException.ThrowIfNegative(value, paramName);
#endif
    }

    /// <summary>Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is negative or zero.</summary>
    /// <param name="value">The argument to validate as non-zero or non-negative.</param>
    /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
    public static void ThrowIfNegativeOrZero(int value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
#if !NET7_0_OR_GREATER
        if (value <= 0)
        {
            ThrowNegativeOrZero(paramName, value);
        }
#else
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, paramName);
#endif
    }

    /// <summary>Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is greater than <paramref name="other"/>.</summary>
    /// <param name="value">The argument to validate as less or equal than <paramref name="other"/>.</param>
    /// <param name="other">The value to compare with <paramref name="value"/>.</param>
    /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
    public static void ThrowIfGreaterThan<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IComparable<T>
    {
#if !NET7_0_OR_GREATER
        if (value.CompareTo(other) > 0)
        {
            ThrowGreater(paramName, value, other);
        }
#else
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, other, paramName);
#endif
    }

    /// <summary>Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is greater than or equal <paramref name="other"/>.</summary>
    /// <param name="value">The argument to validate as less than <paramref name="other"/>.</param>
    /// <param name="other">The value to compare with <paramref name="value"/>.</param>
    /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
    public static void ThrowIfGreaterThanOrEqual<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IComparable<T>
    {
#if !NET7_0_OR_GREATER
        if (value.CompareTo(other) >= 0)
        {
            ThrowGreaterEqual(paramName, value, other);
        }
#else
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(value, other, paramName);
#endif
    }

    /// <summary>Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is less than <paramref name="other"/>.</summary>
    /// <param name="value">The argument to validate as greatar than or equal than <paramref name="other"/>.</param>
    /// <param name="other">The value to compare with <paramref name="value"/>.</param>
    /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
    public static void ThrowIfLessThan<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IComparable<T>
    {
#if !NET7_0_OR_GREATER
        if (value.CompareTo(other) < 0)
        {
            ThrowLess(paramName, value, other);
        }
#else
        ArgumentOutOfRangeException.ThrowIfLessThan(value, other, paramName);
#endif
    }

    /// <summary>Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is less than or equal <paramref name="other"/>.</summary>
    /// <param name="value">The argument to validate as greatar than than <paramref name="other"/>.</param>
    /// <param name="other">The value to compare with <paramref name="value"/>.</param>
    /// <param name="paramName">The name of the parameter with which <paramref name="value"/> corresponds.</param>
    public static void ThrowIfLessThanOrEqual<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IComparable<T>
    {
#if !NET7_0_OR_GREATER
        if (value.CompareTo(other) <= 0)
        {
            ThrowLessEqual(paramName, value, other);
        }
#else
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, other, paramName);
#endif
    }
}
