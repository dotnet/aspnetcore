// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Identity.Service.Claims;
using Microsoft.AspNetCore.Identity.Service.Core;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class JtwAccessTokenIssuerTest
    {
        [Fact]
        public async Task JwtAccessTokenIssuer_Fails_IfUserIsMissingUserId()
        {
            // Arrange
            var options = GetOptions();
            var issuer = new JwtAccessTokenIssuer(
                GetClaimsManager(),
                GetSigningPolicy(options, new TimeStampManager()), new JwtSecurityTokenHandler(), options);
            var context = GetTokenGenerationContext();

            context.InitializeForToken(TokenTypes.AccessToken);

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => issuer.IssueAccessTokenAsync(context));

            // Assert
            Assert.Equal($"Missing '{ClaimTypes.NameIdentifier}' claim from the user.", exception.Message);
        }

        [Fact]
        public async Task JwtAccessTokenIssuer_SignsAccessToken()
        {
            // Arrange
            var expectedDateTime = new DateTimeOffset(2000, 01, 01, 0, 0, 0, TimeSpan.FromHours(1));
            var now = DateTimeOffset.UtcNow;
            var expires = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, TimeSpan.Zero);
            var timeManager = GetTimeManager(expectedDateTime, expires, expectedDateTime);

            var options = GetOptions();

            var handler = new JwtSecurityTokenHandler();

            var tokenValidationParameters = new TokenValidationParameters
            {
                IssuerSigningKey = options.Value.SigningKeys[0].Key,
                ValidAudiences = new[] { "resourceId" },
                ValidIssuers = new[] { options.Value.Issuer }
            };

            var issuer = new JwtAccessTokenIssuer(
                GetClaimsManager(timeManager),
                GetSigningPolicy(options,timeManager),
                new JwtSecurityTokenHandler(), options);
            var context = GetTokenGenerationContext(
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "user") })),
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(IdentityServiceClaimTypes.ClientId, "clientId") })));

            context.InitializeForToken(TokenTypes.AccessToken);

            // Act
            await issuer.IssueAccessTokenAsync(context);

            // Assert
            Assert.NotNull(context.AccessToken);
            Assert.NotNull(context.AccessToken.SerializedValue);

            SecurityToken validatedToken;
            Assert.NotNull(handler.ValidateToken(context.AccessToken.SerializedValue, tokenValidationParameters, out validatedToken));
            Assert.NotNull(validatedToken);

            var jwtToken = Assert.IsType<JwtSecurityToken>(validatedToken);
            var accessToken = Assert.IsType<AccessToken>(context.AccessToken.Token);
            Assert.Equal("http://www.example.com/issuer", jwtToken.Issuer);
            var tokenAudience = Assert.Single(jwtToken.Audiences);
            Assert.Equal("resourceId", tokenAudience);
            var tokenAuthorizedParty = Assert.Single(jwtToken.Claims, c=> c.Type.Equals("azp")).Value;
            Assert.Equal("clientId", tokenAuthorizedParty);
            Assert.Equal("user", jwtToken.Subject);

            Assert.Equal(expires, jwtToken.ValidTo);
            Assert.Equal(expectedDateTime.UtcDateTime, jwtToken.ValidFrom);

            var tokenScopes = jwtToken.Claims
                .Where(c => c.Type == IdentityServiceClaimTypes.Scope)
                .Select(c => c.Value).OrderBy(c => c)
                .ToArray();

            Assert.Equal(new[] { "all" }, tokenScopes);
        }

        [Fact]
        public async Task JwtAccessTokenIssuer_IncludesAllRequiredData()
        {
            // Arrange
            var options = GetOptions();

            var expectedDateTime = new DateTimeOffset(2000, 01, 01, 0, 0, 0, TimeSpan.FromHours(1));
            var timeManager = GetTimeManager(expectedDateTime, expectedDateTime.AddHours(1), expectedDateTime);
            var issuer = new JwtAccessTokenIssuer(
                GetClaimsManager(timeManager),
                GetSigningPolicy(options,timeManager),
                new JwtSecurityTokenHandler(), options);
            var context = GetTokenGenerationContext(
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "user") })),
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(IdentityServiceClaimTypes.ClientId, "clientId") })));

            context.InitializeForToken(TokenTypes.AccessToken);

            // Act
            await issuer.IssueAccessTokenAsync(context);

            // Assert
            Assert.NotNull(context.AccessToken);
            var accessToken = Assert.IsType<AccessToken>(context.AccessToken.Token);
            Assert.NotNull(accessToken);
            Assert.NotNull(accessToken.Id);
            Assert.Equal("user", accessToken.Subject);
            Assert.Equal("resourceId", accessToken.Audience);
            Assert.Equal("clientId", accessToken.AuthorizedParty);
            Assert.Equal(new[] { "all" }, accessToken.Scopes);
            Assert.Equal(expectedDateTime, accessToken.IssuedAt);
            Assert.Equal(expectedDateTime.AddHours(1), accessToken.Expires);
            Assert.Equal(expectedDateTime, accessToken.NotBefore);
        }

        private TokenGeneratingContext GetTokenGenerationContext(
            ClaimsPrincipal user = null,
            ClaimsPrincipal application = null) =>
            new TokenGeneratingContext(
                user ?? new ClaimsPrincipal(new ClaimsIdentity()),
                application ?? new ClaimsPrincipal(new ClaimsIdentity()),
                new OpenIdConnectMessage
                {
                    Code = "code",
                    ClientId = "clientId",
                    Scope = "openid profile https://www.example.com/ResourceApp/all",
                    Nonce = null,
                    RedirectUri = "http://www.example.com/callback"
                },
                new RequestGrants
                {
                    RedirectUri = "http://www.example.com/callback",
                    Scopes = new[] { ApplicationScope.OpenId, ApplicationScope.Profile, new ApplicationScope("resourceId", "all") },
                    Tokens = new[] { TokenTypes.AuthorizationCode }
                });

        private ITimeStampManager GetTimeManager(
            DateTimeOffset? issuedAt = null,
            DateTimeOffset? expires = null,
            DateTimeOffset? notBefore = null)
        {
            issuedAt = issuedAt ?? DateTimeOffset.Now;
            expires = expires ?? DateTimeOffset.Now;
            notBefore = notBefore ?? DateTimeOffset.Now;

            var manager = new Mock<ITimeStampManager>();

            manager.Setup(m => m.GetCurrentTimeStampInEpochTime())
                .Returns(issuedAt.Value.ToUnixTimeSeconds().ToString());

            manager.SetupSequence(t => t.GetTimeStampInEpochTime(It.IsAny<TimeSpan>()))
                .Returns(notBefore.Value.ToUnixTimeSeconds().ToString())
                .Returns(expires.Value.ToUnixTimeSeconds().ToString());

            manager.Setup(m => m.IsValidPeriod(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
                .Returns(true);

            return manager.Object;
        }

        private IOptions<IdentityServiceOptions> GetOptions()
        {
            var IdentityServiceOptions = new IdentityServiceOptions()
            {
                Issuer = "http://www.example.com/issuer"
            };
            IdentityServiceOptions.SigningKeys.Add(new SigningCredentials(CryptoUtilities.CreateTestKey(), "RS256"));

            var optionsSetup = new IdentityServiceOptionsDefaultSetup();
            optionsSetup.Configure(IdentityServiceOptions);

            var mock = new Mock<IOptions<IdentityServiceOptions>>();
            mock.Setup(m => m.Value).Returns(IdentityServiceOptions);

            return mock.Object;
        }

        private ITokenClaimsManager GetClaimsManager(
            ITimeStampManager timeManager = null)
        {
            var options = GetOptions();
            return new DefaultTokenClaimsManager(
                new List<ITokenClaimsProvider>{
                    new DefaultTokenClaimsProvider(options),
                    new GrantedTokensTokenClaimsProvider(),
                    new NonceTokenClaimsProvider(),
                    new ScopesTokenClaimsProvider(),
                    new TimestampsTokenClaimsProvider(timeManager ?? new TimeStampManager(),options),
                    new TokenHashTokenClaimsProvider(new TokenHasher())
                });
        }

        private ISigningCredentialsPolicyProvider GetSigningPolicy(
            IOptions<IdentityServiceOptions> options,
            ITimeStampManager timeStampManager)
        {
            var mock = new Mock<IOptionsSnapshot<IdentityServiceOptions>>();
            mock.Setup(m => m.Value).Returns(options.Value);
            mock.Setup(m => m.Get(It.IsAny<string>())).Returns(options.Value);

            return new DefaultSigningCredentialsPolicyProvider(
                new List<ISigningCredentialsSource> {
                            new DefaultSigningCredentialsSource(mock.Object, timeStampManager)
                },
                timeStampManager,
                new HostingEnvironment());
        }
    }
}
