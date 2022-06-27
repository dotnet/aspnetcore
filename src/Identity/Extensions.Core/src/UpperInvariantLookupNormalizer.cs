// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Implements <see cref="ILookupNormalizer"/> by converting keys to their upper cased invariant culture representation.
/// </summary>
public sealed class UpperInvariantLookupNormalizer : ILookupNormalizer
{
    /// <summary>
    /// Returns a normalized representation of the specified <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The key to normalize.</param>
    /// <returns>A normalized representation of the specified <paramref name="name"/>.</returns>
    [return: NotNullIfNotNull("name")]
    public string? NormalizeName(string? name)
    {
        if (name == null)
        {
            return null;
        }
        return name.Normalize().ToUpperInvariant();
    }

    /// <summary>
    /// Returns a normalized representation of the specified <paramref name="email"/>.
    /// </summary>
    /// <param name="email">The email to normalize.</param>
    /// <returns>A normalized representation of the specified <paramref name="email"/>.</returns>
    [return: NotNullIfNotNull("email")]
    public string? NormalizeEmail(string? email) => NormalizeName(email);
}
