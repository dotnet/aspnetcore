// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNet.Http.Internal;
#if DNX451
using Moq;
#endif
using Xunit;

namespace Microsoft.AspNet.Antiforgery
{
    public class DefaultAntiforgeryTokenGeneratorProviderTest
    {
        [Fact]
        public void GenerateCookieToken()
        {
            // Arrange
            var tokenProvider = new DefaultAntiforgeryTokenGenerator(
                claimUidExtractor: null,
                additionalDataProvider: null);

            // Act
            var token = tokenProvider.GenerateCookieToken();

            // Assert
            Assert.NotNull(token);
        }

        [Fact]
        public void GenerateFormToken_AnonymousUser()
        {
            // Arrange
            var cookieToken = new AntiforgeryToken() { IsSessionToken = true };
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
            Assert.False(httpContext.User.Identity.IsAuthenticated);

            var tokenProvider = new DefaultAntiforgeryTokenGenerator(
                claimUidExtractor: null,
                additionalDataProvider: null);

            // Act
            var fieldToken = tokenProvider.GenerateFormToken(httpContext, cookieToken);

            // Assert
            Assert.NotNull(fieldToken);
            Assert.Equal(cookieToken.SecurityToken, fieldToken.SecurityToken);
            Assert.False(fieldToken.IsSessionToken);
            Assert.Empty(fieldToken.Username);
            Assert.Null(fieldToken.ClaimUid);
            Assert.Empty(fieldToken.AdditionalData);
        }

#if DNX451

        [Fact]
        public void GenerateFormToken_AuthenticatedWithoutUsernameAndNoAdditionalData_NoAdditionalData()
        {
            // Arrange
            var cookieToken = new AntiforgeryToken()
            {
                IsSessionToken = true
            };

            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new MyAuthenticatedIdentityWithoutUsername());

            var options = new AntiforgeryOptions();
            var claimUidExtractor = new Mock<IClaimUidExtractor>().Object;

            var tokenProvider = new DefaultAntiforgeryTokenGenerator(
                claimUidExtractor: claimUidExtractor,
                additionalDataProvider: null);

            // Act & assert
            var exception = Assert.Throws<InvalidOperationException>(
                    () => tokenProvider.GenerateFormToken(httpContext, cookieToken));
            Assert.Equal(
                "The provided identity of type " +
                $"'{typeof(MyAuthenticatedIdentityWithoutUsername).FullName}' " +
                "is marked IsAuthenticated = true but does not have a value for Name. " +
                "By default, the antiforgery system requires that all authenticated identities have a unique Name. " +
                "If it is not possible to provide a unique Name for this identity, " +
                "consider extending IAntiforgeryAdditionalDataProvider by overriding the " +
                "DefaultAntiforgeryAdditionalDataProvider " +
                "or a custom type that can provide some form of unique identifier for the current user.",
                exception.Message);
        }

        [Fact]
        public void GenerateFormToken_AuthenticatedWithoutUsername_WithAdditionalData()
        {
            // Arrange
            var cookieToken = new AntiforgeryToken() { IsSessionToken = true };

            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new MyAuthenticatedIdentityWithoutUsername());

            var mockAdditionalDataProvider = new Mock<IAntiforgeryAdditionalDataProvider>();
            mockAdditionalDataProvider.Setup(o => o.GetAdditionalData(httpContext))
                                      .Returns("additional-data");

            var claimUidExtractor = new Mock<IClaimUidExtractor>().Object;

            var tokenProvider = new DefaultAntiforgeryTokenGenerator(
                claimUidExtractor: claimUidExtractor,
                additionalDataProvider: mockAdditionalDataProvider.Object);

            // Act
            var fieldToken = tokenProvider.GenerateFormToken(httpContext, cookieToken);

