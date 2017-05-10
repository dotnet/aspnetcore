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
    public class JwtAccessTokenIssuer : IAccessTokenIssuer
    {
        private static readonly string[] ClaimsToFilter = new string[]
        {
            IdentityServiceClaimTypes.TokenUniqueId,
            IdentityServiceClaimTypes.ObjectId,
            IdentityServiceClaimTypes.Issuer,
            IdentityServiceClaimTypes.Audience,
            IdentityServiceClaimTypes.IssuedAt,
            IdentityServiceClaimTypes.Expires,
            IdentityServiceClaimTypes.NotBefore,
        };

        private readonly JwtSecurityTokenHandler _handler;
        private readonly IdentityServiceOptions _options;
        private readonly ITokenClaimsManager _claimsManager;
        private readonly ISigningCredentialsPolicyProvider _credentialsProvider;

        public JwtAccessTokenIssuer(
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

        public async Task IssueAccessTokenAsync(TokenGeneratingContext context)
        {
            var accessToken = await CreateAccessTokenAsync(context);
            var subjectIdentity = CreateSubject(accessToken);

            var descriptor = new SecurityTokenDescriptor();

            descriptor.Issuer = accessToken.Issuer;
            descriptor.Audience = accessToken.Audience;
            descriptor.Subject = subjectIdentity;
            descriptor.Expires = accessToken.Expires.UtcDateTime;
            descriptor.NotBefore = accessToken.NotBefore.UtcDateTime;

            var credentialsDescriptor = await _credentialsProvider.GetSigningCredentialsAsync();
            descriptor.SigningCredentials = credentialsDescriptor.Credentials;

            var token = _handler.CreateJwtSecurityToken(descriptor);

            token.Payload.Remove(IdentityServiceClaimTypes.JwtId);
            token.Payload.Remove(IdentityServiceClaimTypes.IssuedAt);
            //token.Payload.Add(IdentityServiceClaimTypes.JwtId, accessToken.Id);

            context.AddToken(new TokenResult(accessToken, _handler.WriteToken(token), TokenKinds.Bearer));
        }

        private ClaimsIdentity CreateSubject(AccessToken accessToken) => new ClaimsIdentity(GetFilteredClaims(accessToken));

        private IEnumerable<Claim> GetFilteredClaims(AccessToken token)
        {
            foreach (var claim in token)
            {
                if (!ClaimsToFilter.Contains(claim.Type))
                {
                    yield return claim;
                }
            }
        }

        private async Task<AccessToken> CreateAccessTokenAsync(TokenGeneratingContext context)
        {
            await _claimsManager.CreateClaimsAsync(context);

            var claims = context.CurrentClaims;
            return new AccessToken(claims);
        }
    }
}
