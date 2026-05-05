// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Antiforgery;

/// <summary>
/// Represents the result of cross-origin antiforgery request validation.
/// </summary>
public enum CsrfProtectionResult
{
    /// <summary>
    /// The request is allowed. The request is either same-origin, from a trusted origin,
    /// uses a safe HTTP method, or originates from a non-browser client.
    /// </summary>
    Allowed,

    /// <summary>
    /// The request is denied. The request is cross-origin and not from a trusted origin.
    /// </summary>
    Denied,
}