            // Assert
            Assert.NotNull(fieldToken);
            Assert.Equal(cookieToken.SecurityToken, fieldToken.SecurityToken);
            Assert.False(fieldToken.IsSessionToken);
            Assert.Empty(fieldToken.Username);
            Assert.Null(fieldToken.ClaimUid);
            Assert.Equal("additional-data", fieldToken.AdditionalData);
        }

        [Fact]
        public void GenerateFormToken_ClaimsBasedIdentity()
        {
            // Arrange
            var cookieToken = new AntiforgeryToken() { IsSessionToken = true };

            var identity = GetAuthenticatedIdentity("some-identity");
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(identity);

            byte[] data = new byte[256 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(data);
            }
            var base64ClaimUId = Convert.ToBase64String(data);
            var expectedClaimUid = new BinaryBlob(256, data);

            var mockClaimUidExtractor = new Mock<IClaimUidExtractor>();
            mockClaimUidExtractor.Setup(o => o.ExtractClaimUid(identity))
                                 .Returns(base64ClaimUId);

            var tokenProvider = new DefaultAntiforgeryTokenGenerator(
                claimUidExtractor: mockClaimUidExtractor.Object,
                additionalDataProvider: null);

            // Act
            var fieldToken = tokenProvider.GenerateFormToken(httpContext, cookieToken);

            // Assert
            Assert.NotNull(fieldToken);
            Assert.Equal(cookieToken.SecurityToken, fieldToken.SecurityToken);
            Assert.False(fieldToken.IsSessionToken);
            Assert.Equal("", fieldToken.Username);
            Assert.Equal(expectedClaimUid, fieldToken.ClaimUid);
            Assert.Equal("", fieldToken.AdditionalData);
        }

        [Fact]
        public void GenerateFormToken_RegularUserWithUsername()
        {
            // Arrange
            var cookieToken = new AntiforgeryToken() { IsSessionToken = true };

            var httpContext = new DefaultHttpContext();
            var mockIdentity = new Mock<ClaimsIdentity>();
            mockIdentity.Setup(o => o.IsAuthenticated)
                        .Returns(true);
            mockIdentity.Setup(o => o.Name)
                        .Returns("my-username");

            httpContext.User = new ClaimsPrincipal(mockIdentity.Object);
            
            var claimUidExtractor = new Mock<IClaimUidExtractor>().Object;

            var tokenProvider = new DefaultAntiforgeryTokenGenerator(
                claimUidExtractor: claimUidExtractor,
                additionalDataProvider: null);

            // Act
            var fieldToken = tokenProvider.GenerateFormToken(httpContext, cookieToken);

            // Assert
            Assert.NotNull(fieldToken);
            Assert.Equal(cookieToken.SecurityToken, fieldToken.SecurityToken);
            Assert.False(fieldToken.IsSessionToken);
            Assert.Equal("my-username", fieldToken.Username);
            Assert.Null(fieldToken.ClaimUid);
            Assert.Empty(fieldToken.AdditionalData);
        }
#endif

        [Fact]
        public void IsCookieTokenValid_FieldToken_ReturnsFalse()
        {
            // Arrange
            var cookieToken = new AntiforgeryToken()
            {
                IsSessionToken = false
            };

            var tokenProvider = new DefaultAntiforgeryTokenGenerator(
                claimUidExtractor: null,
                additionalDataProvider: null);

            // Act
            bool retVal = tokenProvider.IsCookieTokenValid(cookieToken);

            // Assert
            Assert.False(retVal);
        }

        [Fact]
        public void IsCookieTokenValid_NullToken_ReturnsFalse()
        {
            // Arrange
            AntiforgeryToken cookieToken = null;
            var tokenProvider = new DefaultAntiforgeryTokenGenerator(
                claimUidExtractor: null,
                additionalDataProvider: null);

            // Act
            var isValid = tokenProvider.IsCookieTokenValid(cookieToken);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsCookieTokenValid_ValidToken_ReturnsTrue()
        {
            // Arrange
            var cookieToken = new AntiforgeryToken()
            {
                IsSessionToken = true
            };

            var tokenProvider = new DefaultAntiforgeryTokenGenerator(
                claimUidExtractor: null,
                additionalDataProvider: null);

            // Act
            var isValid = tokenProvider.IsCookieTokenValid(cookieToken);

            // Assert
            Assert.True(isValid);
        }


        [Fact]
        public void ValidateTokens_SessionTokenMissing()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            var fieldtoken = new AntiforgeryToken() { IsSessionToken = false };

            var tokenProvider = new DefaultAntiforgeryTokenGenerator(
                claimUidExtractor: null,
                additionalDataProvider: null);

            // Act & assert
            var ex = Assert.Throws<ArgumentNullException>(
                () => tokenProvider.ValidateTokens(httpContext, null, fieldtoken));

            var trimmed = ex.Message.Substring(0, ex.Message.IndexOf(Environment.NewLine));
            Assert.Equal(@"The cookie token must be provided.", trimmed);
        }

        [Fact]
        public void ValidateTokens_FieldTokenMissing()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            var sessionToken = new AntiforgeryToken() { IsSessionToken = true };

            var tokenProvider = new DefaultAntiforgeryTokenGenerator(
                claimUidExtractor: null,
                additionalDataProvider: null);

            // Act & assert
            var ex = Assert.Throws<ArgumentNullException>(
                () => tokenProvider.ValidateTokens(httpContext, sessionToken, null));

            var trimmed = ex.Message.Substring(0, ex.Message.IndexOf(Environment.NewLine));
            Assert.Equal("The form token must be provided.", trimmed);
        }

        [Fact]
        public void ValidateTokens_FieldAndSessionTokensSwapped()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            var sessionToken = new AntiforgeryToken() { IsSessionToken = true };
            var fieldtoken = new AntiforgeryToken() { IsSessionToken = false };

            var tokenProvider = new DefaultAntiforgeryTokenGenerator(
                claimUidExtractor: null,
                additionalDataProvider: null);

            // Act & assert
            var ex1 =
                Assert.Throws<InvalidOperationException>(
                    () => tokenProvider.ValidateTokens(httpContext, fieldtoken, fieldtoken));
            Assert.Equal(
                "Validation of the provided antiforgery token failed. " +
                @"The cookie token and the form token were swapped.",
                ex1.Message);

            var ex2 =
                Assert.Throws<InvalidOperationException>(
                    () => tokenProvider.ValidateTokens(httpContext, sessionToken, sessionToken));
            Assert.Equal(
                "Validation of the provided antiforgery token failed. " +
                @"The cookie token and the form token were swapped.",
                ex2.Message);
        }

        [Fact]
        public void ValidateTokens_FieldAndSessionTokensHaveDifferentSecurityKeys()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            var sessionToken = new AntiforgeryToken() { IsSessionToken = true };
            var fieldtoken = new AntiforgeryToken() { IsSessionToken = false };

            var tokenProvider = new DefaultAntiforgeryTokenGenerator(
                claimUidExtractor: null,
                additionalDataProvider: null);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                    () => tokenProvider.ValidateTokens(httpContext, sessionToken, fieldtoken));
            Assert.Equal(
                @"The antiforgery cookie token and form field token do not match.",
                exception.Message);
        }

