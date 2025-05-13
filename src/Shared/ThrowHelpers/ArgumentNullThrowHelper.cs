// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Shared;

#nullable enable

internal static partial class ArgumentNullThrowHelper
{
    /// <summary>Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null.</summary>
    /// <param name="argument">The reference type argument to validate as non-null.</param>
    /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
    public static void ThrowIfNull(
#if INTERNAL_NULLABLE_ATTRIBUTES || NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        [NotNull]
#endif
        object? argument, [CallerArgumentExpression("argument")] string? paramName = null)
    {
#if !NET7_0_OR_GREATER || NETSTANDARD || NETFRAMEWORK
        if (argument is null)
        {
            Throw(paramName);
        }
#else
        ArgumentNullException.ThrowIfNull(argument, paramName);
#endif
    }

    /// <summary>Throws an <see cref="ArgumentException"/> if <paramref name="argument"/> is null or empty.</summary>
    /// <param name="argument">The <see cref="string"/> argument to validate as non-null and non-empty.</param>
    /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
    public static void ThrowIfNullOrEmpty(
#if INTERNAL_NULLABLE_ATTRIBUTES || NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        [NotNull]
#endif
        string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
#if !NET7_0_OR_GREATER || NETSTANDARD || NETFRAMEWORK
        if (argument is null)
        {
            Throw(paramName);
        }
        else if (argument.Length == 0)
        {
            throw new ArgumentException("Must not be null or empty", paramName);
        }
#else
        ArgumentException.ThrowIfNullOrEmpty(argument, paramName);
#endif
    }

#if !NET7_0_OR_GREATER || NETSTANDARD || NETFRAMEWORK
#if INTERNAL_NULLABLE_ATTRIBUTES || NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
    [DoesNotReturn]
#endif
    internal static void Throw(string? paramName) =>
        throw new ArgumentNullException(paramName);
#endif
}
