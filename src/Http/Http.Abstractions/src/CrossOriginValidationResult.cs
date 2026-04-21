// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Antiforgery;

/// <summary>
/// Represents the result of cross-origin request validation.
/// </summary>
public enum CrossOriginValidationResult
{
    /// <summary>
    /// The request passed cross-origin validation and should be allowed.
    /// </summary>
    Allowed,

    /// <summary>
    /// The request failed cross-origin validation and should be blocked.
    /// </summary>
    Denied,

    /// <summary>
    /// Cross-origin protection is not enabled. The caller should fall back
    /// to other protection mechanisms or fail as appropriate.
    /// </summary>
    Disabled
}
