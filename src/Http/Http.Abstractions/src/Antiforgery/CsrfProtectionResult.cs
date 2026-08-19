// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Antiforgery;

/// <summary>
/// Represents the result of cross-origin antiforgery request validation.
/// </summary>
public sealed class CsrfProtectionResult
{
    private static readonly CsrfProtectionResult _allowed = new(isAllowed: true);
    private static readonly CsrfProtectionResult _denied = new(isAllowed: false);

    private CsrfProtectionResult(bool isAllowed)
    {
        IsAllowed = isAllowed;
    }

    /// <summary>
    /// Gets a value indicating whether the request is allowed.
    /// </summary>
    public bool IsAllowed { get; }

    /// <summary>
    /// Returns a <see cref="CsrfProtectionResult"/> indicating the request is allowed.
    /// </summary>
    public static CsrfProtectionResult Allowed() => _allowed;

    /// <summary>
    /// Returns a <see cref="CsrfProtectionResult"/> indicating the request is denied.
    /// </summary>
    public static CsrfProtectionResult Denied() => _denied;
}
