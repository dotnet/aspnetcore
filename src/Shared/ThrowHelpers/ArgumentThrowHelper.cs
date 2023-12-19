// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Shared;

#nullable enable

internal static partial class ArgumentThrowHelper
{
    /// <summary>Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null or an <see cref="ArgumentException"/> if it is empty.</summary>
    /// <param name="argument">The reference type argument to validate as neither null nor empty.</param>
    /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
    public static void ThrowIfNullOrEmpty(
#if INTERNAL_NULLABLE_ATTRIBUTES || NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        [NotNull]
#endif
        string? argument, [CallerArgumentExpression("argument")] string? paramName = null)
    {
#if !NET7_0_OR_GREATER || NETSTANDARD || NETFRAMEWORK
        if (argument is null || argument == string.Empty)
        {
            ArgumentNullThrowHelper.ThrowIfNull(argument);
            Throw(paramName);
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
        throw new ArgumentException("The value cannot be an empty string.", paramName);
#endif

    /// <summary>Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null or an <see cref="ArgumentException"/> if it is empty or whitespace.</summary>
    /// <param name="argument">The reference type argument to validate as neither null nor empty nor whitespace.</param>
    /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
    public static void ThrowIfNullOrWhiteSpace(
#if INTERNAL_NULLABLE_ATTRIBUTES || NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
        [NotNull]
#endif
        string? argument, [CallerArgumentExpression("argument")] string? paramName = null)
    {
        ArgumentNullThrowHelper.ThrowIfNull(argument);

#if !NET8_0_OR_GREATER || NETSTANDARD || NETFRAMEWORK
        if (string.IsNullOrWhiteSpace(argument))
        {
            ThrowNullOrWhiteSpaceException(paramName);
        }
#else
        ArgumentException.ThrowIfNullOrWhiteSpace(argument, paramName);
#endif
    }

#if !NET8_0_OR_GREATER || NETSTANDARD || NETFRAMEWORK
#if INTERNAL_NULLABLE_ATTRIBUTES || NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
    [DoesNotReturn]
#endif
    internal static void ThrowNullOrWhiteSpaceException(string? paramName) =>
        throw new ArgumentException("The value cannot be an empty or whitespace string.", paramName);
#endif
}
