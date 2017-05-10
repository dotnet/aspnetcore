// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Service.Claims
{
    public class DefaultTokenClaimsProviderTest
    {
        [Theory]
        [InlineData(TokenTypes.AuthorizationCode)]
        [InlineData(TokenTypes.AccessToken)]
        [InlineData(TokenTypes.IdToken)]
        [InlineData(TokenTypes.RefreshToken)]
        public async Task OnGeneratingClaims_AddsTokenIdForAllTokens(string tokenType)
        {
            // Arrange
            var context = new TokenGeneratingContext(
                new ClaimsPrincipal(),
                new ClaimsPrincipal(),
                new OpenIdConnectMessage(),
                new RequestGrants());

            var options = new IdentityServiceOptions()
            {
                Issuer = "http://www.example.com/Identity"
            };
            var claimsProvider = new DefaultTokenClaimsProvider(Options.Create(options));

            context.InitializeForToken(tokenType);

            // Act
            await claimsProvider.OnGeneratingClaims(context);

            // Assert
            Assert.Single(
                context.CurrentClaims,
                c => c.Type.Equals(IdentityServiceClaimTypes.TokenUniqueId, StringComparison.Ordinal));
        }

        [Theory]
        [InlineData(TokenTypes.AccessToken)]
        [InlineData(TokenTypes.IdToken)]
        public async Task OnGeneratingClaims_AddsIssuerForAccessTokenAndIdToken(string tokenType)
        {
            // Arrange
            var context = new TokenGeneratingContext(
                new ClaimsPrincipal(),
                new ClaimsPrincipal(),
                new OpenIdConnectMessage(),
                new RequestGrants());

            var options = new IdentityServiceOptions()
            {
                Issuer = "http://www.example.com/Identity"
            };
            var claimsProvider = new DefaultTokenClaimsProvider(Options.Create(options));

            context.InitializeForToken(tokenType);

            // Act
            await claimsProvider.OnGeneratingClaims(context);

            // Assert
            Assert.Single(
                context.CurrentClaims,
                c => c.Type.Equals(IdentityServiceClaimTypes.Issuer, StringComparison.Ordinal));
        }

        [Fact]
        public async Task OnGeneratingClaims_AddsRedirectUriIfPresentOnTheRequest()
        {
            // Arrange
            var expectedRedirectUri = "http://wwww.example.com/callback";
            var context = new TokenGeneratingContext(
                new ClaimsPrincipal(),
                new ClaimsPrincipal(),
                new OpenIdConnectMessage() { RedirectUri = expectedRedirectUri },
                new RequestGrants());

            var options = new IdentityServiceOptions()
            {
                Issuer = "http://www.example.com/Identity"
            };
            var claimsProvider = new DefaultTokenClaimsProvider(Options.Create(options));

            context.InitializeForToken(TokenTypes.AuthorizationCode);

            // Act
            await claimsProvider.OnGeneratingClaims(context);

            // Assert
            Assert.Single(
                context.CurrentClaims,
                c => c.Type.Equals(IdentityServiceClaimTypes.RedirectUri, StringComparison.Ordinal) &&
                     c.Value.Equals(expectedRedirectUri));
        }

        [Theory]
        [InlineData(TokenTypes.AuthorizationCode)]
        [InlineData(TokenTypes.AccessToken)]
        [InlineData(TokenTypes.IdToken)]
        [InlineData(TokenTypes.RefreshToken)]
        public async Task OnGeneratingClaims_MapsClaimsFromUsersApplicationsAndAmbient(string tokenType)
        {
            // Arrange
            var expectedClaims = new List<Claim>
            {
                new Claim("user-single","us"),
                new Claim("user-single-claim","usa"),
                new Claim("user-multiple","um1"),
                new Claim("user-multiple","um2"),
                new Claim("user-multiple-claim","uma1"),
                new Claim("user-multiple-claim","uma2"),
                new Claim("application-single","as"),
                new Claim("application-single-claim","asa"),
                new Claim("application-multiple","am1"),
                new Claim("application-multiple","am2"),
                new Claim("application-multiple-claim","ama1"),
                new Claim("application-multiple-claim","ama2"),
                new Claim("context-single", "cs"),
                new Claim("context-single-claim", "csa"),
                new Claim("context-multiple", "cm1"),
                new Claim("context-multiple", "cm2"),
                new Claim("context-multiple-claim", "cma1"),
                new Claim("context-multiple-claim", "cma2"),
            };

            var user = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim("user-single","us"),
                new Claim("user-single-aliased","usa"),
                new Claim("user-multiple","um1"),
                new Claim("user-multiple","um2"),
                new Claim("user-multiple-aliased","uma1"),
                new Claim("user-multiple-aliased","uma2"),
            }));

            var application = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim("application-single","as"),
                new Claim("application-single-aliased","asa"),
                new Claim("application-multiple","am1"),
                new Claim("application-multiple","am2"),
                new Claim("application-multiple-aliased","ama1"),
                new Claim("application-multiple-aliased","ama2"),
            }));

            var context = new TokenGeneratingContext(
                user,
                application,
                new OpenIdConnectMessage(),
                new RequestGrants());

            context.AmbientClaims.Add(new Claim("context-single", "cs"));
            context.AmbientClaims.Add(new Claim("context-single-aliased", "csa"));
            context.AmbientClaims.Add(new Claim("context-multiple", "cm1"));
            context.AmbientClaims.Add(new Claim("context-multiple", "cm2"));
            context.AmbientClaims.Add(new Claim("context-multiple-aliased", "cma1"));
            context.AmbientClaims.Add(new Claim("context-multiple-aliased", "cma2"));

            var options = new IdentityServiceOptions()
            {
                Issuer = "http://www.example.com/Identity"
            };

            CreateTestMapping(options.AuthorizationCodeOptions);
            CreateTestMapping(options.AccessTokenOptions);
            CreateTestMapping(options.IdTokenOptions);
            CreateTestMapping(options.RefreshTokenOptions);

            var claimsProvider = new DefaultTokenClaimsProvider(Options.Create(options));

            context.InitializeForToken(tokenType);

            // Act
            await claimsProvider.OnGeneratingClaims(context);
            var claims = context.CurrentClaims.Where(c => 
                c.Type != IdentityServiceClaimTypes.Issuer &&
                c.Type != IdentityServiceClaimTypes.TokenUniqueId).ToList();

            // Assert
            Assert.Equal(expectedClaims.Count, claims.Count);
            foreach (var claim in expectedClaims)
            {
                Assert.Contains(claims, c => c.Type.Equals(claim.Type) && c.Value.Equals(claim.Value));
            }
        }

        private void CreateTestMapping(TokenOptions tokenOptions)
        {
            tokenOptions.ContextClaims.AddSingle("context-single");
            tokenOptions.ContextClaims.AddSingle("context-single-claim", "context-single-aliased");
            tokenOptions.ContextClaims.Add(new TokenValueDescriptor("context-multiple", TokenValueCardinality.Many));
            tokenOptions.ContextClaims.Add(new TokenValueDescriptor("context-multiple-claim", "context-multiple-aliased", TokenValueCardinality.Many));
            tokenOptions.UserClaims.AddSingle("user-single");
            tokenOptions.UserClaims.AddSingle("user-single-claim", "user-single-aliased");
            tokenOptions.UserClaims.Add(new TokenValueDescriptor("user-multiple", TokenValueCardinality.Many));
            tokenOptions.UserClaims.Add(new TokenValueDescriptor("user-multiple-claim", "user-multiple-aliased", TokenValueCardinality.Many));
            tokenOptions.ApplicationClaims.AddSingle("application-single");
            tokenOptions.ApplicationClaims.AddSingle("application-single-claim", "application-single-aliased");
            tokenOptions.ApplicationClaims.Add(new TokenValueDescriptor("application-multiple", TokenValueCardinality.Many));
            tokenOptions.ApplicationClaims.Add(new TokenValueDescriptor("application-multiple-claim", "application-multiple-aliased", TokenValueCardinality.Many));
        }
    }
}
