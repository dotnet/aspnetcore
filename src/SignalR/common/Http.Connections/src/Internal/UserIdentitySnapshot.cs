// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;

namespace Microsoft.AspNetCore.Http.Connections.Internal;

internal sealed class UserIdentitySnapshot
{
    private static readonly string[] _preferredClaimTypes =
        new[] { "sub", ClaimTypes.NameIdentifier, ClaimTypes.Upn };

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
                (authenticatedClaims ??= new List<Claim>()).Add(claim);
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

    public static bool Matches(UserIdentitySnapshot? current, UserIdentitySnapshot? incoming)
    {
        if (current is null || incoming is null)
        {
            return current is null && incoming is null;
        }

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
