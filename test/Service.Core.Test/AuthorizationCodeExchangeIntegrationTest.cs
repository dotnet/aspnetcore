// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Service.Claims;
using Microsoft.AspNetCore.Identity.Service.Core;
using Microsoft.AspNetCore.Identity.Service.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class AuthorizationCodeExchangeIntegrationTest
    {
        [Fact]
        public async Task ValidAuthorizationCode_ProducesAccessTokenIdTokenAndRefreshToken()
        {
            // Arrange
            var tokenManager = GetTokenManager();

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = await CreateAuthorizationCode(tokenManager),
                ["client_id"] = "s6BhdRkqt",
                ["redirect_uri"] = "https://client.example.org/cb",
                ["scope"] = "openid offline_access"
            });

            var factory = CreateRequestFactory(tokenManager);

            var user = CreateUser("user");
            var application = CreateApplication("s6BhdRkqt");
            var responseGenerator = CreateTokenResponseFactory();

            // Act
            var result = await factory.CreateTokenRequestAsync(httpContext.Request.Form.ToDictionary(kvp => kvp.Key, kvp => (string[])kvp.Value));

            var context = result.CreateTokenGeneratingContext(user, application);

            await tokenManager.IssueTokensAsync(context);

            var response = await responseGenerator.CreateTokenResponseAsync(context);

            // Assert
            Assert.Equal(5, response.Parameters.Count);
            Assert.Equal("Bearer", response.TokenType);
            Assert.NotNull(response.IdToken);
            Assert.Contains(response.Parameters, kvp => kvp.Key == "id_token_expires_in");
            Assert.Equal("7200", response.Parameters["id_token_expires_in"]);
            Assert.NotNull(response.RefreshToken);
            Assert.Contains(response.Parameters, kvp => kvp.Key == "refresh_token_expires_in");
            Assert.Equal("2592000", response.Parameters["refresh_token_expires_in"]);
        }

        private ITokenResponseFactory CreateTokenResponseFactory() =>
            new DefaultTokenResponseFactory(new ITokenResponseParameterProvider[]{
                new DefaultTokenResponseParameterProvider(new TimeStampManager())
            });

        private async Task<StringValues> CreateAuthorizationCode(ITokenManager tokenManager)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = QueryString.FromUriComponent(@"?response_type=code&client_id=s6BhdRkqt3&redirect_uri=https%3A%2F%2Fclient.example.org%2Fcb&scope=openid%20profile%20email%20offline_access&nonce=n-0S6_WzA2Mj&state=af0ifjsldkj");
            var requestParameters = httpContext.Request.Query.ToDictionary(kvp => kvp.Key, kvp => (string[])kvp.Value);

            var requestFactory = CreateAuthorizationRequestFactory();

            var user = CreateUser("user");
            var application = CreateApplication("s6BhdRkqt");

            var queryExecutor = new QueryResponseGenerator();

            // Act
            var result = await requestFactory.CreateAuthorizationRequestAsync(requestParameters);
            var authorization = result.Message;

            var tokenContext = result.CreateTokenGeneratingContext(user, application);

            await tokenManager.IssueTokensAsync(tokenContext);

            return tokenContext.AuthorizationCode.SerializedValue;
        }

        private IAuthorizationRequestFactory CreateAuthorizationRequestFactory()
        {
            var clientIdValidatorMock = new Mock<IClientIdValidator>();
            clientIdValidatorMock
                .Setup(m => m.ValidateClientIdAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            var redirectUriValidatorMock = new Mock<IRedirectUriResolver>();
            redirectUriValidatorMock
                .Setup(m => m.ResolveRedirectUriAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((string clientId, string redirectUrl) => RedirectUriResolutionResult.Valid(redirectUrl));

            var scopeValidatorMock = new Mock<IScopeResolver>();
            scopeValidatorMock
                .Setup(m => m.ResolveScopesAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(
                (string clientId, IEnumerable<string> scopes) =>
                    ScopeResolutionResult.Valid(scopes.Select(s => ApplicationScope.CanonicalScopes.TryGetValue(s, out var parsedScope) ? parsedScope : new ApplicationScope(clientId, s))));

            return new AuthorizationRequestFactory(
                clientIdValidatorMock.Object,
                redirectUriValidatorMock.Object,
                scopeValidatorMock.Object,
                Enumerable.Empty<IAuthorizationRequestValidator>(),
                new ProtocolErrorProvider());
        }

        private static ClaimsPrincipal CreateApplication(string clientId) =>
            new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(IdentityServiceClaimTypes.ClientId, clientId) }));

        private static ClaimsPrincipal CreateUser(string userName) =>
            new ClaimsPrincipal(new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.NameIdentifier, userName)}));

        private static TokenManager GetTokenManager()
        {
            var options = CreateOptions();
            var claimsManager = CreateClaimsManager(options);

            var factory = new LoggerFactory();
            var protector = new EphemeralDataProtectionProvider(factory).CreateProtector("test");
            var codeSerializer = new TokenDataSerializer<AuthorizationCode>(options, ArrayPool<char>.Shared);
            var codeDataFormat = new SecureDataFormat<AuthorizationCode>(codeSerializer, protector);
            var refreshTokenSerializer = new TokenDataSerializer<RefreshToken>(options, ArrayPool<char>.Shared);
            var refreshTokenDataFormat = new SecureDataFormat<RefreshToken>(refreshTokenSerializer, protector);

            var timeStampManager = new TimeStampManager();
            var credentialsPolicy = GetCredentialsPolicy(options, timeStampManager);
            var codeIssuer = new AuthorizationCodeIssuer(claimsManager, codeDataFormat, new ProtocolErrorProvider());
            var accessTokenIssuer = new JwtAccessTokenIssuer(claimsManager, credentialsPolicy, new JwtSecurityTokenHandler(), options);
            var idTokenIssuer = new JwtIdTokenIssuer(claimsManager, credentialsPolicy, new JwtSecurityTokenHandler(), options);
            var refreshTokenIssuer = new RefreshTokenIssuer(claimsManager, refreshTokenDataFormat);

            return new TokenManager(
                codeIssuer,
                accessTokenIssuer,
                idTokenIssuer,
                refreshTokenIssuer,
                new ProtocolErrorProvider());
        }

        private static DefaultSigningCredentialsPolicyProvider GetCredentialsPolicy(IOptionsSnapshot<IdentityServiceOptions> options, TimeStampManager timeStampManager) =>
            new DefaultSigningCredentialsPolicyProvider(
                new List<ISigningCredentialsSource> {
                    new DefaultSigningCredentialsSource(options, timeStampManager)
                },
                timeStampManager,
                new HostingEnvironment());

        private static ITokenClaimsManager CreateClaimsManager(
            IOptions<IdentityServiceOptions> options)
        {
            return new DefaultTokenClaimsManager(
                new List<ITokenClaimsProvider>{
                    new DefaultTokenClaimsProvider(options),
                    new GrantedTokensTokenClaimsProvider(),
                    new NonceTokenClaimsProvider(),
                    new ScopesTokenClaimsProvider(),
                    new TimestampsTokenClaimsProvider(new TimeStampManager(),options),
                    new TokenHashTokenClaimsProvider(new TokenHasher())
                });
        }

        private static IOptionsSnapshot<IdentityServiceOptions> CreateOptions()
        {
            var identityServiceOptions = new IdentityServiceOptions();
            var optionsSetup = new IdentityServiceOptionsDefaultSetup();
            optionsSetup.Configure(identityServiceOptions);

            SigningCredentials signingCredentials = new SigningCredentials(CryptoUtilities.CreateTestKey(), "RS256");
            identityServiceOptions.SigningKeys.Add(signingCredentials);
            identityServiceOptions.Issuer = "http://server.example.com";
            identityServiceOptions.IdTokenOptions.UserClaims.AddSingle(
                IdentityServiceClaimTypes.Subject,
                ClaimTypes.NameIdentifier);

            identityServiceOptions.RefreshTokenOptions.UserClaims.AddSingle(
    IdentityServiceClaimTypes.Subject,
    ClaimTypes.NameIdentifier);

            var mock = new Mock<IOptionsSnapshot<IdentityServiceOptions>>();
            mock.Setup(m => m.Value).Returns(identityServiceOptions);
            mock.Setup(m => m.Get(It.IsAny<string>())).Returns(identityServiceOptions);

            return mock.Object;
        }

        private ITokenRequestFactory CreateRequestFactory(ITokenManager tokenManager)
        {
            var clientIdValidatorMock = new Mock<IClientIdValidator>();
            clientIdValidatorMock
                .Setup(m => m.ValidateClientIdAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            clientIdValidatorMock
                .Setup(m => m.ValidateClientCredentialsAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            var redirectUriValidatorMock = new Mock<IRedirectUriResolver>();
            redirectUriValidatorMock
                .Setup(m => m.ResolveRedirectUriAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((string clientId, string redirectUrl) => RedirectUriResolutionResult.Valid(redirectUrl));

            var scopeValidatorMock = new Mock<IScopeResolver>();
            scopeValidatorMock
                .Setup(m => m.ResolveScopesAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(
                (string clientId, IEnumerable<string> scopes) =>
                    ScopeResolutionResult.Valid(scopes.Select(s => ApplicationScope.CanonicalScopes.TryGetValue(s, out var parsedScope) ? parsedScope : new ApplicationScope(clientId, s))));

            return new TokenRequestFactory(
                clientIdValidatorMock.Object,
                redirectUriValidatorMock.Object,
                scopeValidatorMock.Object,
                Enumerable.Empty<ITokenRequestValidator>(),
                tokenManager,
                new TimeStampManager(),
                new ProtocolErrorProvider());
        }
    }
}
