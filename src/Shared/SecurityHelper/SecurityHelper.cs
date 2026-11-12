// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Linq;
using System.Security.Claims;

namespace Microsoft.Extensions.Internal;

/// <summary>
/// Helper code used when implementing authentication middleware
/// </summary>
internal static class SecurityHelper
{
    /// <summary>
    /// Add all ClaimsIdentities from an additional ClaimPrincipal to the ClaimsPrincipal
    /// Merges a new claims principal, placing all new identities first, and eliminating
    /// any empty unauthenticated identities from context.User
    /// </summary>
    /// <param name="existingPrincipal">The <see cref="ClaimsPrincipal"/> containing existing <see cref="ClaimsIdentity"/>.</param>
    /// <param name="additionalPrincipal">The <see cref="ClaimsPrincipal"/> containing <see cref="ClaimsIdentity"/> to be added.</param>
    public static ClaimsPrincipal MergeUserPrincipal(ClaimsPrincipal? existingPrincipal, ClaimsPrincipal? additionalPrincipal)
    {
        // For the first principal, just use the new principal rather than copying it
        if (existingPrincipal == null && additionalPrincipal != null)
        {
            return additionalPrincipal;
        }

        var newPrincipal = new ClaimsPrincipal();

        // New principal identities go first
        if (additionalPrincipal != null)
        {
            newPrincipal.AddIdentities(additionalPrincipal.Identities);
        }

        // Then add any existing non empty or authenticated identities
        if (existingPrincipal != null)
        {
            newPrincipal.AddIdentities(existingPrincipal.Identities.Where(i => i.IsAuthenticated || i.Claims.Any()));
        }
        return newPrincipal;
    }

    /// <summary>
    /// Determines whether the <paramref name="user"/> is authenticated.
    /// Uses the same aggregate semantics as authorization's
    /// <c>DenyAnonymousAuthorizationRequirement</c>: the principal is authenticated when it has an
    /// identity and at least one of its identities is authenticated (not only the primary identity).
    /// </summary>
    /// <param name="user">The <see cref="ClaimsPrincipal"/> to inspect.</param>
    /// <returns><see langword="true"/> if the principal is authenticated; otherwise <see langword="false"/>.</returns>
    public static bool IsAuthenticated(ClaimsPrincipal? user)
        => user?.Identity is not null && user.Identities.Any(static identity => identity.IsAuthenticated);
}
