// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Used to protect/unprotect lookups with a specific key.
/// </summary>
public interface ILookupProtector
{
    /// <summary>
    /// Protect the data using the specified key.
    /// </summary>
    /// <param name="keyId">The key to use.</param>
    /// <param name="data">The data to protect.</param>
    /// <returns>The protected data.</returns>
    [return: NotNullIfNotNull("data")]
    string? Protect(string keyId, string? data);

    /// <summary>
    /// Unprotect the data using the specified key.
    /// </summary>
    /// <param name="keyId">The key to use.</param>
    /// <param name="data">The data to unprotect.</param>
    /// <returns>The original data.</returns>
    [return: NotNullIfNotNull("data")]
    string? Unprotect(string keyId, string? data);
}
