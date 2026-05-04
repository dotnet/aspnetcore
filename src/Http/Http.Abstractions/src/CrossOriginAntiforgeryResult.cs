// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Antiforgery;

/// <summary>
/// Represents the result of cross-origin antiforgery request validation
/// based on Fetch Metadata headers (Sec-Fetch-Site) and Origin header.
/// </summary>
public enum CrossOriginAntiforgeryResult
{
    /// <summary>
    /// The request passed cross-origin validation and should be allowed.
    /// </summary>
    Allowed,

    /// <summary>
    /// The request failed cross-origin validation and should be blocked.
    /// </summary>
    Denied
}
