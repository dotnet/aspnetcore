// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Service.Core.Claims
{
    public class ProofOfKeyForCodeExchangeTokenClaimsProviderTest
    {
        [Fact]
        public async Task OnGeneratingClaims_AddsCodeChallengeAndChallengeMethod_ToTheAuthorizationCode()
        {
            // Arrange
            var context = new TokenGeneratingContext(
                new ClaimsPrincipal(),
                new ClaimsPrincipal(),
                new OpenIdConnectMessage(new Dictionary<string, string[]>
                {
                    [ProofOfKeyForCodeExchangeParameterNames.CodeChallenge] = new[] { "challenge" },
                    [ProofOfKeyForCodeExchangeParameterNames.CodeChallengeMethod] = new[] { "S256" },
                }),
                new RequestGrants());

            context.InitializeForToken(TokenTypes.AuthorizationCode);

            var provider = new ProofOfKeyForCodeExchangeTokenClaimsProvider();

            // Act
            await provider.OnGeneratingClaims(context);

            // Assert
            Assert.Contains(context.CurrentClaims, c => c.Type == IdentityServiceClaimTypes.CodeChallenge && c.Value == "challenge");
            Assert.Contains(context.CurrentClaims, c => c.Type == IdentityServiceClaimTypes.CodeChallengeMethod && c.Value == "S256");
        }

        [Theory]
        [InlineData(TokenTypes.AccessToken)]
        [InlineData(TokenTypes.IdToken)]
        [InlineData(TokenTypes.RefreshToken)]
        public async Task OnGeneratingClaims_DoesNothing_ForOtherTokenTypes(string tokenType)
        {
            // Arrange
            var context = new TokenGeneratingContext(
                new ClaimsPrincipal(),
                new ClaimsPrincipal(),
                new OpenIdConnectMessage(new Dictionary<string, string[]>
                {
                    [ProofOfKeyForCodeExchangeParameterNames.CodeChallenge] = new[] { "challenge" },
                    [ProofOfKeyForCodeExchangeParameterNames.CodeChallengeMethod] = new[] { "S256" },
                }),
                new RequestGrants());

            context.InitializeForToken(tokenType);

            var provider = new ProofOfKeyForCodeExchangeTokenClaimsProvider();

            // Act
            await provider.OnGeneratingClaims(context);

            // Assert
            Assert.Empty(context.CurrentClaims);
        }

        [Fact]
        public async Task OnGeneratingClaims_DoesNothing_IfChallengeNotPresent()
        {
            // Arrange
            var context = new TokenGeneratingContext(
                new ClaimsPrincipal(),
                new ClaimsPrincipal(),
                new OpenIdConnectMessage(new Dictionary<string, string[]>
                {
                    [ProofOfKeyForCodeExchangeParameterNames.CodeChallengeMethod] = new[] { "S256" },
                }),
                new RequestGrants());

            context.InitializeForToken(TokenTypes.AuthorizationCode);

            var provider = new ProofOfKeyForCodeExchangeTokenClaimsProvider();

            // Act
            await provider.OnGeneratingClaims(context);

            // Assert
            Assert.Empty(context.CurrentClaims);
        }
    }
}
