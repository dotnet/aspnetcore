// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Antiforgery.Internal
{
    public class DefaultAntiforgeryTest
    {
        [Fact]
        public async Task ChecksSSL_ValidateRequestAsync_Throws()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var options = new AntiforgeryOptions()
            {
                RequireSsl = true
            };

            var antiforgery = GetAntiforgery(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => antiforgery.ValidateRequestAsync(httpContext));
            Assert.Equal(
                @"The antiforgery system has the configuration value AntiforgeryOptions.RequireSsl = true, " +
                "but the current request is not an SSL request.",
                exception.Message);
        }

        [Fact]
        public async Task ChecksSSL_IsRequestValidAsync_Throws()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var options = new AntiforgeryOptions()
            {
                RequireSsl = true
            };

            var antiforgery = GetAntiforgery(options);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => antiforgery.IsRequestValidAsync(httpContext));
            Assert.Equal(
                @"The antiforgery system has the configuration value AntiforgeryOptions.RequireSsl = true, " +
                "but the current request is not an SSL request.",
                exception.Message);
        }

        [Fact]
        public void ChecksSSL_GetAndStoreTokens_Throws()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var options = new AntiforgeryOptions()
            {
                RequireSsl = true
            };

            var antiforgery = GetAntiforgery(options);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => antiforgery.GetAndStoreTokens(httpContext));
            Assert.Equal(
                 @"The antiforgery system has the configuration value AntiforgeryOptions.RequireSsl = true, " +
                 "but the current request is not an SSL request.",
                 exception.Message);
        }

        [Fact]
        public void ChecksSSL_GetTokens_Throws()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var options = new AntiforgeryOptions()
            {
                RequireSsl = true
            };

            var antiforgery = GetAntiforgery(options);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => antiforgery.GetTokens(httpContext));
            Assert.Equal(
                 @"The antiforgery system has the configuration value AntiforgeryOptions.RequireSsl = true, " +
                 "but the current request is not an SSL request.",
                 exception.Message);
        }

        [Fact]
        public void ChecksSSL_SetCookieTokenAndHeader_Throws()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var options = new AntiforgeryOptions()
            {
                RequireSsl = true
            };

            var antiforgery = GetAntiforgery(options);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => antiforgery.SetCookieTokenAndHeader(httpContext));
            Assert.Equal(
                 @"The antiforgery system has the configuration value AntiforgeryOptions.RequireSsl = true, " +
                 "but the current request is not an SSL request.",
                 exception.Message);
        }

        [Fact]
        public void GetTokens_ExistingInvalidCookieToken_GeneratesANewCookieTokenAndANewFormToken()
        {
            // Arrange
            // Generate a new cookie.
            var context = CreateMockContext(
                new AntiforgeryOptions(),
                useOldCookie: false,
                isOldCookieValid: false);
            var antiforgery = GetAntiforgery(context);

            // Act
            var tokenset = antiforgery.GetTokens(context.HttpContext);

            // Assert
            Assert.Equal("serialized-new-cookie-token", tokenset.CookieToken);
            Assert.Equal("serialized-form-token", tokenset.RequestToken);
        }

        [Fact]
        public void GetTokens_ExistingInvalidCookieToken_SwallowsExceptions()
        {
            // Arrange
            // Make sure the existing cookie is invalid.
            var context = CreateMockContext(
                new AntiforgeryOptions(),
                useOldCookie: false,
                isOldCookieValid: false);

            // This will cause the cookieToken to be null.
            context.TokenSerializer
                .Setup(o => o.Deserialize("serialized-old-cookie-token"))
                .Throws(new Exception("should be swallowed"));

            var antiforgery = GetAntiforgery(context);

            // Act
            var tokenset = antiforgery.GetTokens(context.HttpContext);

            // Assert
            Assert.Equal("serialized-new-cookie-token", tokenset.CookieToken);
            Assert.Equal("serialized-form-token", tokenset.RequestToken);
        }

        [Fact]
        public void GetTokens_ExistingValidCookieToken_GeneratesANewFormToken()
        {
            // Arrange
            var context = CreateMockContext(
                new AntiforgeryOptions(),
                useOldCookie: true,
                isOldCookieValid: true);
            var antiforgery = GetAntiforgery(context);

            // Act
            var tokenset = antiforgery.GetTokens(context.HttpContext);

            // Assert
            Assert.Equal("serialized-old-cookie-token", tokenset.CookieToken);
            Assert.Equal("serialized-form-token", tokenset.RequestToken);
        }

        [Fact]
        public void GetAndStoreTokens_ExistingValidCookieToken_NotOverriden()
        {
            // Arrange
            var context = CreateMockContext(
                new AntiforgeryOptions(),
                useOldCookie: true,
                isOldCookieValid: true);
            var antiforgery = GetAntiforgery(context);

            // Act
            var tokenSet = antiforgery.GetAndStoreTokens(context.HttpContext);

            // Assert
            // We shouldn't have saved the cookie because it already existed.
            context.TokenStore.Verify(
                t => t.SaveCookieToken(It.IsAny<HttpContext>(), It.IsAny<AntiforgeryToken>()), Times.Never);

            Assert.Equal("serialized-old-cookie-token", tokenSet.CookieToken);
            Assert.Equal("serialized-form-token", tokenSet.RequestToken);
        }

        [Fact]
        public void GetAndStoreTokens_NoExistingCookieToken_Saved()
        {
            // Arrange
            var context = CreateMockContext(
                new AntiforgeryOptions(),
                useOldCookie: false,
                isOldCookieValid: false);
            var antiforgery = GetAntiforgery(context);

            // Act
            var tokenSet = antiforgery.GetAndStoreTokens(context.HttpContext);

            // Assert
            context.TokenStore.Verify(
                t => t.SaveCookieToken(It.IsAny<HttpContext>(), It.IsAny<AntiforgeryToken>()), Times.Once);

            Assert.Equal("serialized-new-cookie-token", tokenSet.CookieToken);
            Assert.Equal("serialized-form-token", tokenSet.RequestToken);
        }

        [Fact]
        public async Task IsRequestValidAsync_FromStore_Failure()
        {
            // Arrange
            var context = CreateMockContext(new AntiforgeryOptions());

            string message;
            context.TokenGenerator
                .Setup(o => o.TryValidateTokenSet(
                    context.HttpContext,
                    context.TestTokenSet.OldCookieToken,
                    context.TestTokenSet.RequestToken,
                    out message))
                .Returns(false);

            var antiforgery = GetAntiforgery(context);

            // Act
            var result = await antiforgery.IsRequestValidAsync(context.HttpContext);

            // Assert
            Assert.False(result);
            context.TokenGenerator.Verify();
        }

        [Fact]
        public async Task IsRequestValidAsync_FromStore_Success()
        {
            // Arrange
            var context = CreateMockContext(new AntiforgeryOptions());

            string message;
            context.TokenGenerator
                .Setup(o => o.TryValidateTokenSet(
                    context.HttpContext,
                    context.TestTokenSet.OldCookieToken,
                    context.TestTokenSet.RequestToken,
                    out message))
                .Returns(true)
                .Verifiable();

            var antiforgery = GetAntiforgery(context);

            // Act
            var result = await antiforgery.IsRequestValidAsync(context.HttpContext);

            // Assert
            Assert.True(result);
            context.TokenGenerator.Verify();
        }

        [Fact]
        public async Task ValidateRequestAsync_FromStore_Failure()
        {
            // Arrange
            var context = CreateMockContext(new AntiforgeryOptions());

            var message = "my-message";
            context.TokenGenerator
                .Setup(o => o.TryValidateTokenSet(
                    context.HttpContext,
                    context.TestTokenSet.OldCookieToken,
                    context.TestTokenSet.RequestToken,
                    out message))
                .Returns(false)
                .Verifiable();

            var antiforgery = new DefaultAntiforgery(
                new TestOptionsManager(),
                context.TokenGenerator.Object,
                context.TokenSerializer.Object,
                context.TokenStore.Object);

            // Act & assert
            var exception = await Assert.ThrowsAsync<AntiforgeryValidationException>(
                () => antiforgery.ValidateRequestAsync(context.HttpContext));
            Assert.Equal("my-message", exception.Message);
            context.TokenGenerator.Verify();
        }

        [Fact]
        public async Task ValidateRequestAsync_FromStore_Success()
        {
            // Arrange
            var context = CreateMockContext(new AntiforgeryOptions());

            string message;
            context.TokenGenerator
                .Setup(o => o.TryValidateTokenSet(
                    context.HttpContext,
                    context.TestTokenSet.OldCookieToken,
                    context.TestTokenSet.RequestToken,
                    out message))
                .Returns(true)
                .Verifiable();

            var antiforgery = GetAntiforgery(context);

            // Act
            await antiforgery.ValidateRequestAsync(context.HttpContext);

            // Assert
            context.TokenGenerator.Verify();
        }

        [Fact]
        public async Task ValidateRequestAsync_NoCookieToken_Throws()
        {
            // Arrange
            var context = CreateMockContext(new AntiforgeryOptions()
            {
                CookieName = "cookie-name",
                FormFieldName = "form-field-name",
                HeaderName = null,
            });

            var tokenSet = new AntiforgeryTokenSet(null, null, "form-field-name", null);
            context.TokenStore
                .Setup(s => s.GetRequestTokensAsync(context.HttpContext))
                .Returns(Task.FromResult(tokenSet));

            var antiforgery = GetAntiforgery(context);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AntiforgeryValidationException>(
                () => antiforgery.ValidateRequestAsync(context.HttpContext));
            Assert.Equal("The required antiforgery cookie \"cookie-name\" is not present.", exception.Message);
        }

        [Fact]
        public async Task ValidateRequestAsync_NonFormRequest_HeaderDisabled_Throws()
        {
            // Arrange
            var context = CreateMockContext(new AntiforgeryOptions()
            {
                CookieName = "cookie-name",
                FormFieldName = "form-field-name",
                HeaderName = null,
            });

            var tokenSet = new AntiforgeryTokenSet(null, "cookie-token", "form-field-name", null);
            context.TokenStore
                .Setup(s => s.GetRequestTokensAsync(context.HttpContext))
                .Returns(Task.FromResult(tokenSet));

            var antiforgery = GetAntiforgery(context);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AntiforgeryValidationException>(
                () => antiforgery.ValidateRequestAsync(context.HttpContext));
            Assert.Equal("The required antiforgery form field \"form-field-name\" is not present.", exception.Message);
        }

        [Fact]
        public async Task ValidateRequestAsync_NonFormRequest_NoHeaderValue_Throws()
        {
            // Arrange
            var context = CreateMockContext(new AntiforgeryOptions()
            {
                CookieName = "cookie-name",
                FormFieldName = "form-field-name",
                HeaderName = "header-name",
            });

            context.HttpContext.Request.ContentType = "application/json";

            var tokenSet = new AntiforgeryTokenSet(null, "cookie-token", "form-field-name", "header-name");
            context.TokenStore
                .Setup(s => s.GetRequestTokensAsync(context.HttpContext))
                .Returns(Task.FromResult(tokenSet));

            var antiforgery = GetAntiforgery(context);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AntiforgeryValidationException>(
                () => antiforgery.ValidateRequestAsync(context.HttpContext));
            Assert.Equal("The required antiforgery header value \"header-name\" is not present.", exception.Message);
        }

        [Fact]
        public async Task ValidateRequestAsync_FormRequest_NoRequestTokenValue_Throws()
        {
            // Arrange
            var context = CreateMockContext(new AntiforgeryOptions()
            {
                CookieName = "cookie-name",
                FormFieldName = "form-field-name",
                HeaderName = "header-name",
            });

            context.HttpContext.Request.ContentType = "application/x-www-form-urlencoded";

            var tokenSet = new AntiforgeryTokenSet(null, "cookie-token", "form-field-name", "header-name");
            context.TokenStore
                .Setup(s => s.GetRequestTokensAsync(context.HttpContext))
                .Returns(Task.FromResult(tokenSet));

            var antiforgery = GetAntiforgery(context);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AntiforgeryValidationException>(
                () => antiforgery.ValidateRequestAsync(context.HttpContext));
            Assert.Equal(
                "The required antiforgery request token was not provided in either form field \"form-field-name\" " +
                "or header value \"header-name\".",
                exception.Message);
        }

        [Theory]
        [InlineData(false, "SAMEORIGIN")]
        [InlineData(true, null)]
        public void SetCookieTokenAndHeader_AddsXFrameOptionsHeader(
            bool suppressXFrameOptions,
            string expectedHeaderValue)
        {
            // Arrange
            var options = new AntiforgeryOptions()
            {
                SuppressXFrameOptionsHeader = suppressXFrameOptions
            };

            // Generate a new cookie.
            var context = CreateMockContext(options, useOldCookie: false, isOldCookieValid: false);
            var antiforgery = GetAntiforgery(context);

            // Act
            antiforgery.SetCookieTokenAndHeader(context.HttpContext);

            // Assert
            var xFrameOptions = context.HttpContext.Response.Headers["X-Frame-Options"];
            Assert.Equal(expectedHeaderValue, xFrameOptions);
        }

        private DefaultAntiforgery GetAntiforgery(
            AntiforgeryOptions options = null,
            IAntiforgeryTokenGenerator tokenGenerator = null,
            IAntiforgeryTokenSerializer tokenSerializer = null,
            IAntiforgeryTokenStore tokenStore = null)
        {
            var optionsManager = new TestOptionsManager();
            if (options != null)
            {
                optionsManager.Value = options;
            }

            return new DefaultAntiforgery(
                antiforgeryOptionsAccessor: optionsManager,
                tokenGenerator: tokenGenerator,
                tokenSerializer: tokenSerializer,
                tokenStore: tokenStore);
        }

        private HttpContext GetHttpContext()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity("some-auth"));
            return httpContext;
        }

        private DefaultAntiforgery GetAntiforgery(AntiforgeryMockContext context)
        {
            return GetAntiforgery(
                context.Options,
                context.TokenGenerator?.Object,
                context.TokenSerializer?.Object,
                context.TokenStore?.Object);
        }

        private Mock<IAntiforgeryTokenStore> GetTokenStore(
            HttpContext context,
            TestTokenSet testTokenSet,
            bool saveNewCookie = true)
        {
            var oldCookieToken = testTokenSet.OldCookieToken;
            var formToken = testTokenSet.RequestToken;
            var mockTokenStore = new Mock<IAntiforgeryTokenStore>(MockBehavior.Strict);
            mockTokenStore.Setup(o => o.GetCookieToken(context))
                          .Returns(oldCookieToken);

            mockTokenStore.Setup(o => o.GetRequestTokensAsync(context))
                          .Returns(() => Task.FromResult(new AntiforgeryTokenSet(
                              testTokenSet.FormTokenString,
                              testTokenSet.OldCookieTokenString,
                              "form",
                              "header")));

            if (saveNewCookie)
            {
                var newCookieToken = testTokenSet.NewCookieToken;
                mockTokenStore.Setup(o => o.SaveCookieToken(context, newCookieToken))
                              .Verifiable();
            }

            return mockTokenStore;
        }

        private Mock<IAntiforgeryTokenSerializer> GetTokenSerializer(TestTokenSet testTokenSet)
        {
            var oldCookieToken = testTokenSet.OldCookieToken;
            var newCookieToken = testTokenSet.NewCookieToken;
            var formToken = testTokenSet.RequestToken;
            var mockSerializer = new Mock<IAntiforgeryTokenSerializer>(MockBehavior.Strict);
            mockSerializer.Setup(o => o.Serialize(formToken))
                          .Returns(testTokenSet.FormTokenString);
            mockSerializer.Setup(o => o.Deserialize(testTokenSet.FormTokenString))
                          .Returns(formToken);
            mockSerializer.Setup(o => o.Deserialize(testTokenSet.OldCookieTokenString))
                          .Returns(oldCookieToken);
            mockSerializer.Setup(o => o.Serialize(oldCookieToken))
                          .Returns(testTokenSet.OldCookieTokenString);
            mockSerializer.Setup(o => o.Serialize(newCookieToken))
                          .Returns(testTokenSet.NewCookieTokenString);
            return mockSerializer;
        }

        private AntiforgeryMockContext CreateMockContext(
            AntiforgeryOptions options,
            bool useOldCookie = false,
            bool isOldCookieValid = true)
        {
            // Arrange
            var httpContext = GetHttpContext();
            var testTokenSet = GetTokenSet();

            var mockSerializer = GetTokenSerializer(testTokenSet);

            var mockTokenStore = GetTokenStore(httpContext, testTokenSet, !useOldCookie);

            var mockGenerator = new Mock<IAntiforgeryTokenGenerator>(MockBehavior.Strict);
            mockGenerator
                .Setup(o => o.GenerateRequestToken(
                    httpContext,
                    useOldCookie ? testTokenSet.OldCookieToken : testTokenSet.NewCookieToken))
                .Returns(testTokenSet.RequestToken);

            mockGenerator
                .Setup(o => o.GenerateCookieToken())
                .Returns(useOldCookie ? testTokenSet.OldCookieToken : testTokenSet.NewCookieToken);

            mockGenerator
                .Setup(o => o.IsCookieTokenValid(testTokenSet.OldCookieToken))
                .Returns(isOldCookieValid);

            mockGenerator
                .Setup(o => o.IsCookieTokenValid(testTokenSet.NewCookieToken))
                .Returns(!isOldCookieValid);

            return new AntiforgeryMockContext()
            {
                Options = options,
                HttpContext = httpContext,
                TokenGenerator = mockGenerator,
                TokenSerializer = mockSerializer,
                TokenStore = mockTokenStore,
                TestTokenSet = testTokenSet
            };
        }

        private TestTokenSet GetTokenSet()
        {
            return new TestTokenSet()
            {
                RequestToken = new AntiforgeryToken() { IsCookieToken = false },
                FormTokenString = "serialized-form-token",
                OldCookieToken = new AntiforgeryToken() { IsCookieToken = true },
                OldCookieTokenString = "serialized-old-cookie-token",
                NewCookieToken = new AntiforgeryToken() { IsCookieToken = true },
                NewCookieTokenString = "serialized-new-cookie-token",
            };
        }

        private class TestTokenSet
        {
            public AntiforgeryToken RequestToken { get; set; }

            public string FormTokenString { get; set; }

            public AntiforgeryToken OldCookieToken { get; set; }

            public string OldCookieTokenString { get; set; }

            public AntiforgeryToken NewCookieToken { get; set; }

            public string NewCookieTokenString { get; set; }
        }

        private class AntiforgeryMockContext
        {
            public AntiforgeryOptions Options { get; set; }

            public TestTokenSet TestTokenSet { get; set; }

            public HttpContext HttpContext { get; set; }

            public Mock<IAntiforgeryTokenGenerator> TokenGenerator { get; set; }

            public Mock<IAntiforgeryTokenStore> TokenStore { get; set; }

            public Mock<IAntiforgeryTokenSerializer> TokenSerializer { get; set; }
        }

        private class TestOptionsManager : IOptions<AntiforgeryOptions>
        {
            public AntiforgeryOptions Value { get; set; } = new AntiforgeryOptions();
        }
    }
}