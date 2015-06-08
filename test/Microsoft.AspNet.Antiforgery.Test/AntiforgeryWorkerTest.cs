// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if DNX451

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Antiforgery.Internal;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.Framework.WebEncoders.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Antiforgery
{
    public class AntiforgeryWorkerTest
    {

        [Fact]
        public async Task ChecksSSL_ValidateAsync_Throws()
        {
            // Arrange
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(o => o.Request.IsHttps)
                           .Returns(false);

            var config = new AntiforgeryOptions()
            {
                RequireSSL = true
            };

            var worker = new AntiforgeryWorker(
                config: config,
                serializer: null,
                tokenStore: null,
                generator: null,
                validator: null,
                htmlEncoder: new CommonTestEncoder());

            // Act & assert
            var ex =
                await
                    Assert.ThrowsAsync<InvalidOperationException>(
                        async () => await worker.ValidateAsync(mockHttpContext.Object));
            Assert.Equal(
             @"The anti-forgery system has the configuration value AntiforgeryOptions.RequireSsl = true, " +
             "but the current request is not an SSL request.",
             ex.Message);
        }

        [Fact]
        public void ChecksSSL_Validate_Throws()
        {
            // Arrange
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(o => o.Request.IsHttps)
                           .Returns(false);

            var config = new AntiforgeryOptions()
            {
                RequireSSL = true
            };

            var worker = new AntiforgeryWorker(
                config: config,
                serializer: null,
                tokenStore: null,
                generator: null,
                validator: null,
                htmlEncoder: new CommonTestEncoder());

            // Act & assert
            var ex = Assert.Throws<InvalidOperationException>(
                         () => worker.Validate(mockHttpContext.Object, cookieToken: null, formToken: null));
            Assert.Equal(
                @"The anti-forgery system has the configuration value AntiforgeryOptions.RequireSsl = true, " +
                "but the current request is not an SSL request.",
                ex.Message);
        }

        [Fact]
        public void ChecksSSL_GetFormInputElement_Throws()
        {
            // Arrange
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(o => o.Request.IsHttps)
                           .Returns(false);

            var config = new AntiforgeryOptions()
            {
                RequireSSL = true
            };

            var worker = new AntiforgeryWorker(
                config: config,
                serializer: null,
                tokenStore: null,
                generator: null,
                validator: null,
                htmlEncoder: new CommonTestEncoder());

            // Act & assert
            var ex = Assert.Throws<InvalidOperationException>(() => worker.GetFormInputElement(mockHttpContext.Object));
            Assert.Equal(
             @"The anti-forgery system has the configuration value AntiforgeryOptions.RequireSsl = true, " +
             "but the current request is not an SSL request.",
             ex.Message);
        }

        [Fact]
        public void ChecksSSL_GetTokens_Throws()
        {
            // Arrange
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(o => o.Request.IsHttps)
                           .Returns(false);

            var config = new AntiforgeryOptions()
            {
                RequireSSL = true
            };

            var worker = new AntiforgeryWorker(
                config: config,
                serializer: null,
                tokenStore: null,
                generator: null,
                validator: null,
                htmlEncoder: new CommonTestEncoder());

            // Act & assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                worker.GetTokens(mockHttpContext.Object, "cookie-token"));
            Assert.Equal(
             @"The anti-forgery system has the configuration value AntiforgeryOptions.RequireSsl = true, " +
             "but the current request is not an SSL request.",
             ex.Message);
        }

        [Fact]
        public void GetFormInputElement_ExistingInvalidCookieToken_GeneratesANewCookieAndAnAntiforgeryToken()
        {
            // Arrange
            var config = new AntiforgeryOptions()
            {
                FormFieldName = "form-field-name"
            };

            // Make sure the existing cookie is invalid.
            var context = GetAntiforgeryWorkerContext(config, isOldCookieValid: false);
            var worker = GetAntiforgeryWorker(context);

            // Act
            var inputElement = worker.GetFormInputElement(context.HttpContext.Object);

            // Assert
            Assert.Equal(
                @"<input name=""HtmlEncode[[form-field-name]]"" type=""HtmlEncode[[hidden]]"" " +
                @"value=""HtmlEncode[[serialized-form-token]]"" />",
                inputElement);
            context.TokenStore.Verify();
        }

        [Fact]
        public void GetFormInputElement_ExistingInvalidCookieToken_SwallowsExceptions()
        {
            // Arrange
            var config = new AntiforgeryOptions()
            {
                FormFieldName = "form-field-name"
            };

            // Make sure the existing cookie is invalid.
            var context = GetAntiforgeryWorkerContext(config, isOldCookieValid: false);
            var worker = GetAntiforgeryWorker(context);

            // This will cause the cookieToken to be null.
            context.TokenStore.Setup(o => o.GetCookieToken(context.HttpContext.Object))
                              .Throws(new Exception("should be swallowed"));

            // Setup so that the null cookie token returned is treated as invalid.
            context.TokenValidator.Setup(o => o.IsCookieTokenValid(null))
                                 .Returns(false);

            // Act
            var inputElement = worker.GetFormInputElement(context.HttpContext.Object);

            // Assert
            Assert.Equal(
                @"<input name=""HtmlEncode[[form-field-name]]"" type=""HtmlEncode[[hidden]]"" " +
                @"value=""HtmlEncode[[serialized-form-token]]"" />",
                inputElement);
            context.TokenStore.Verify();
        }

        [Fact]
        public void GetFormInputElement_ExistingValidCookieToken_GeneratesAnAntiforgeryToken()
        {
            // Arrange
            var options = new AntiforgeryOptions()
            {
                FormFieldName = "form-field-name"
            };

            // Make sure the existing cookie is valid and use the same cookie for the mock Token Provider.
            var context = GetAntiforgeryWorkerContext(options, useOldCookie: true, isOldCookieValid: true);
            var worker = GetAntiforgeryWorker(context);

            // Act
            var inputElement = worker.GetFormInputElement(context.HttpContext.Object);

            // Assert
            Assert.Equal(
                @"<input name=""HtmlEncode[[form-field-name]]"" type=""HtmlEncode[[hidden]]"" " +
                @"value=""HtmlEncode[[serialized-form-token]]"" />",
                inputElement);
        }

        [Theory]
        [InlineData(false, "SAMEORIGIN")]
        [InlineData(true, null)]
        public void GetFormInputElement_AddsXFrameOptionsHeader(bool suppressXFrameOptions, string expectedHeaderValue)
        {
            // Arrange
            var options = new AntiforgeryOptions()
            {
                SuppressXFrameOptionsHeader = suppressXFrameOptions
            };

            // Genreate a new cookie.
            var context = GetAntiforgeryWorkerContext(options, useOldCookie: false, isOldCookieValid: false);
            var worker = GetAntiforgeryWorker(context);

            // Act
            var inputElement = worker.GetFormInputElement(context.HttpContext.Object);

            // Assert
            string xFrameOptions = context.HttpContext.Object.Response.Headers["X-Frame-Options"];
            Assert.Equal(expectedHeaderValue, xFrameOptions);
        }

        [Fact]
        public void GetTokens_ExistingInvalidCookieToken_GeneratesANewCookieTokenAndANewFormToken()
        {
            // Arrange
            // Genreate a new cookie.
            var context = GetAntiforgeryWorkerContext(
                new AntiforgeryOptions(),
                useOldCookie: false,
                isOldCookieValid: false);
            var worker = GetAntiforgeryWorker(context);

            // Act
            var tokenset = worker.GetTokens(context.HttpContext.Object, "serialized-old-cookie-token");

            // Assert
            Assert.Equal("serialized-new-cookie-token", tokenset.CookieToken);
            Assert.Equal("serialized-form-token", tokenset.FormToken);
        }

        [Fact]
        public void GetTokens_ExistingInvalidCookieToken_SwallowsExceptions()
        {
            // Arrange
            // Make sure the existing cookie is invalid.
            var context = GetAntiforgeryWorkerContext(
                new AntiforgeryOptions(),
                useOldCookie: false,
                isOldCookieValid: false);

            // This will cause the cookieToken to be null.
            context.TokenSerializer.Setup(o => o.Deserialize("serialized-old-cookie-token"))
                                   .Throws(new Exception("should be swallowed"));

            // Setup so that the null cookie token returned is treated as invalid.
            context.TokenValidator.Setup(o => o.IsCookieTokenValid(null))
                                 .Returns(false);
            var worker = GetAntiforgeryWorker(context);

            // Act
            var tokenset = worker.GetTokens(context.HttpContext.Object, "serialized-old-cookie-token");

            // Assert
            Assert.Equal("serialized-new-cookie-token", tokenset.CookieToken);
            Assert.Equal("serialized-form-token", tokenset.FormToken);
        }

        [Fact]
        public void GetTokens_ExistingValidCookieToken_GeneratesANewFormToken()
        {
            // Arrange
            var context = GetAntiforgeryWorkerContext(
                new AntiforgeryOptions(),
                useOldCookie: true,
                isOldCookieValid: true);
            context.TokenStore = null;
            var worker = GetAntiforgeryWorker(context);

            // Act
            var tokenset = worker.GetTokens(context.HttpContext.Object, "serialized-old-cookie-token");

            // Assert
            Assert.Null(tokenset.CookieToken);
            Assert.Equal("serialized-form-token", tokenset.FormToken);
        }

        [Fact]
        public void Validate_FromInvalidStrings_Throws()
        {
            // Arrange
            var context = GetAntiforgeryWorkerContext(new AntiforgeryOptions());

            context.TokenSerializer.Setup(o => o.Deserialize("cookie-token"))
                                   .Returns(context.TestTokenSet.OldCookieToken);
            context.TokenSerializer.Setup(o => o.Deserialize("form-token"))
                                   .Returns(context.TestTokenSet.FormToken);

            context.TokenValidator.Setup(o => o.ValidateTokens(
                                                context.HttpContext.Object,
                                                context.HttpContext.Object.User.Identity as ClaimsIdentity,
                                                context.TestTokenSet.OldCookieToken, context.TestTokenSet.FormToken))
                                 .Throws(new InvalidOperationException("my-message"));
            context.TokenStore = null;
            var worker = GetAntiforgeryWorker(context);

            // Act & assert
            var ex =
                Assert.Throws<InvalidOperationException>(
                    () => worker.Validate(context.HttpContext.Object, "cookie-token", "form-token"));
            Assert.Equal("my-message", ex.Message);
        }

        [Fact]
        public void Validate_FromValidStrings_TokensValidatedSuccessfully()
        {
            // Arrange
            var context = GetAntiforgeryWorkerContext(new AntiforgeryOptions());

            context.TokenSerializer.Setup(o => o.Deserialize("cookie-token"))
                                   .Returns(context.TestTokenSet.OldCookieToken);
            context.TokenSerializer.Setup(o => o.Deserialize("form-token"))
                                   .Returns(context.TestTokenSet.FormToken);

            context.TokenValidator.Setup(o => o.ValidateTokens(
                                                context.HttpContext.Object,
                                                context.HttpContext.Object.User.Identity as ClaimsIdentity,
                                                context.TestTokenSet.OldCookieToken, context.TestTokenSet.FormToken))
                                 .Verifiable();
            context.TokenStore = null;
            var worker = GetAntiforgeryWorker(context);

            // Act
            worker.Validate(context.HttpContext.Object, "cookie-token", "form-token");

            // Assert
            context.TokenValidator.Verify();
        }

        [Fact]
        public async Task Validate_FromStore_Failure()
        {
            // Arrange
            var context = GetAntiforgeryWorkerContext(new AntiforgeryOptions());

            context.TokenValidator.Setup(o => o.ValidateTokens(
                                                 context.HttpContext.Object,
                                                 context.HttpContext.Object.User.Identity as ClaimsIdentity,
                                                 context.TestTokenSet.OldCookieToken, context.TestTokenSet.FormToken))
                                  .Throws(new InvalidOperationException("my-message"));
            context.TokenSerializer = null;
            var worker = GetAntiforgeryWorker(context);

            // Act & assert
            var ex =
                await
                    Assert.ThrowsAsync<InvalidOperationException>(
                        async () => await worker.ValidateAsync(context.HttpContext.Object));
            Assert.Equal("my-message", ex.Message);
        }

        [Fact]
        public async Task Validate_FromStore_Success()
        {
            // Arrange
            var context = GetAntiforgeryWorkerContext(new AntiforgeryOptions());

            context.TokenValidator.Setup(o => o.ValidateTokens(
                                                 context.HttpContext.Object,
                                                 context.HttpContext.Object.User.Identity as ClaimsIdentity,
                                                 context.TestTokenSet.OldCookieToken, context.TestTokenSet.FormToken))
                                .Verifiable();
            context.TokenSerializer = null;
            var worker = GetAntiforgeryWorker(context);

            // Act
            await worker.ValidateAsync(context.HttpContext.Object);

            // Assert
            context.TokenValidator.Verify();
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
            var context = GetAntiforgeryWorkerContext(options, useOldCookie: false, isOldCookieValid: false);
            var worker = GetAntiforgeryWorker(context);

            // Act
            worker.SetCookieTokenAndHeader(context.HttpContext.Object);

            // Assert
            var xFrameOptions = context.HttpContext.Object.Response.Headers["X-Frame-Options"];
            Assert.Equal(expectedHeaderValue, xFrameOptions);
        }

        private AntiforgeryWorker GetAntiforgeryWorker(AntiforgeryWorkerContext context)
        {
            return new AntiforgeryWorker(
                 config: context.Options,
                 serializer: context.TokenSerializer != null ? context.TokenSerializer.Object : null,
                 tokenStore: context.TokenStore != null ? context.TokenStore.Object : null,
                 generator: context.TokenGenerator != null ? context.TokenGenerator.Object : null,
                 validator: context.TokenValidator != null ? context.TokenValidator.Object : null,
                 htmlEncoder: new CommonTestEncoder());
        }

        private Mock<HttpContext> GetHttpContext(bool setupResponse = true)
        {
            var identity = new ClaimsIdentity("some-auth");
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(o => o.User)
                           .Returns(new ClaimsPrincipal(identity));

            if (setupResponse)
            {
                var mockResponse = new Mock<HttpResponse>();
                mockResponse.Setup(r => r.Headers)
                            .Returns(new HeaderDictionary(new Dictionary<string, string[]>()));
                mockHttpContext.Setup(o => o.Response)
                               .Returns(mockResponse.Object);
            }

            return mockHttpContext;
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
            mockTokenStore.Setup(o => o.GetFormTokenAsync(context))
                          .Returns(Task.FromResult(formToken));

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
                          .Returns("serialized-form-token");
            mockSerializer.Setup(o => o.Deserialize("serialized-old-cookie-token"))
                          .Returns(oldCookieToken);
            mockSerializer.Setup(o => o.Serialize(newCookieToken))
                          .Returns("serialized-new-cookie-token");
            return mockSerializer;
        }

        private TestTokenSet GetTokenSet(bool isOldCookieTokenSessionToken = true, bool isNewCookieSessionToken = true)
        {
            return new TestTokenSet()
            {
                FormToken = new AntiforgeryToken() { IsSessionToken = false },
                OldCookieToken = new AntiforgeryToken() { IsSessionToken = isOldCookieTokenSessionToken },
                NewCookieToken = new AntiforgeryToken() { IsSessionToken = isNewCookieSessionToken },
            };
        }

        private AntiforgeryWorkerContext GetAntiforgeryWorkerContext(
            AntiforgeryOptions config,
            bool useOldCookie = false,
            bool isOldCookieValid = true)
        {
            // Arrange
            var mockHttpContext = GetHttpContext();
            var testTokenSet = GetTokenSet(isOldCookieTokenSessionToken: true, isNewCookieSessionToken: true);

            var mockSerializer = GetTokenSerializer(testTokenSet);

            var mockTokenStore = GetTokenStore(mockHttpContext.Object, testTokenSet);

            var mockGenerator = new Mock<IAntiforgeryTokenGenerator>(MockBehavior.Strict);
            mockGenerator
                .Setup(o => o.GenerateFormToken(
                    mockHttpContext.Object,
                    mockHttpContext.Object.User.Identity as ClaimsIdentity,
                    useOldCookie ? testTokenSet.OldCookieToken : testTokenSet.NewCookieToken))
                .Returns(testTokenSet.FormToken);

            mockGenerator
                .Setup(o => o.GenerateCookieToken())
                .Returns(useOldCookie ? testTokenSet.OldCookieToken : testTokenSet.NewCookieToken);

            var mockValidator = new Mock<IAntiforgeryTokenValidator>(MockBehavior.Strict);
            mockValidator
                .Setup(o => o.IsCookieTokenValid(testTokenSet.OldCookieToken))
                .Returns(isOldCookieValid);

            mockValidator
                .Setup(o => o.IsCookieTokenValid(testTokenSet.NewCookieToken))
                .Returns(!isOldCookieValid);

            return new AntiforgeryWorkerContext()
            {
                Options = config,
                HttpContext = mockHttpContext,
                TokenGenerator = mockGenerator,
                TokenValidator = mockValidator,
                TokenSerializer = mockSerializer,
                TokenStore = mockTokenStore,
                TestTokenSet = testTokenSet
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

        private class AntiforgeryWorkerContext
        {
            public AntiforgeryOptions Options { get; set; }

            public TestTokenSet TestTokenSet { get; set; }

            public Mock<HttpContext> HttpContext { get; set; }

            public Mock<IAntiforgeryTokenGenerator> TokenGenerator { get; set; }

            public Mock<IAntiforgeryTokenValidator> TokenValidator { get; set; }

            public Mock<IAntiforgeryTokenStore> TokenStore { get; set; }

            public Mock<IAntiforgeryTokenSerializer> TokenSerializer { get; set; }
        }
    }
}

#endif