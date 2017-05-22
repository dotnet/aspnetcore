// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class AuthorizationCode : Token
    {
        public AuthorizationCode(IEnumerable<Claim> claims)
            : base(ValidateClaims(claims))
        {
        }

        private static IEnumerable<Claim> ValidateClaims(IEnumerable<Claim> claims)
        {
            EnsureUniqueClaim(IdentityServiceClaimTypes.UserId, claims);
            EnsureUniqueClaim(IdentityServiceClaimTypes.ClientId, claims);
            EnsureUniqueClaim(IdentityServiceClaimTypes.RedirectUri, claims);
            EnsureUniqueClaim(IdentityServiceClaimTypes.Scope, claims);
            EnsureRequiredClaim(IdentityServiceClaimTypes.GrantedToken, claims);

            return claims;
        }

        public override string Kind => TokenTypes.AuthorizationCode;
        public string UserId => GetClaimValue(IdentityServiceClaimTypes.UserId);
        public string ClientId => GetClaimValue(IdentityServiceClaimTypes.ClientId);
        public string Resource => GetClaimValue(IdentityServiceClaimTypes.Resource);
        public string RedirectUri => GetClaimValue(IdentityServiceClaimTypes.RedirectUri);
        public string CodeChallenge => GetClaimValue(IdentityServiceClaimTypes.CodeChallenge);
        public string CodeChallengeMethod => GetClaimValue(IdentityServiceClaimTypes.CodeChallengeMethod);
        public IEnumerable<string> Scopes => GetClaimValuesOrEmpty(IdentityServiceClaimTypes.Scope);
        public IEnumerable<string> GrantedTokens => GetClaimValuesOrEmpty(IdentityServiceClaimTypes.GrantedToken);
        public string Nonce => GetClaimValue(IdentityServiceClaimTypes.Nonce);
    }
}
