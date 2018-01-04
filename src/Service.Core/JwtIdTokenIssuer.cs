// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class JwtIdTokenIssuer : IIdTokenIssuer
    {
        private static readonly string[] ClaimsToFilter = new string[]
        {
            IdentityServiceClaimTypes.TokenUniqueId,
            IdentityServiceClaimTypes.Issuer,
            IdentityServiceClaimTypes.Audience,
            IdentityServiceClaimTypes.IssuedAt,
            IdentityServiceClaimTypes.Expires,
            IdentityServiceClaimTypes.NotBefore,
            IdentityServiceClaimTypes.Nonce,
            IdentityServiceClaimTypes.CodeHash,
            IdentityServiceClaimTypes.AccessTokenHash,
        };

        private readonly ITokenClaimsManager _claimsManager;
        private readonly JwtSecurityTokenHandler _handler;
        private readonly IdentityServiceOptions _options;
        private readonly ISigningCredentialsPolicyProvider _credentialsProvider;

        public JwtIdTokenIssuer(
            ITokenClaimsManager claimsManager,
            ISigningCredentialsPolicyProvider credentialsProvider,
            JwtSecurityTokenHandler handler,
            IOptions<IdentityServiceOptions> options)
        {
            _claimsManager = claimsManager;
            _credentialsProvider = credentialsProvider;
            _handler = handler;
            _options = options.Value;
        }

        public async Task IssueIdTokenAsync(TokenGeneratingContext context)
        {
            var idToken = await CreateIdTokenAsync(context);
            var subjectIdentity = CreateSubject(idToken);

            var descriptor = new SecurityTokenDescriptor();

            descriptor.Issuer = idToken.Issuer;
            descriptor.Audience = idToken.Audience;
            descriptor.Subject = subjectIdentity;
            descriptor.IssuedAt = idToken.IssuedAt.UtcDateTime;
            descriptor.Expires = idToken.Expires.UtcDateTime;
            descriptor.NotBefore = idToken.NotBefore.UtcDateTime;

            var credentialsDescriptor = await _credentialsProvider.GetSigningCredentialsAsync();
            descriptor.SigningCredentials = credentialsDescriptor.Credentials;

            var token = _handler.CreateJwtSecurityToken(descriptor);

            token.Payload.Remove(IdentityServiceClaimTypes.JwtId);
            //token.Payload.Add(IdentityServiceClaimTypes.JwtId, idToken.Id);

            if (idToken.Nonce != null)
            {
                token.Payload.AddClaim(new Claim(IdentityServiceClaimTypes.Nonce, idToken.Nonce));
            }

            if (idToken.CodeHash != null)
            {
                token.Payload.AddClaim(new Claim(IdentityServiceClaimTypes.CodeHash, idToken.CodeHash));
            }

            if (idToken.AccessTokenHash != null)
            {
                token.Payload.AddClaim(new Claim(IdentityServiceClaimTypes.AccessTokenHash, idToken.AccessTokenHash));
            }

            context.AddToken(new TokenResult(idToken, _handler.WriteToken(token)));
        }

        private ClaimsIdentity CreateSubject(IdToken idToken) =>
            new ClaimsIdentity(GetFilteredClaims(idToken));

        private IEnumerable<Claim> GetFilteredClaims(IdToken token)
        {
            foreach (var claim in token)
            {
                if (!ClaimsToFilter.Contains(claim.Type))
                {
                    yield return claim;
                }
            }
        }

        private async Task<IdToken> CreateIdTokenAsync(TokenGeneratingContext context)
        {
            await _claimsManager.CreateClaimsAsync(context);
            
            var claims = context.CurrentClaims;

            return new IdToken(claims);
        }
    }
}
