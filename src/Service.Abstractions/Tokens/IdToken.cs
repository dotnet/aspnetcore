// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class IdToken : Token
    {
        public IdToken(IEnumerable<Claim> claims)
            : base(ValidateClaims(claims))
        {
        }

        private static IEnumerable<Claim> ValidateClaims(IEnumerable<Claim> claims)
        {
            EnsureUniqueClaim(IdentityServiceClaimTypes.Issuer, claims);
            EnsureUniqueClaim(IdentityServiceClaimTypes.Subject, claims);
            EnsureUniqueClaim(IdentityServiceClaimTypes.Audience, claims);
            EnsureUniqueClaim(IdentityServiceClaimTypes.Nonce, claims, required: false);
            EnsureUniqueClaim(IdentityServiceClaimTypes.CodeHash, claims, required: false);
            EnsureUniqueClaim(IdentityServiceClaimTypes.AccessTokenHash, claims, required: false);
            return claims;
        }

        public override string Kind => TokenTypes.IdToken;
        public string Issuer => GetClaimValue(IdentityServiceClaimTypes.Issuer);
        public string Subject => GetClaimValue(IdentityServiceClaimTypes.Subject);
        public string Audience => GetClaimValue(IdentityServiceClaimTypes.Audience);
        public string Nonce => GetClaimValue(IdentityServiceClaimTypes.Nonce);
        public string CodeHash => GetClaimValue(IdentityServiceClaimTypes.CodeHash);
        public string AccessTokenHash => GetClaimValue(IdentityServiceClaimTypes.AccessTokenHash);
    }
}
