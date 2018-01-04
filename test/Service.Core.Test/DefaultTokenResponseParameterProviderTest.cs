// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class DefaultTokenResponseParameterProviderTest
    {
        [Fact]
        public async Task AddParameters_AddsIdTokenToResponse_WhenEmitted()
        {
            // Arrange
            var provider = new DefaultTokenResponseParameterProvider(new TimeStampManager());
            var response = new OpenIdConnectMessage();

            var context = new TokenGeneratingContext(
                new ClaimsPrincipal(),
                new ClaimsPrincipal(),
                new OpenIdConnectMessage(),
                new RequestGrants());

            context.InitializeForToken(TokenTypes.IdToken);
            context.AddToken(new TokenResult(new TestToken(TokenTypes.IdToken), "serialized_id_token"));

            // Act
            await provider.AddParameters(context, response);

            // Assert
            Assert.Equal("serialized_id_token", response.IdToken);
            Assert.True(response.Parameters.ContainsKey("id_token_expires_in"));
            Assert.Equal("3600", response.Parameters["id_token_expires_in"]);
            Assert.Equal("Bearer", response.TokenType);
        }

        [Fact]
        public async Task AddParameters_AddsAccessTokenToResponse_WhenAccessTokenIsEmitted()
        {
            // Arrange
            var provider = new DefaultTokenResponseParameterProvider(new TimeStampManager());
            var response = new OpenIdConnectMessage();

            var context = new TokenGeneratingContext(
                new ClaimsPrincipal(),
                new ClaimsPrincipal(),
                new OpenIdConnectMessage(),
                new RequestGrants() {
                    Scopes = new[] { ApplicationScope.OpenId, new ApplicationScope("resourceId", "read") }
                });

            context.InitializeForToken(TokenTypes.AccessToken);
            context.AddToken(new TokenResult(new TestToken(TokenTypes.AccessToken), "serialized_access_token"));

            // Act
            await provider.AddParameters(context, response);

            // Assert
            Assert.Equal("serialized_access_token", response.AccessToken);
            Assert.Equal("3600", response.ExpiresIn);
            Assert.True(response.Parameters.ContainsKey("expires_on"));
            Assert.Equal("946688400", response.Parameters["expires_on"]);
            Assert.True(response.Parameters.ContainsKey("not_before"));
            Assert.Equal("946684800", response.Parameters["not_before"]);
            Assert.Equal("resourceId", response.Resource);
            Assert.Equal("Bearer", response.TokenType);
        }

        [Fact]
        public async Task AddParameters_AddsRefreshTokenToResponse_WhenRefreshTokenIsEmitted()
        {
            // Arrange
            var provider = new DefaultTokenResponseParameterProvider(new TimeStampManager());
            var response = new OpenIdConnectMessage();

            var context = new TokenGeneratingContext(
                new ClaimsPrincipal(),
                new ClaimsPrincipal(),
                new OpenIdConnectMessage(),
                new RequestGrants());

            context.InitializeForToken(TokenTypes.RefreshToken);
            context.AddToken(new TokenResult(new TestToken(TokenTypes.RefreshToken), "serialized_refresh_token"));

            // Act
            await provider.AddParameters(context, response);

            // Assert
            Assert.Equal("serialized_refresh_token", response.RefreshToken);
            Assert.True(response.Parameters.ContainsKey("refresh_token_expires_in"));
            Assert.Equal("3600", response.Parameters["refresh_token_expires_in"]);
            Assert.Equal("Bearer", response.TokenType);
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
