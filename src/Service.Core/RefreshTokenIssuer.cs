// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class RefreshTokenIssuer : IRefreshTokenIssuer
    {
        private static readonly string[] ClaimsToFilter = new string[]
        {
            IdentityServiceClaimTypes.ObjectId,
            IdentityServiceClaimTypes.Issuer,
            IdentityServiceClaimTypes.Audience,
            IdentityServiceClaimTypes.IssuedAt,
            IdentityServiceClaimTypes.Expires,
            IdentityServiceClaimTypes.NotBefore,
        };

        private static readonly string[] ClaimsToExclude = new string[]
        {
            IdentityServiceClaimTypes.JwtId,
            IdentityServiceClaimTypes.Issuer,
            IdentityServiceClaimTypes.Subject,
            IdentityServiceClaimTypes.Audience,
            IdentityServiceClaimTypes.Scope,
            IdentityServiceClaimTypes.IssuedAt,
            IdentityServiceClaimTypes.Expires,
            IdentityServiceClaimTypes.NotBefore,
        };

        private readonly ISecureDataFormat<RefreshToken> _dataFormat;
        private readonly ITokenClaimsManager _claimsManager;

        public RefreshTokenIssuer(
            ITokenClaimsManager claimsManager,
            ISecureDataFormat<RefreshToken> dataFormat)
        {
            _claimsManager = claimsManager;
            _dataFormat = dataFormat;
        }

        public async Task IssueRefreshTokenAsync(TokenGeneratingContext context)
        {
            var refreshToken = await CreateRefreshTokenAsync(context);
            var token = _dataFormat.Protect(refreshToken);
            context.AddToken(new TokenResult(refreshToken, token));
        }

        private async Task<RefreshToken> CreateRefreshTokenAsync(TokenGeneratingContext context)
        {
            await _claimsManager.CreateClaimsAsync(context);

            var claims = context.CurrentClaims;

            return new RefreshToken(claims);
        }

        public Task<AuthorizationGrant> ExchangeRefreshTokenAsync(OpenIdConnectMessage message)
        {
            var refreshToken = _dataFormat.Unprotect(message.RefreshToken);

            var resource = refreshToken.Resource;
            var scopes = refreshToken.Scopes
                .Select(s => ApplicationScope.CanonicalScopes.TryGetValue(s, out var scope) ? scope : new ApplicationScope(resource, s));

            return Task.FromResult(AuthorizationGrant.Valid(
                refreshToken.UserId,
                refreshToken.ClientId,
                refreshToken.GrantedTokens,
                scopes,
                refreshToken));
        }
    }
}
