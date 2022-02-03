// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Provides an abstraction for normalizing keys (emails/names) for lookup purposes.
/// </summary>
public interface ILookupNormalizer
{
    /// <summary>
    /// Returns a normalized representation of the specified <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The key to normalize.</param>
    /// <returns>A normalized representation of the specified <paramref name="name"/>.</returns>
    string NormalizeName(string name);

    /// <summary>
    /// Returns a normalized representation of the specified <paramref name="email"/>.
    /// </summary>
    /// <param name="email">The email to normalize.</param>
    /// <returns>A normalized representation of the specified <paramref name="email"/>.</returns>
    string NormalizeEmail(string email);

}
