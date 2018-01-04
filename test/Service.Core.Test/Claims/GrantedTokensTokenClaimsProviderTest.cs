// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Service.Claims
{
    public class GrantedTokensTokenClaimsProviderTest
    {
        [Theory]
        [InlineData("openid", "id_token")]
        [InlineData("offline_access", "refresh_token")]
        [InlineData("custom", "access_token")]
        [InlineData("openid offline_access", "id_token refresh_token")]
        [InlineData("openid custom", "id_token access_token")]
        [InlineData("offline_access custom", "refresh_token access_token")]
        [InlineData("openid offline_access custom", "id_token refresh_token access_token")]
        public async Task OnGeneratingClaims_AddsGrantedTokensForAuthorizationCode(
            string scopes,
            string tokens)
        {
            // Arrange
            var context = new TokenGeneratingContext(
                new ClaimsPrincipal(),
                new ClaimsPrincipal(),
                new OpenIdConnectMessage(),
                new RequestGrants()
                {
                    Scopes = scopes.Split(' ').Select(CreateScope).ToList()
                });

            var expectedTokens = tokens.Split(' ').OrderBy(t => t).ToArray();

            var claimsProvider = new GrantedTokensTokenClaimsProvider();

            context.InitializeForToken(TokenTypes.AuthorizationCode);

            // Act
            await claimsProvider.OnGeneratingClaims(context);
            var granted = context.CurrentClaims
                .Where(c => c.Type.Equals(IdentityServiceClaimTypes.GrantedToken))
                .OrderBy(c => c.Value)
                .Select(c => c.Value)
                .ToArray();

            // Assert
            Assert.Equal(expectedTokens, granted);
        }

        [Fact]
        public async Task OnGeneratingClaims_AddsGrantedTokensForRefreshToken()
        {
            // Arrange
            var context = new TokenGeneratingContext(
                new ClaimsPrincipal(),
                new ClaimsPrincipal(),
                new OpenIdConnectMessage(),
                new RequestGrants()
                {
                    Tokens = new List<string>
                    {
                        TokenTypes.AccessToken,
                        TokenTypes.IdToken,
                        TokenTypes.RefreshToken
                    }
                });

            var expectedTokens = new[]
            {
                TokenTypes.AccessToken,
                TokenTypes.IdToken,
                TokenTypes.RefreshToken
            };

            var claimsProvider = new GrantedTokensTokenClaimsProvider();

            context.InitializeForToken(TokenTypes.RefreshToken);

            // Act
            await claimsProvider.OnGeneratingClaims(context);
            var granted = context.CurrentClaims
                .Where(c => c.Type.Equals(IdentityServiceClaimTypes.GrantedToken))
                .OrderBy(c => c.Value)
                .Select(c => c.Value)
                .ToArray();

            // Assert
            Assert.Equal(expectedTokens, granted);
        }

        private static ApplicationScope CreateScope(string scope)
        {
            return ApplicationScope.CanonicalScopes.TryGetValue(scope, out var canonical) ? canonical : new ApplicationScope("clientId", scope);
        }
    }
}
