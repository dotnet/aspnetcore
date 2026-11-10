// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Http.Connections.Internal
{
    internal sealed class UserIdentitySnapshot
    {
        private static readonly string[] _preferredClaimTypes =
            new[] { "sub", ClaimTypes.NameIdentifier, ClaimTypes.Upn };

        private readonly string _preferredClaimType;
        private readonly string _preferredClaimValue;
        private readonly string _preferredClaimIssuer;
        private readonly string[] _allAuthenticatedClaims;
        private readonly string _name;

        private UserIdentitySnapshot(Claim preferredClaim)
        {
            _preferredClaimType = preferredClaim.Type;
            _preferredClaimValue = preferredClaim.Value;
            _preferredClaimIssuer = preferredClaim.Issuer;
        }

        private UserIdentitySnapshot(string[] allAuthenticatedClaims)
        {
            _allAuthenticatedClaims = allAuthenticatedClaims;
        }

        private UserIdentitySnapshot(string name)
        {
            _name = name;
        }

        public static UserIdentitySnapshot Capture(ClaimsPrincipal user)
        {
            if (user == null)
            {
                return null;
            }

            var preferredClaim = GetUserIdentityClaim(user);
            if (preferredClaim != null)
            {
                return new UserIdentitySnapshot(preferredClaim);
            }

            ClaimsIdentity firstAuthenticatedIdentity = null;
            List<Claim> authenticatedClaims = null;
            foreach (var identity in user.Identities)
            {
                if (!identity.IsAuthenticated)
                {
                    continue;
                }

                if (firstAuthenticatedIdentity == null)
                {
                    firstAuthenticatedIdentity = identity;
                }

                foreach (var claim in identity.Claims)
                {
                    if (authenticatedClaims == null)
                    {
                        authenticatedClaims = new List<Claim>();
                    }

                    authenticatedClaims.Add(claim);
                }
            }

            if (authenticatedClaims == null)
            {
                var name = firstAuthenticatedIdentity?.Name;
                return string.IsNullOrEmpty(name) ? null : new UserIdentitySnapshot(name);
            }

            authenticatedClaims.Sort((a, b) =>
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

        public static string GetUserIdentityValue(ClaimsPrincipal user)
        {
            if (user == null)
            {
                return null;
            }

            var claim = GetUserIdentityClaim(user);
            return claim?.Value;
        }

        public static bool Matches(UserIdentitySnapshot current, UserIdentitySnapshot incoming)
        {
            if (current == null || incoming == null)
            {
                return current == null && incoming == null;
            }

            if (current._preferredClaimType != null || incoming._preferredClaimType != null)
            {
                return string.Equals(current._preferredClaimType, incoming._preferredClaimType, StringComparison.Ordinal)
                    && string.Equals(current._preferredClaimValue, incoming._preferredClaimValue, StringComparison.Ordinal)
                    && string.Equals(current._preferredClaimIssuer, incoming._preferredClaimIssuer, StringComparison.Ordinal);
            }

            if (current._allAuthenticatedClaims != null || incoming._allAuthenticatedClaims != null)
            {
                if (current._allAuthenticatedClaims == null
                    || incoming._allAuthenticatedClaims == null
                    || current._allAuthenticatedClaims.Length != incoming._allAuthenticatedClaims.Length)
                {
                    return false;
                }

                for (var i = 0; i < current._allAuthenticatedClaims.Length; i++)
                {
                    if (!string.Equals(current._allAuthenticatedClaims[i], incoming._allAuthenticatedClaims[i], StringComparison.Ordinal))
                    {
                        return false;
                    }
                }

                return true;
            }

            var comparison = IsHttpUri(incoming._name)
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;
            return string.Equals(current._name, incoming._name, comparison);
        }

        private static bool IsHttpUri(string value)
        {
            return value != null
                && (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
        }

        private static Claim GetUserIdentityClaim(ClaimsPrincipal user)
        {
            foreach (var identity in user.Identities)
            {
                if (!identity.IsAuthenticated)
                {
                    continue;
                }

                foreach (var claimType in _preferredClaimTypes)
                {
                    var claim = identity.FindFirst(candidate =>
                        string.Equals(candidate.Type, claimType, StringComparison.Ordinal));
                    if (claim != null && !string.IsNullOrEmpty(claim.Value))
                    {
                        return claim;
                    }
                }
            }

            return null;
        }
    }
}
