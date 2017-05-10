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
    public class ScopeTokenClaimsProviderTest
    {
        [Fact]
        public async Task OnGeneratingClaims_AddsAllScopesToAuthorizationCode()
        {
            // Arrange
            var applicationScopes = new List<ApplicationScope> { ApplicationScope.OpenId, new ApplicationScope("resourceId", "custom") };

            var context = new TokenGeneratingContext(
                new ClaimsPrincipal(),
                new ClaimsPrincipal(),
                new OpenIdConnectMessage()
                {
                    RequestType = OpenIdConnectRequestType.Authentication
                },
                new RequestGrants()
                {
                    Scopes = applicationScopes.ToList()
                });

            var claimsProvider = new ScopesTokenClaimsProvider();

            context.InitializeForToken(TokenTypes.AuthorizationCode);

            // Act
            await claimsProvider.OnGeneratingClaims(context);
            var claims = context.CurrentClaims;

            // Assert
            Assert.Single(claims, c => c.Type.Equals(IdentityServiceClaimTypes.Scope) && c.Value.Equals("openid custom"));
            Assert.Single(claims, c => c.Type.Equals(IdentityServiceClaimTypes.Resource) && c.Value.Equals("resourceId"));
        }

        [Fact]
        public async Task OnGeneratingClaims_AddsCustomScopesFromRequest_ToAccessTokenOnAuthorization()
        {
            // Arrange
            var applicationScopes = new List<ApplicationScope> { ApplicationScope.OpenId, new ApplicationScope("resourceId", "custom") };
            var expectedResourceValue = applicationScopes.FirstOrDefault(s => s.ClientId != null)?.ClientId;

            var context = new TokenGeneratingContext(
                new ClaimsPrincipal(),
                new ClaimsPrincipal(),
                new OpenIdConnectMessage()
                {
                    ClientId = "clientId"
                },
                new RequestGrants()
                {
                    Scopes = applicationScopes.ToList()
                });

            var claimsProvider = new ScopesTokenClaimsProvider();

            context.InitializeForToken(TokenTypes.AccessToken);

            // Act
            await claimsProvider.OnGeneratingClaims(context);
            var claims = context.CurrentClaims;

            // Assert
            Assert.Single(claims, c => c.Type.Equals(IdentityServiceClaimTypes.Scope) && c.Value.Equals("custom"));
            Assert.Single(claims, c => c.Type.Equals(IdentityServiceClaimTypes.Audience) && c.Value.Equals("resourceId"));
            Assert.Single(claims, c => c.Type.Equals(IdentityServiceClaimTypes.AuthorizedParty) && c.Value.Equals("clientId"));
        }

        [Fact]
        public async Task OnGeneratingClaims_AddsCustomScopesFromRequest_ToAccessTokenOnTokenRequest()
        {
            // Arrange
            var applicationScopes = new List<ApplicationScope> {
                ApplicationScope.OpenId,
                new ApplicationScope("resourceId", "custom"),
                new ApplicationScope("resourceId","custom2")
            };

            var expectedResourceValue = applicationScopes.FirstOrDefault(s => s.ClientId != null)?.ClientId;

            var context = new TokenGeneratingContext(
                new ClaimsPrincipal(),
                new ClaimsPrincipal(),
                new OpenIdConnectMessage()
                {
                    ClientId = "clientId"
                },
                new RequestGrants()
                {
                    Scopes = applicationScopes.ToList(),
                    // This is just to prove that we always pick the values for the scope related claims
                    // from the set of granted scopes (for access tokens).
                    Claims = new List<Claim>
                    {
                        new Claim(IdentityServiceClaimTypes.Resource, "ridClaim"),
                        new Claim(IdentityServiceClaimTypes.Scope, "custom3")
                    }
                });

            var claimsProvider = new ScopesTokenClaimsProvider();

            context.InitializeForToken(TokenTypes.AccessToken);

            // Act
            await claimsProvider.OnGeneratingClaims(context);
            var claims = context.CurrentClaims;

            // Assert
            Assert.Single(claims, c => c.Type.Equals(IdentityServiceClaimTypes.Scope) && c.Value.Equals("custom custom2"));
            Assert.Single(claims, c => c.Type.Equals(IdentityServiceClaimTypes.Audience) && c.Value.Equals("resourceId"));
            Assert.Single(claims, c => c.Type.Equals(IdentityServiceClaimTypes.AuthorizedParty) && c.Value.Equals("clientId"));
        }

        [Fact]
        public async Task OnGeneratingClaims_AddsAllScopesFromGrantClaims_ToRefreshTokenOnTokenRequest()
        {
            // Arrange
            // This is just to prove that we always transfer the scope and resource claims from the grant
            // into the refresh token untouched.
            var applicationScopes = new List<ApplicationScope>
            {
                ApplicationScope.OpenId,
                new ApplicationScope("resourceId", "custom"),
                new ApplicationScope("resourceId","custom2")
            };

            var expectedResourceValue = applicationScopes.FirstOrDefault(s => s.ClientId != null)?.ClientId;

            var context = new TokenGeneratingContext(
                new ClaimsPrincipal(),
                new ClaimsPrincipal(),
                new OpenIdConnectMessage()
                {
                    ClientId = "clientId"
                },
                new RequestGrants()
                {
                    Scopes = applicationScopes.ToList(),
                    Claims = new List<Claim>
                    {
                        new Claim(IdentityServiceClaimTypes.Resource, "ridClaim"),
                        new Claim(IdentityServiceClaimTypes.Scope, "openid custom3")
                    }
                });

            var claimsProvider = new ScopesTokenClaimsProvider();

            context.InitializeForToken(TokenTypes.RefreshToken);

            // Act
            await claimsProvider.OnGeneratingClaims(context);
            var claims = context.CurrentClaims;

            // Assert
            Assert.Single(claims, c => c.Type.Equals(IdentityServiceClaimTypes.Scope) && c.Value.Equals("openid custom3"));
            Assert.Single(claims, c => c.Type.Equals(IdentityServiceClaimTypes.Resource) && c.Value.Equals("ridClaim"));
        }

        [Fact]
        public async Task OnGeneratingClaims_DoesNotAddResourceClaim_ToRefreshTokenIfNotPresent()
        {
            // Arrange
            // This is just to prove that we always transfer the scope and resource claims from the grant
            // into the refresh token untouched.
            var applicationScopes = new List<ApplicationScope>
            {
                ApplicationScope.OpenId,
                new ApplicationScope("resourceId", "custom"),
                new ApplicationScope("resourceId","custom2")
            };

            var expectedResourceValue = applicationScopes.FirstOrDefault(s => s.ClientId != null)?.ClientId;

            var context = new TokenGeneratingContext(
                new ClaimsPrincipal(),
                new ClaimsPrincipal(),
                new OpenIdConnectMessage()
                {
                    ClientId = "clientId"
                },
                new RequestGrants()
                {
                    Scopes = applicationScopes.ToList(),
                    Claims = new List<Claim>
                    {
                        new Claim(IdentityServiceClaimTypes.Scope, "openid")
                    }
                });

            var claimsProvider = new ScopesTokenClaimsProvider();

            context.InitializeForToken(TokenTypes.RefreshToken);

            // Act
            await claimsProvider.OnGeneratingClaims(context);
            var claims = context.CurrentClaims;

            // Assert
            Assert.Single(claims, c => c.Type.Equals(IdentityServiceClaimTypes.Scope) && c.Value.Equals("openid"));
        }
    }
}
