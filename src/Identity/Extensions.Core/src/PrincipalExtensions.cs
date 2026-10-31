// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Shared;

namespace System.Security.Claims;

/// <summary>
/// Claims related extensions for <see cref="ClaimsPrincipal"/>.
/// </summary>
public static class PrincipalExtensions
{
    /// <summary>
    /// Returns the value for the first claim of the specified type, otherwise null if the claim is not present.
    /// </summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/> instance this method extends.</param>
    /// <param name="claimType">The claim type whose first value should be returned.</param>
    /// <returns>The value of the first instance of the specified claim type, or null if the claim is not present.</returns>
    public static string? FindFirstValue(this ClaimsPrincipal principal, string claimType)
    {
        ArgumentNullThrowHelper.ThrowIfNull(principal);
        var claim = principal.FindFirst(claimType);
        return claim?.Value;
    }
}
