// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.Framework.OptionsModel;
using Microsoft.Framework.WebEncoders.Testing;
#if DNX451
using Moq;
#endif
using Xunit;

namespace Microsoft.AspNet.Antiforgery
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
        public void ChecksSSL_ValidateTokens_Throws()
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
                () => antiforgery.ValidateTokens(httpContext, new AntiforgeryTokenSet("hello", "world")));
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
            var exception = Assert.Throws<InvalidOperationException>(
                () => antiforgery.GetHtml(httpContext));
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

#if DNX451

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

            // Act
            var inputElement = antiforgery.GetHtml(context.HttpContext);

            // Assert
            Assert.Equal(
                @"<input name=""HtmlEncode[[form-field-name]]"" type=""HtmlEncode[[hidden]]"" " +
                @"value=""HtmlEncode[[serialized-form-token]]"" />",
                inputElement);
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
            context.TokenStore.Setup(o => o.GetCookieToken(context.HttpContext))
                              .Throws(new Exception("should be swallowed"));

            // Setup so that the null cookie token returned is treated as invalid.
            context.TokenGenerator.Setup(o => o.IsCookieTokenValid(null))
                                 .Returns(false);

            // Act
            var inputElement = antiforgery.GetHtml(context.HttpContext);

            // Assert
            Assert.Equal(
                @"<input name=""HtmlEncode[[form-field-name]]"" type=""HtmlEncode[[hidden]]"" " +
                @"value=""HtmlEncode[[serialized-form-token]]"" />",
                inputElement);
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

            // Act
            var inputElement = antiforgery.GetHtml(context.HttpContext);

            // Assert
            Assert.Equal(
                @"<input name=""HtmlEncode[[form-field-name]]"" type=""HtmlEncode[[hidden]]"" " +
                @"value=""HtmlEncode[[serialized-form-token]]"" />",
                inputElement);
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

            // Genreate a new cookie.
            var context = CreateMockContext(options, useOldCookie: false, isOldCookieValid: false);
            var antiforgery = GetAntiforgery(context);

            // Act
            var inputElement = antiforgery.GetHtml(context.HttpContext);

            // Assert
            string xFrameOptions = context.HttpContext.Response.Headers["X-Frame-Options"];
            Assert.Equal(expectedHeaderValue, xFrameOptions);
        }

        [Fact]
        public void GetTokens_ExistingInvalidCookieToken_GeneratesANewCookieTokenAndANewFormToken()
        {
            // Arrange
            // Genreate a new cookie.
            var context = CreateMockContext(
                new AntiforgeryOptions(),
                useOldCookie: false,
                isOldCookieValid: false);
            var antiforgery = GetAntiforgery(context);

            // Act
            var tokenset = antiforgery.GetTokens(context.HttpContext);

            // Assert
            Assert.Equal("serialized-new-cookie-token", tokenset.CookieToken);
            Assert.Equal("serialized-form-token", tokenset.FormToken);
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
            context.TokenSerializer.Setup(o => o.Deserialize("serialized-old-cookie-token"))
                                   .Throws(new Exception("should be swallowed"));

            // Setup so that the null cookie token returned is treated as invalid.
            context.TokenGenerator.Setup(o => o.IsCookieTokenValid(null))
                                 .Returns(false);
            var antiforgery = GetAntiforgery(context);

            // Act
            var tokenset = antiforgery.GetTokens(context.HttpContext);

            // Assert
            Assert.Equal("serialized-new-cookie-token", tokenset.CookieToken);
            Assert.Equal("serialized-form-token", tokenset.FormToken);
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
            Assert.Null(tokenset.CookieToken);
            Assert.Equal("serialized-form-token", tokenset.FormToken);
        }

        [Fact]
        public void ValidateTokens_FromInvalidStrings_Throws()
        {
            // Arrange
            var context = CreateMockContext(new AntiforgeryOptions());

            context.TokenSerializer.Setup(o => o.Deserialize("cookie-token"))
                                   .Returns(context.TestTokenSet.OldCookieToken);
            context.TokenSerializer.Setup(o => o.Deserialize("form-token"))
                                   .Returns(context.TestTokenSet.FormToken);

            context.TokenGenerator.Setup(o => o.ValidateTokens(
                                                context.HttpContext,
                                                context.TestTokenSet.OldCookieToken, context.TestTokenSet.FormToken))
                                 .Throws(new InvalidOperationException("my-message"));
            context.TokenStore = null;
            var antiforgery = GetAntiforgery(context);

            // Act & assert
            var exception = Assert.Throws<InvalidOperationException>(
                    () => antiforgery.ValidateTokens(
                        context.HttpContext, 
                        new AntiforgeryTokenSet("form-token", "cookie-token")));
            Assert.Equal("my-message", exception.Message);
        }

        [Fact]
        public void ValidateTokens_FromValidStrings_TokensValidatedSuccessfully()
        {
            // Arrange
            var context = CreateMockContext(new AntiforgeryOptions());

            context.TokenSerializer.Setup(o => o.Deserialize("cookie-token"))
                                   .Returns(context.TestTokenSet.OldCookieToken);
            context.TokenSerializer.Setup(o => o.Deserialize("form-token"))
                                   .Returns(context.TestTokenSet.FormToken);

            context.TokenGenerator.Setup(o => o.ValidateTokens(
                                                context.HttpContext,
                                                context.TestTokenSet.OldCookieToken, context.TestTokenSet.FormToken))
                                 .Verifiable();
            context.TokenStore = null;
            var antiforgery = GetAntiforgery(context);

            // Act
            antiforgery.ValidateTokens(context.HttpContext, new AntiforgeryTokenSet("form-token", "cookie-token"));

            // Assert
            context.TokenGenerator.Verify();
        }

        [Fact]
        public void ValidateTokens_MissingCookieInTokenSet_Throws()
        {
            // Arrange
            var context = CreateMockContext(new AntiforgeryOptions());
            var antiforgery = GetAntiforgery(context);

            var tokenSet = new AntiforgeryTokenSet("hi", cookieToken: null);


            // Act
            var exception = Assert.Throws<ArgumentException>(
                () => antiforgery.ValidateTokens(context.HttpContext, tokenSet));

            // Assert
            var trimmed = exception.Message.Substring(0, exception.Message.IndexOf(Environment.NewLine));
            Assert.Equal("The cookie token must be provided.", trimmed);
        }

        [Fact]
        public async Task ValidateRequestAsync_FromStore_Failure()
        {
            // Arrange
            var context = CreateMockContext(new AntiforgeryOptions());

            context.TokenGenerator
                .Setup(o => o.ValidateTokens(
                    context.HttpContext,
                    context.TestTokenSet.OldCookieToken,
                    context.TestTokenSet.FormToken))
                .Throws(new InvalidOperationException("my-message"));

            var antiforgery = GetAntiforgery(context);

            // Act & assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                        async () => await antiforgery.ValidateRequestAsync(context.HttpContext));
            Assert.Equal("my-message", exception.Message);
        }

        [Fact]
        public async Task ValidateRequestAsync_FromStore_Success()
        {
            // Arrange
            var context = CreateMockContext(new AntiforgeryOptions());

            context.TokenGenerator
                .Setup(o => o.ValidateTokens(
                    context.HttpContext,
                    context.TestTokenSet.OldCookieToken,
                    context.TestTokenSet.FormToken))
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

            // Genreate a new cookie.
            var context = CreateMockContext(options, useOldCookie: false, isOldCookieValid: false);
            var antiforgery = GetAntiforgery(context);

            // Act
            antiforgery.SetCookieTokenAndHeader(context.HttpContext);

            // Assert
            var xFrameOptions = context.HttpContext.Response.Headers["X-Frame-Options"];
            Assert.Equal(expectedHeaderValue, xFrameOptions);
        }

