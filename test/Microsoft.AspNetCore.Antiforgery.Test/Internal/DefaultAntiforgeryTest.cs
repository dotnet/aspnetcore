// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders.Testing;
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
                        async () => await antiforgery.ValidateRequestAsync(httpContext));
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
                        async () => await antiforgery.IsRequestValidAsync(httpContext));
            Assert.Equal(
                @"The antiforgery system has the configuration value AntiforgeryOptions.RequireSsl = true, " +
                "but the current request is not an SSL request.",
                exception.Message);
        }

        [Fact]
        public void ChecksSSL_ValidateTokens_Throws()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();

            var options = new AntiforgeryOptions()
            {
                RequireSsl = true
            };

            var antiforgery = GetAntiforgery(options);

            var tokenSet = new AntiforgeryTokenSet("hello", "world", "form", "header");

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => antiforgery.ValidateTokens(httpContext, tokenSet));
            Assert.Equal(
                @"The antiforgery system has the configuration value AntiforgeryOptions.RequireSsl = true, " +
                "but the current request is not an SSL request.",
                exception.Message);
        }

        [Fact]
        public void ChecksSSL_GetHtml_Throws()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();

            var options = new AntiforgeryOptions()
            {
                RequireSsl = true
            };

            var antiforgery = GetAntiforgery(options);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => antiforgery.GetHtml(httpContext));
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
        public void GetHtml_ExistingInvalidCookieToken_GeneratesANewCookieAndAnAntiforgeryToken()
        {
            // Arrange
            var options = new AntiforgeryOptions()
            {
                FormFieldName = "form-field-name"
            };

            // Make sure the existing cookie is invalid.
            var context = CreateMockContext(options, isOldCookieValid: false);
            var antiforgery = GetAntiforgery(context);
            var encoder = new HtmlTestEncoder();

            // Setup so that the null cookie token returned is treated as invalid.
            context.TokenGenerator
                .Setup(o => o.IsCookieTokenValid(null))
                .Returns(false);

            // Act
            var inputElement = antiforgery.GetHtml(context.HttpContext);

            // Assert
            using (var writer = new StringWriter())
            {
                inputElement.WriteTo(writer, encoder);

                Assert.Equal(
                    @"<input name=""HtmlEncode[[form-field-name]]"" type=""hidden"" " +
                    @"value=""HtmlEncode[[serialized-form-token]]"" />",
                    writer.ToString());
            }

            context.TokenStore.Verify();
        }

        [Fact]
        public void GetHtml_ExistingInvalidCookieToken_SwallowsExceptions()
        {
            // Arrange
            var options = new AntiforgeryOptions()
            {
                FormFieldName = "form-field-name"
            };

            // Make sure the existing cookie is invalid.
            var context = CreateMockContext(options, isOldCookieValid: false);
            var antiforgery = GetAntiforgery(context);

            // This will cause the cookieToken to be null.
            context.TokenStore
                .Setup(o => o.GetCookieToken(context.HttpContext))
                .Throws(new Exception("should be swallowed"));

            // Setup so that the null cookie token returned is treated as invalid.
            context.TokenGenerator
                .Setup(o => o.IsCookieTokenValid(null))
                .Returns(false);

            var encoder = new HtmlTestEncoder();

            // Act
            var inputElement = antiforgery.GetHtml(context.HttpContext);

            // Assert
            using (var writer = new StringWriter())
            {
                inputElement.WriteTo(writer, encoder);

                Assert.Equal(
                    @"<input name=""HtmlEncode[[form-field-name]]"" type=""hidden"" " +
                    @"value=""HtmlEncode[[serialized-form-token]]"" />",
                    writer.ToString());
            }

            context.TokenStore.Verify();
        }

        [Fact]
        public void GetHtml_ExistingValidCookieToken_GeneratesAnAntiforgeryToken()
        {
            // Arrange
            var options = new AntiforgeryOptions()
            {
                FormFieldName = "form-field-name"
            };

            // Make sure the existing cookie is valid and use the same cookie for the mock Token Provider.
            var context = CreateMockContext(options, useOldCookie: true, isOldCookieValid: true);
            var antiforgery = GetAntiforgery(context);
            var encoder = new HtmlTestEncoder();

            // Act
            var inputElement = antiforgery.GetHtml(context.HttpContext);

            // Assert
            using (var writer = new StringWriter())
            {
                inputElement.WriteTo(writer, encoder);

                Assert.Equal(
                    @"<input name=""HtmlEncode[[form-field-name]]"" type=""hidden"" " +
                    @"value=""HtmlEncode[[serialized-form-token]]"" />",
                    writer.ToString());
            }
        }

        [Theory]
        [InlineData(false, "SAMEORIGIN")]
        [InlineData(true, null)]
        public void GetHtml_AddsXFrameOptionsHeader(bool suppressXFrameOptions, string expectedHeaderValue)
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
            antiforgery.GetHtml(context.HttpContext);

            // Assert
            string xFrameOptions = context.HttpContext.Response.Headers["X-Frame-Options"];
            Assert.Equal(expectedHeaderValue, xFrameOptions);
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
        public void ValidateTokens_InvalidTokens_Throws()
        {
            // Arrange
            var context = CreateMockContext(new AntiforgeryOptions());

            context.TokenSerializer
                .Setup(o => o.Deserialize("cookie-token"))
                .Returns(context.TestTokenSet.OldCookieToken);
            context.TokenSerializer
                .Setup(o => o.Deserialize("form-token"))
                .Returns(context.TestTokenSet.RequestToken);

            // You can't really do Moq with out-parameters :(
            var tokenGenerator = new TestTokenGenerator()
            {
                Message = "my-message",
            };

            var antiforgery = new DefaultAntiforgery(
                new TestOptionsManager(),
                tokenGenerator,
                context.TokenSerializer.Object,
                tokenStore: null);

            var tokenSet = new AntiforgeryTokenSet("form-token", "cookie-token", "form", "header");

            // Act & Assert
            var exception = Assert.Throws<AntiforgeryValidationException>(
                    () => antiforgery.ValidateTokens(
                        context.HttpContext,
                        tokenSet));
            Assert.Equal("my-message", exception.Message);
        }

        [Fact]
        public void ValidateTokens_FromValidStrings_TokensValidatedSuccessfully()
        {
            // Arrange
            var context = CreateMockContext(new AntiforgeryOptions());

            context.TokenSerializer
                .Setup(o => o.Deserialize("cookie-token"))
                .Returns(context.TestTokenSet.OldCookieToken);
            context.TokenSerializer
                .Setup(o => o.Deserialize("form-token"))
                .Returns(context.TestTokenSet.RequestToken);

            string message;
            context.TokenGenerator
                .Setup(o => o.TryValidateTokenSet(
                    context.HttpContext,
                    context.TestTokenSet.OldCookieToken,
                    context.TestTokenSet.RequestToken,
                    out message))
                .Returns(true)
                .Verifiable();
            context.TokenStore = null;
            var antiforgery = GetAntiforgery(context);

            var tokenSet = new AntiforgeryTokenSet("form-token", "cookie-token", "form", "header");

            // Act
            antiforgery.ValidateTokens(context.HttpContext, tokenSet);

            // Assert
            context.TokenGenerator.Verify();
        }

        [Fact]
        public void ValidateTokens_MissingCookieInTokenSet_Throws()
        {
            // Arrange
            var context = CreateMockContext(new AntiforgeryOptions());
            var antiforgery = GetAntiforgery(context);

            var tokenSet = new AntiforgeryTokenSet("form-token", null, "form", "header");

            // Act
            ExceptionAssert.ThrowsArgument(
                () => antiforgery.ValidateTokens(context.HttpContext, tokenSet),
                "antiforgeryTokenSet",
                "The required antiforgery cookie token must be provided.");
        }

        [Fact]
        public async Task IsRequestValueAsync_FromStore_Failure()
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

            // You can't really do Moq with out-parameters :(
            var tokenGenerator = new TestTokenGenerator()
            {
                Message = "my-message",
            };

            var antiforgery = new DefaultAntiforgery(
                new TestOptionsManager(),
                tokenGenerator,
                context.TokenSerializer.Object,
                context.TokenStore.Object);

            // Act & assert
            var exception = await Assert.ThrowsAsync<AntiforgeryValidationException>(
                        async () => await antiforgery.ValidateRequestAsync(context.HttpContext));
            Assert.Equal("my-message", exception.Message);
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

        private class TestTokenGenerator : IAntiforgeryTokenGenerator
        {
            public string Message { get; set; }

            public AntiforgeryToken GenerateCookieToken()
            {
                throw new NotImplementedException();
            }

            public AntiforgeryToken GenerateRequestToken(HttpContext httpContext, AntiforgeryToken cookieToken)
            {
                throw new NotImplementedException();
            }

            public bool IsCookieTokenValid(AntiforgeryToken cookieToken)
            {
                throw new NotImplementedException();
            }

            public bool TryValidateTokenSet(
                HttpContext httpContext,
                AntiforgeryToken cookieToken,
                AntiforgeryToken requestToken,
                out string message)
            {
                message = Message;
                return false;
            }
        }
    }
}