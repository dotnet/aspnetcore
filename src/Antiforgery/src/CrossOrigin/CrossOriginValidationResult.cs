// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Antiforgery.CrossOrigin;

/// <summary>
/// Result of cross-origin request validation.
/// </summary>
public enum CrossOriginValidationResult
{
    /// <summary>
    /// Request is safe (same-origin, trusted, safe method)
    /// </summary>
    Allowed,

    /// <summary>
    /// Request is cross-origin attack
    /// </summary>
    Denied,

    /// <summary>
    /// Not enough information to determine the outcome.
    /// </summary>
    Unknown
}
