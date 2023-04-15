// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Shared;

#nullable enable

internal static partial class ArgumentThrowHelper
{
#if !NET7_0_OR_GREATER
    private const string EmptyString = "";
#endif

    /// <summary>Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null or an <see cref="ArgumentException"/> if it is empty.</summary>
    /// <param name="argument">The reference type argument to validate as neither null nor empty.</param>
    /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
    public static void ThrowIfNullOrEmpty([NotNull] string? argument, [CallerArgumentExpression("argument")] string? paramName = null)
    {
#if !NET7_0_OR_GREATER
        if (argument is null or EmptyString)
        {
            ArgumentNullThrowHelper.ThrowIfNull(argument);
            Throw(paramName);
        }
#else
        ArgumentException.ThrowIfNullOrEmpty(argument, paramName);
#endif
    }

#if !NET7_0_OR_GREATER
    [DoesNotReturn]
    internal static void Throw(string? paramName) =>
        throw new ArgumentException("The value cannot be an empty string.", paramName);
#endif
}
