// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class AccessToken : Token
    {
        public AccessToken(IEnumerable<Claim> claims)
            : base(ValidateClaims(claims))
        {
        }

        private static IEnumerable<Claim> ValidateClaims(IEnumerable<Claim> claims)
        {
            EnsureUniqueClaim(IdentityServiceClaimTypes.Issuer, claims);
            EnsureUniqueClaim(IdentityServiceClaimTypes.Subject, claims);
            EnsureUniqueClaim(IdentityServiceClaimTypes.Audience, claims);
            EnsureUniqueClaim(IdentityServiceClaimTypes.Scope, claims);
            EnsureUniqueClaim(IdentityServiceClaimTypes.AuthorizedParty, claims);
            return claims;
        }

        public override string Kind => TokenTypes.AccessToken;
        public string Issuer => GetClaimValue(IdentityServiceClaimTypes.Issuer);
        public string Subject => GetClaimValue(IdentityServiceClaimTypes.Subject);
        public string Audience => GetClaimValue(IdentityServiceClaimTypes.Audience);
        public string AuthorizedParty => GetClaimValue(IdentityServiceClaimTypes.AuthorizedParty);
        public IEnumerable<string> Scopes => GetClaimValuesOrEmpty(IdentityServiceClaimTypes.Scope);
    }
}