#if DNX451

        [Theory]
        [InlineData("the-user", "the-other-user")]
        [InlineData("http://example.com/uri-casing", "http://example.com/URI-casing")]
        [InlineData("https://example.com/secure-uri-casing", "https://example.com/secure-URI-casing")]
        public void ValidateTokens_UsernameMismatch(string identityUsername, string embeddedUsername)
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var identity = GetAuthenticatedIdentity(identityUsername);
            httpContext.User = new ClaimsPrincipal(identity);

            var sessionToken = new AntiforgeryToken() { IsSessionToken = true };
            var fieldtoken = new AntiforgeryToken()
            {
                SecurityToken = sessionToken.SecurityToken,
                Username = embeddedUsername,
                IsSessionToken = false
            };

            var mockClaimUidExtractor = new Mock<IClaimUidExtractor>();
            mockClaimUidExtractor.Setup(o => o.ExtractClaimUid(identity))
                                 .Returns((string)null);

            var tokenProvider = new DefaultAntiforgeryTokenGenerator(
                claimUidExtractor: mockClaimUidExtractor.Object,
                additionalDataProvider: null);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                    () => tokenProvider.ValidateTokens(httpContext, sessionToken, fieldtoken));
            Assert.Equal(
                @"The provided antiforgery token was meant for user """ + embeddedUsername +
                @""", but the current user is """ + identityUsername + @""".",
                exception.Message);
        }

        [Fact]
        public void ValidateTokens_ClaimUidMismatch()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var identity = GetAuthenticatedIdentity("the-user");
            httpContext.User = new ClaimsPrincipal(identity);

            var sessionToken = new AntiforgeryToken() { IsSessionToken = true };
            var fieldtoken = new AntiforgeryToken()
            {
                SecurityToken = sessionToken.SecurityToken,
                IsSessionToken = false,
                ClaimUid = new BinaryBlob(256)
            };

            var differentToken = new BinaryBlob(256);
            var mockClaimUidExtractor = new Mock<IClaimUidExtractor>();
            mockClaimUidExtractor.Setup(o => o.ExtractClaimUid(identity))
                                 .Returns(Convert.ToBase64String(differentToken.GetData()));

            var tokenProvider = new DefaultAntiforgeryTokenGenerator(
                claimUidExtractor: mockClaimUidExtractor.Object,
                additionalDataProvider: null);

            // Act & assert
            var exception = Assert.Throws<InvalidOperationException>(
                    () => tokenProvider.ValidateTokens(httpContext, sessionToken, fieldtoken));
            Assert.Equal(
                @"The provided antiforgery token was meant for a different claims-based user than the current user.",
                exception.Message);
        }

        [Fact]
        public void ValidateTokens_AdditionalDataRejected()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity();
            httpContext.User = new ClaimsPrincipal(identity);

            var sessionToken = new AntiforgeryToken() { IsSessionToken = true };
            var fieldtoken = new AntiforgeryToken()
            {
                SecurityToken = sessionToken.SecurityToken,
                Username = String.Empty,
                IsSessionToken = false,
                AdditionalData = "some-additional-data"
            };

            var mockAdditionalDataProvider = new Mock<IAntiforgeryAdditionalDataProvider>();
            mockAdditionalDataProvider.Setup(o => o.ValidateAdditionalData(httpContext, "some-additional-data"))
                                      .Returns(false);

            var tokenProvider = new DefaultAntiforgeryTokenGenerator(
                claimUidExtractor: null,
                additionalDataProvider: mockAdditionalDataProvider.Object);

            // Act & assert
            var exception = Assert.Throws<InvalidOperationException>(
                    () => tokenProvider.ValidateTokens(httpContext, sessionToken, fieldtoken));
            Assert.Equal(@"The provided antiforgery token failed a custom data check.", exception.Message);
        }

        [Fact]
        public void ValidateTokens_Success_AnonymousUser()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity();
            httpContext.User = new ClaimsPrincipal(identity);

            var sessionToken = new AntiforgeryToken() { IsSessionToken = true };
            var fieldtoken = new AntiforgeryToken()
            {
                SecurityToken = sessionToken.SecurityToken,
                Username = String.Empty,
                IsSessionToken = false,
                AdditionalData = "some-additional-data"
            };

            var mockAdditionalDataProvider = new Mock<IAntiforgeryAdditionalDataProvider>();
            mockAdditionalDataProvider.Setup(o => o.ValidateAdditionalData(httpContext, "some-additional-data"))
                                      .Returns(true);

            var tokenProvider = new DefaultAntiforgeryTokenGenerator(
                claimUidExtractor: null,
                additionalDataProvider: mockAdditionalDataProvider.Object);

            // Act
            tokenProvider.ValidateTokens(httpContext, sessionToken, fieldtoken);

            // Assert
            // Nothing to assert - if we got this far, success!
        }

        [Fact]
        public void ValidateTokens_Success_AuthenticatedUserWithUsername()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var identity = GetAuthenticatedIdentity("the-user");
            httpContext.User = new ClaimsPrincipal(identity);

            var sessionToken = new AntiforgeryToken() { IsSessionToken = true };
            var fieldtoken = new AntiforgeryToken()
            {
                SecurityToken = sessionToken.SecurityToken,
                Username = "THE-USER",
                IsSessionToken = false,
                AdditionalData = "some-additional-data"
            };

            var mockAdditionalDataProvider = new Mock<IAntiforgeryAdditionalDataProvider>();
            mockAdditionalDataProvider.Setup(o => o.ValidateAdditionalData(httpContext, "some-additional-data"))
                                      .Returns(true);

            var tokenProvider = new DefaultAntiforgeryTokenGenerator(
                claimUidExtractor: new Mock<IClaimUidExtractor>().Object,
                additionalDataProvider: mockAdditionalDataProvider.Object);

            // Act
            tokenProvider.ValidateTokens(httpContext, sessionToken, fieldtoken);

            // Assert
            // Nothing to assert - if we got this far, success!
        }

        [Fact]
        public void ValidateTokens_Success_ClaimsBasedUser()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var identity = GetAuthenticatedIdentity("the-user");
            httpContext.User = new ClaimsPrincipal(identity);

            var sessionToken = new AntiforgeryToken() { IsSessionToken = true };
            var fieldtoken = new AntiforgeryToken()
            {
                SecurityToken = sessionToken.SecurityToken,
                IsSessionToken = false,
                ClaimUid = new BinaryBlob(256)
            };

            var mockClaimUidExtractor = new Mock<IClaimUidExtractor>();
            mockClaimUidExtractor.Setup(o => o.ExtractClaimUid(identity))
                                 .Returns(Convert.ToBase64String(fieldtoken.ClaimUid.GetData()));

            var tokenProvider = new DefaultAntiforgeryTokenGenerator(
                claimUidExtractor: mockClaimUidExtractor.Object,
                additionalDataProvider: null);

            // Act
            tokenProvider.ValidateTokens(httpContext, sessionToken, fieldtoken);

            // Assert
            // Nothing to assert - if we got this far, success!
        }
#endif

        private static ClaimsIdentity GetAuthenticatedIdentity(string identityUsername)
        {
            var claim = new Claim(ClaimsIdentity.DefaultNameClaimType, identityUsername);
            return new ClaimsIdentity(new[] { claim }, "Some-Authentication");
        }

        private sealed class MyAuthenticatedIdentityWithoutUsername : ClaimsIdentity
        {
            public override bool IsAuthenticated
            {
                get { return true; }
            }

            public override string Name
            {
                get { return String.Empty; }
            }
        }
    }
}