#endif

        private DefaultAntiforgery GetAntiforgery(
            AntiforgeryOptions options = null,
            IAntiforgeryTokenGenerator tokenGenerator = null,
            IAntiforgeryTokenSerializer tokenSerializer = null,
            IAntiforgeryTokenStore tokenStore = null)
        {
            var optionsManager = new TestOptionsManager();
            if (options != null)
            {
                optionsManager.Options = options;
            }

            return new DefaultAntiforgery(
                antiforgeryOptionsAccessor: optionsManager,
                tokenGenerator: tokenGenerator,
                tokenSerializer: tokenSerializer,
                tokenStore: tokenStore,
                htmlEncoder: new CommonTestEncoder());
        }

        private HttpContext GetHttpContext()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity("some-auth"));
            return httpContext;
        }

#if DNX451

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
            var formToken = testTokenSet.FormToken;
            var mockTokenStore = new Mock<IAntiforgeryTokenStore>(MockBehavior.Strict);
            mockTokenStore.Setup(o => o.GetCookieToken(context))
                          .Returns(oldCookieToken);
            
            mockTokenStore.Setup(o => o.GetRequestTokensAsync(context))
                          .Returns(() => Task.FromResult(new AntiforgeryTokenSet(
                              testTokenSet.FormTokenString,
                              testTokenSet.OldCookieTokenString)));

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
            var formToken = testTokenSet.FormToken;
            var mockSerializer = new Mock<IAntiforgeryTokenSerializer>(MockBehavior.Strict);
            mockSerializer.Setup(o => o.Serialize(formToken))
                          .Returns(testTokenSet.FormTokenString);
            mockSerializer.Setup(o => o.Deserialize(testTokenSet.FormTokenString))
                          .Returns(formToken);
            mockSerializer.Setup(o => o.Deserialize(testTokenSet.OldCookieTokenString))
                          .Returns(oldCookieToken);
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
            var testTokenSet = GetTokenSet(isOldCookieTokenSessionToken: true, isNewCookieSessionToken: true);

            var mockSerializer = GetTokenSerializer(testTokenSet);

            var mockTokenStore = GetTokenStore(httpContext, testTokenSet);

            var mockGenerator = new Mock<IAntiforgeryTokenGenerator>(MockBehavior.Strict);
            mockGenerator
                .Setup(o => o.GenerateFormToken(
                    httpContext,
                    useOldCookie ? testTokenSet.OldCookieToken : testTokenSet.NewCookieToken))
                .Returns(testTokenSet.FormToken);

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

        private TestTokenSet GetTokenSet(bool isOldCookieTokenSessionToken = true, bool isNewCookieSessionToken = true)
        {
            return new TestTokenSet()
            {
                FormToken = new AntiforgeryToken() { IsSessionToken = false },
                FormTokenString = "serialized-form-token",
                OldCookieToken = new AntiforgeryToken() { IsSessionToken = isOldCookieTokenSessionToken },
                OldCookieTokenString = "serialized-old-cookie-token",
                NewCookieToken = new AntiforgeryToken() { IsSessionToken = isNewCookieSessionToken },
                NewCookieTokenString = "serialized-new-cookie-token",
            };
        }

        private class TestTokenSet
        {
            public AntiforgeryToken FormToken { get; set; }

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

#endif

        private class TestOptionsManager : IOptions<AntiforgeryOptions>
        {
            public AntiforgeryOptions Options { get; set; } = new AntiforgeryOptions();

            public AntiforgeryOptions GetNamedOptions(string name)
            {
                throw new NotImplementedException();
            }
        }
    }
}