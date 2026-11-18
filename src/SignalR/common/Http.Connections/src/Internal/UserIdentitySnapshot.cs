// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;

namespace Microsoft.AspNetCore.Http.Connections.Internal;

/// <summary>
/// An immutable snapshot of a principal's identity, used to decide whether a later request belongs to the
/// same user as the one that negotiated a connection.
/// </summary>
/// <remarks>
/// <para>
/// The snapshot is captured eagerly from a live <see cref="ClaimsPrincipal"/>. This matters because a
/// <see cref="ClaimsIdentity"/> subclass may decide <see cref="ClaimsIdentity.IsAuthenticated"/> for itself -
/// <see cref="System.Security.Principal.GenericIdentity"/> is authenticated whenever it has a name, even
/// though its <see cref="ClaimsIdentity.AuthenticationType"/> is empty. Rebuilding such an identity as a base
/// <see cref="ClaimsIdentity"/> would silently make it unauthenticated and discard its identity claims.
/// </para>
/// <para>
/// Extraction follows the precedence used by antiforgery's <c>DefaultClaimUidExtractor</c>: the first
/// authenticated identity carrying <c>sub</c>, <see cref="ClaimTypes.NameIdentifier"/>, or
/// <see cref="ClaimTypes.Upn"/> wins. When no identity carries one of those, every authenticated claim is used
/// instead, ordered so that claim ordering does not affect the result. When there are no authenticated claims,
/// extraction follows <c>DefaultAntiforgeryTokenGenerator</c> and falls back to the first authenticated
/// identity's nonempty <see cref="ClaimsIdentity.Name"/>.
/// </para>
/// </remarks>
internal sealed class UserIdentitySnapshot
{
    private static readonly string[] _preferredClaimTypes = ["sub", ClaimTypes.NameIdentifier, ClaimTypes.Upn];

    private readonly (string Type, string Value, string Issuer)? _preferredClaim;
    private readonly string[]? _allAuthenticatedClaims;
    private readonly string? _name;

    private UserIdentitySnapshot((string Type, string Value, string Issuer) preferredClaim)
    {
        _preferredClaim = preferredClaim;
    }

    private UserIdentitySnapshot(string[] allAuthenticatedClaims)
    {
        _allAuthenticatedClaims = allAuthenticatedClaims;
    }

    private UserIdentitySnapshot(string name)
    {
        _name = name;
    }

    /// <summary>
    /// Captures <paramref name="user"/>'s identity, or <see langword="null"/> when the principal has no
    /// authenticated claims or name to identify it by.
    /// </summary>
    public static UserIdentitySnapshot? Capture(ClaimsPrincipal? user)
    {
        if (user is null)
        {
            return null;
        }

        if (GetUserIdentityKey(user) is { } preferredClaim)
        {
            return new UserIdentitySnapshot(preferredClaim);
        }

        ClaimsIdentity? firstAuthenticatedIdentity = null;
        List<Claim>? authenticatedClaims = null;
        foreach (var identity in user.Identities)
        {
            if (!identity.IsAuthenticated)
            {
                continue;
            }

            firstAuthenticatedIdentity ??= identity;
            foreach (var claim in identity.Claims)
            {
                (authenticatedClaims ??= []).Add(claim);
            }
        }

        if (authenticatedClaims is null)
        {
            var name = firstAuthenticatedIdentity?.Name;
            return string.IsNullOrEmpty(name) ? null : new UserIdentitySnapshot(name);
        }

        authenticatedClaims.Sort(static (a, b) =>
        {
            var result = string.CompareOrdinal(a.Type, b.Type);
            if (result != 0)
            {
                return result;
            }

            result = string.CompareOrdinal(a.Value, b.Value);

            return result != 0 ? result : string.CompareOrdinal(a.Issuer, b.Issuer);
        });

        var parameters = new string[authenticatedClaims.Count * 3];
        for (var i = 0; i < authenticatedClaims.Count; i++)
        {
            var claim = authenticatedClaims[i];
            parameters[i * 3] = claim.Type;
            parameters[(i * 3) + 1] = claim.Value;
            parameters[(i * 3) + 2] = claim.Issuer;
        }

        return new UserIdentitySnapshot(parameters);
    }

    /// <summary>
    /// Returns the preferred identity claim for <paramref name="user"/>, or <see langword="null"/> when no
    /// authenticated identity carries one.
    /// </summary>
    public static (string Type, string Value, string Issuer)? GetUserIdentityKey(ClaimsPrincipal user)
    {
        foreach (var identity in user.Identities)
        {
            if (!identity.IsAuthenticated)
            {
                continue;
            }

            foreach (var claimType in _preferredClaimTypes)
            {
                var claim = identity.FindFirst(claim => string.Equals(claim.Type, claimType, StringComparison.Ordinal));
                if (claim is not null && !string.IsNullOrEmpty(claim.Value))
                {
                    return (claim.Type, claim.Value, claim.Issuer);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Determines whether two captured identities refer to the same user. Two principals that both lack an
    /// identifiable authenticated identity are considered to match, because neither is bound to a user.
    /// </summary>
    public static bool Matches(UserIdentitySnapshot? current, UserIdentitySnapshot? incoming)
    {
        if (current is null || incoming is null)
        {
            return current is null && incoming is null;
        }

        // A principal carrying a preferred identity claim can never match one that doesn't.
        if (current._preferredClaim is not null || incoming._preferredClaim is not null)
        {
            return current._preferredClaim == incoming._preferredClaim;
        }

        if (current._allAuthenticatedClaims is not null || incoming._allAuthenticatedClaims is not null)
        {
            return current._allAuthenticatedClaims is not null
                && incoming._allAuthenticatedClaims is not null
                && current._allAuthenticatedClaims.AsSpan().SequenceEqual(incoming._allAuthenticatedClaims);
        }

        var comparer = IsHttpUri(incoming._name) ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        return comparer.Equals(current._name, incoming._name);
    }

    private static bool IsHttpUri(string? value)
    {
        return value is not null
            && (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
    }
}
