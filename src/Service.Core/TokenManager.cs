// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class TokenManager : ITokenManager
    {
        private readonly IAccessTokenIssuer _accessTokenIssuer;
        private readonly IAuthorizationCodeIssuer _codeIssuer;
        private readonly IIdTokenIssuer _idTokenIssuer;
        private readonly IRefreshTokenIssuer _refreshTokenIssuer;
        private readonly ProtocolErrorProvider _errorProvider;

        public TokenManager(
            IAuthorizationCodeIssuer codeIssuer,
            IAccessTokenIssuer accessTokenIssuer,
            IIdTokenIssuer idTokenIssuer,
            IRefreshTokenIssuer refreshTokenIssuer,
            ProtocolErrorProvider errorProvider)
        {
            _codeIssuer = codeIssuer;
            _accessTokenIssuer = accessTokenIssuer;
            _idTokenIssuer = idTokenIssuer;
            _refreshTokenIssuer = refreshTokenIssuer;
            _errorProvider = errorProvider;
        }

        public async Task IssueTokensAsync(TokenGeneratingContext context)
        {
            if (context.RequestGrants.Tokens.Contains(TokenTypes.AuthorizationCode))
            {
                context.InitializeForToken(TokenTypes.AuthorizationCode);
                await _codeIssuer.CreateAuthorizationCodeAsync(context);
            }

            if (context.RequestGrants.Tokens.Contains(TokenTypes.AccessToken))
            {
                context.InitializeForToken(TokenTypes.AccessToken);
                await _accessTokenIssuer.IssueAccessTokenAsync(context);
            }

            if (context.RequestGrants.Tokens.Contains(TokenTypes.IdToken))
            {
                context.InitializeForToken(TokenTypes.IdToken);
                await _idTokenIssuer.IssueIdTokenAsync(context);
            }

            if (context.RequestGrants.Tokens.Contains(TokenTypes.RefreshToken))
            {
                context.InitializeForToken(TokenTypes.RefreshToken);
                await _refreshTokenIssuer.IssueRefreshTokenAsync(context);
            }
        }

        public async Task<AuthorizationGrant> ExchangeTokenAsync(OpenIdConnectMessage message)
        {
            switch (message.GrantType)
            {
                case OpenIdConnectGrantTypes.AuthorizationCode:
                    return await _codeIssuer.ExchangeAuthorizationCodeAsync(message);
                case OpenIdConnectGrantTypes.RefreshToken:
                    return await _refreshTokenIssuer.ExchangeRefreshTokenAsync(message);
                default:
                    return AuthorizationGrant.Invalid(_errorProvider.InvalidGrantType(message.GrantType));
            }
        }
    }
}
