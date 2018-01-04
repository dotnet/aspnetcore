// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class RefreshToken : Token
    {
        public RefreshToken(IEnumerable<Claim> claims)
            : base(ValidateClaims(claims))
        {
        }

        private static IEnumerable<Claim> ValidateClaims(IEnumerable<Claim> claims)
        {
            EnsureUniqueClaim(IdentityServiceClaimTypes.UserId, claims);
            EnsureUniqueClaim(IdentityServiceClaimTypes.ClientId, claims);
            EnsureUniqueClaim(IdentityServiceClaimTypes.Scope, claims);
            EnsureRequiredClaim(IdentityServiceClaimTypes.GrantedToken, claims);

            return claims;
        }

        public override string Kind => TokenTypes.RefreshToken;
        public string UserId => GetClaimValue(IdentityServiceClaimTypes.UserId);
        public string ClientId => GetClaimValue(IdentityServiceClaimTypes.ClientId);
        public string Resource => GetClaimValue(IdentityServiceClaimTypes.Resource);
        public string Issuer => GetClaimValue(IdentityServiceClaimTypes.Issuer);
        public IEnumerable<string> GrantedTokens => GetClaimValuesOrEmpty(IdentityServiceClaimTypes.GrantedToken);
        public IEnumerable<string> Scopes => GetClaimValuesOrEmpty(IdentityServiceClaimTypes.Scope);
    }
}
