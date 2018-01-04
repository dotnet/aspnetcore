// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class DefaultAuthorizationResponseParameterProviderTest
    {
        [Fact]
        public async Task AddParameters_AddsCodeToResponse_WhenCodeIsEmitted()
        {
            // Arrange
            var provider = new DefaultAuthorizationResponseParameterProvider(new TimeStampManager());
            var response = new AuthorizationResponse()
            {
                Message = new OpenIdConnectMessage(),
                RedirectUri = "http://www.example.com/callback",
                ResponseMode = "query"
            };

            var context = new TokenGeneratingContext(
                new ClaimsPrincipal(),
                new ClaimsPrincipal(),
                new OpenIdConnectMessage()
                {
                    State = "state"
                },
                new RequestGrants());

            context.InitializeForToken(TokenTypes.AuthorizationCode);
            context.AddToken(new TokenResult(new TestToken(TokenTypes.AuthorizationCode), "serialized_authorization_code"));

            // Act
            await provider.AddParameters(context, response);

            // Assert
            Assert.Equal("state", response.Message.State);
            Assert.Equal("serialized_authorization_code", response.Message.Code);
        }

        [Fact]
        public async Task AddParameters_AddsAccessTokenToResponse_WhenAccessTokenIsEmitted()
        {
            // Arrange
            var provider = new DefaultAuthorizationResponseParameterProvider(new TimeStampManager());
            var response = new AuthorizationResponse()
            {
                Message = new OpenIdConnectMessage(),
                RedirectUri = "http://www.example.com/callback",
                ResponseMode = "query"
            };

            var context = new TokenGeneratingContext(
                new ClaimsPrincipal(),
                new ClaimsPrincipal(),
                new OpenIdConnectMessage()
                {
                    State = "state"
                },
                new RequestGrants()
                {
                    Scopes = { ApplicationScope.OpenId, new ApplicationScope("resourceId", "read") }
                });

            context.InitializeForToken(TokenTypes.AccessToken);
            context.AddToken(new TokenResult(new TestToken(TokenTypes.AccessToken), "serialized_access_token"));

            // Act
            await provider.AddParameters(context, response);

            // Assert
            Assert.Equal("state", response.Message.State);
            Assert.Equal("serialized_access_token", response.Message.AccessToken);
            Assert.Equal("3600", response.Message.ExpiresIn);
            Assert.Equal("openid read", response.Message.Scope);
            Assert.Equal("Bearer", response.Message.TokenType);
        }

        [Fact]
        public async Task AddParameters_AddsIdTokenToResponse_WhenIdTokenIsEmitted()
        {
            // Arrange
            var provider = new DefaultAuthorizationResponseParameterProvider(new TimeStampManager());
            var response = new AuthorizationResponse()
            {
                Message = new OpenIdConnectMessage(),
                RedirectUri = "http://www.example.com/callback",
                ResponseMode = "query"
            };

            var context = new TokenGeneratingContext(
                new ClaimsPrincipal(),
                new ClaimsPrincipal(),
                new OpenIdConnectMessage()
                {
                    State = "state"
                },
                new RequestGrants());

            context.InitializeForToken(TokenTypes.IdToken);
            context.AddToken(new TokenResult(new TestToken(TokenTypes.IdToken), "serialized_id_token"));

            // Act
            await provider.AddParameters(context, response);

            // Assert
            Assert.Equal("state", response.Message.State);
            Assert.Equal("serialized_id_token", response.Message.IdToken);
        }

        public class TestToken : Token
        {
            private readonly string _kind;

            public TestToken(string kind)
                : base(new List<Claim>
                {
                    new Claim(IdentityServiceClaimTypes.TokenUniqueId,"tuid"),
                    new Claim(IdentityServiceClaimTypes.Expires,"946688400"),
                    new Claim(IdentityServiceClaimTypes.IssuedAt,"946684800"),
                    new Claim(IdentityServiceClaimTypes.NotBefore,"946684800"),
                })
            {
                _kind = kind;
            }

            public override string Kind => _kind;
        }
    }
}
