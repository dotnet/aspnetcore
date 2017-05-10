// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.Service;
using Microsoft.AspNetCore.Identity.Service.Claims;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Claims
{
    public class NonceTokenClaimsProviderTest
    {
        [Theory]
        [InlineData(TokenTypes.IdToken)]
        [InlineData(TokenTypes.AccessToken)]
        [InlineData(TokenTypes.AuthorizationCode)]
        public async Task OnGeneratingClaims_AddsNonceToCodeAccessAndIdToken_WhenPresentInTheRequest(string tokenType)
        {
            // Arrange
            var context = new TokenGeneratingContext(
                new ClaimsPrincipal(),
                new ClaimsPrincipal(),
                new OpenIdConnectMessage()
                {
                    Nonce = "nonce-value",
                    RequestType = OpenIdConnectRequestType.Authentication
                },
                new RequestGrants()
                {
                    Claims = new List<Claim> { new Claim(IdentityServiceClaimTypes.Nonce, "invalid-nonce") }
                });

            var claimsProvider = new NonceTokenClaimsProvider();

            context.InitializeForToken(tokenType);

            // Act
            await claimsProvider.OnGeneratingClaims(context);
            var claims = context.CurrentClaims;

            // Assert
            Assert.Single(claims, c => c.Type.Equals(IdentityServiceClaimTypes.Nonce) && c.Value.Equals("nonce-value"));
        }

        [Theory]
        [InlineData(TokenTypes.IdToken)]
        [InlineData(TokenTypes.AccessToken)]
        [InlineData(TokenTypes.AuthorizationCode)]
        public async Task OnGeneratingClaims_DoesNotAddNonce_WhenNotPresentInTheRequest(string tokenType)
        {
            // Arrange
            var context = new TokenGeneratingContext(
                new ClaimsPrincipal(),
                new ClaimsPrincipal(),
                new OpenIdConnectMessage()
                {
                    RequestType = OpenIdConnectRequestType.Authentication
                },
                new RequestGrants()
                {
                    // Makes sure we don't add the nonce in an authorization request
                    // even if for some reason ends up in the claims grant (which is not
                    // used in authentication).
                    Claims = new List<Claim> { new Claim(IdentityServiceClaimTypes.Nonce, "nonce-value") }
                });

            var claimsProvider = new NonceTokenClaimsProvider();

            context.InitializeForToken(tokenType);

            // Act
            await claimsProvider.OnGeneratingClaims(context);
            var claims = context.CurrentClaims;

            // Assert
            Assert.DoesNotContain(claims, c => c.Type.Equals(IdentityServiceClaimTypes.Nonce));
        }

        [Theory]
        [InlineData(TokenTypes.IdToken)]
        [InlineData(TokenTypes.AccessToken)]
        [InlineData(TokenTypes.AuthorizationCode)]
        public async Task OnGeneratingClaims_AddsNonce_WhenPresentInTheGrantClaimsOfATokenRequest(string tokenType)
        {
            // Arrange
            var context = new TokenGeneratingContext(
                new ClaimsPrincipal(),
                new ClaimsPrincipal(),
                new OpenIdConnectMessage()
                {
                    RequestType = OpenIdConnectRequestType.Token,
                    // Makes sure we ignore the value from the request
                    // for non authorization requests even when its present.
                    Nonce = "invalid-value"
                },
                new RequestGrants()
                {
                    Claims = new List<Claim> { new Claim(IdentityServiceClaimTypes.Nonce, "nonce-value") }
                });

            var claimsProvider = new NonceTokenClaimsProvider();

            context.InitializeForToken(tokenType);

            // Act
            await claimsProvider.OnGeneratingClaims(context);
            var claims = context.CurrentClaims;

            // Assert
            Assert.Single(claims, c => c.Type.Equals(IdentityServiceClaimTypes.Nonce) && c.Value.Equals("nonce-value"));
        }

        [Theory]
        [InlineData(TokenTypes.IdToken)]
        [InlineData(TokenTypes.AccessToken)]
        [InlineData(TokenTypes.AuthorizationCode)]
        public async Task OnGeneratingClaims_DoesNotAddNonce_WhenNotPresentInTheGrantClaimsOfATokenRequest(string tokenType)
        {
            // Arrange
            var context = new TokenGeneratingContext(
                new ClaimsPrincipal(),
                new ClaimsPrincipal(),
                new OpenIdConnectMessage()
                {
                    RequestType = OpenIdConnectRequestType.Token,
                    // Makes sure we ignore the value from the request
                    // for non authorization requests even when its present.
                    Nonce = "invalid-value"
                },
                new RequestGrants());

            var claimsProvider = new NonceTokenClaimsProvider();

            context.InitializeForToken(tokenType);

            // Act
            await claimsProvider.OnGeneratingClaims(context);
            var claims = context.CurrentClaims;

            // Assert
            Assert.DoesNotContain(claims, c => c.Type.Equals(IdentityServiceClaimTypes.Nonce));
        }
    }
}
