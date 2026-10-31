// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Used to pass information during the SecurityStamp validation event.
/// </summary>
public class SecurityStampRefreshingPrincipalContext
{
    /// <summary>
    /// The principal contained in the current cookie.
    /// </summary>
    public ClaimsPrincipal? CurrentPrincipal { get; set; }

    /// <summary>
    /// The new principal which should replace the current.
    /// </summary>
    public ClaimsPrincipal? NewPrincipal { get; set; }
}
