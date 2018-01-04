// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class TokenGeneratingContextTest
    {
        [Theory]
        [InlineData("code", "code")]
        [InlineData("token", "access_token")]
        [InlineData("id_token", "id_token")]
        [InlineData("code token", "code access_token")]
        [InlineData("code id_token", "code id_token")]
        [InlineData("token id_token", "access_token id_token")]
        [InlineData("code token id_token", "code access_token id_token")]
        public void CreateTokenGenerationContext_CorrectlyCreatesContext_FromTheAuthorizationRequest(
            string responseTypes,
            string expectedRequestedTokens)
        {
            // Arrange
            var authorizationRequest = new Dictionary<string, string[]>
            {
                [OpenIdConnectParameterNames.ClientId] = new[] { "clientId" },
                [OpenIdConnectParameterNames.RedirectUri] = new[] { "http://localhost:123/callback" },
                [OpenIdConnectParameterNames.ResponseType] = new[] { responseTypes },
                [OpenIdConnectParameterNames.ResponseMode] = new[] { OpenIdConnectResponseMode.FormPost },
                [OpenIdConnectParameterNames.Scope] = new[] { "openid" },
                [OpenIdConnectParameterNames.Nonce] = new[] { "asdf" },
                [OpenIdConnectParameterNames.State] = new[] { "state" }
            };
            var message = new OpenIdConnectMessage(authorizationRequest);

            var requestGrants = new RequestGrants
            {
                RedirectUri = "http://localhost:123/callback",
                ResponseMode = OpenIdConnectResponseMode.FormPost,
                Scopes = new List<ApplicationScope> { ApplicationScope.OpenId },
                Tokens = expectedRequestedTokens.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
            };

            var request = AuthorizationRequest.Valid(message, requestGrants);

            var user = new ClaimsPrincipal();
            var application = new ClaimsPrincipal();

            // Act
            var context = request.CreateTokenGeneratingContext(user, application);

            // Assert
            Assert.NotNull(context);
            Assert.Equal(expectedRequestedTokens.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), context.RequestGrants.Tokens);
            Assert.Equal("http://localhost:123/callback", context.RequestParameters.RedirectUri);
            Assert.Equal("openid", context.RequestParameters.Scope);
            Assert.Equal("asdf", context.RequestParameters.Nonce);
        }
    }
}
