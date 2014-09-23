// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.PipelineCore.Collections;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test
{
    public class AntiForgeryWorkerTest
    {

        [Fact]
        public async Task ChecksSSL_ValidateAsync_Throws()
        {
            // Arrange
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(o => o.Request.IsSecure)
                           .Returns(false);

            var config = new AntiForgeryOptions()
            {
                RequireSSL = true
            };

            var worker = new AntiForgeryWorker(
                config: config,
                serializer: null,
                tokenStore: null,
                generator: null,
                validator: null);

            // Act & assert
            var ex =
                await
                    Assert.ThrowsAsync<InvalidOperationException>(
                        async () => await worker.ValidateAsync(mockHttpContext.Object));
            Assert.Equal(
             @"The anti-forgery system has the configuration value AntiForgeryOptions.RequireSsl = true, " +
             "but the current request is not an SSL request.",
             ex.Message);
        }

        [Fact]
        public void ChecksSSL_Validate_Throws()
        {
            // Arrange
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(o => o.Request.IsSecure)
                           .Returns(false);

            var config = new AntiForgeryOptions()
            {
                RequireSSL = true
            };

            var worker = new AntiForgeryWorker(
                config: config,
                serializer: null,
                tokenStore: null,
                generator: null,
                validator: null);

            // Act & assert
            var ex = Assert.Throws<InvalidOperationException>(
                         () => worker.Validate(mockHttpContext.Object, cookieToken: null, formToken: null));
            Assert.Equal(
                @"The anti-forgery system has the configuration value AntiForgeryOptions.RequireSsl = true, " +
                "but the current request is not an SSL request.",
                ex.Message);
        }

        [Fact]
        public void ChecksSSL_GetFormInputElement_Throws()
        {
            // Arrange
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(o => o.Request.IsSecure)
                           .Returns(false);

            var config = new AntiForgeryOptions()
            {
                RequireSSL = true
            };

            var worker = new AntiForgeryWorker(
                config: config,
                serializer: null,
                tokenStore: null,
                generator: null,
                validator: null);

            // Act & assert
            var ex = Assert.Throws<InvalidOperationException>(() => worker.GetFormInputElement(mockHttpContext.Object));
            Assert.Equal(
             @"The anti-forgery system has the configuration value AntiForgeryOptions.RequireSsl = true, " +
             "but the current request is not an SSL request.",
             ex.Message);
        }

        [Fact]
        public void ChecksSSL_GetTokens_Throws()
        {
            // Arrange
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(o => o.Request.IsSecure)
                           .Returns(false);

            var config = new AntiForgeryOptions()
            {
                RequireSSL = true
            };

            var worker = new AntiForgeryWorker(
                config: config,
                serializer: null,
                tokenStore: null,
                generator: null,
                validator: null);

            // Act & assert
            var ex = Assert.Throws<InvalidOperationException>(() => worker.GetTokens(mockHttpContext.Object, "cookie-token"));
            Assert.Equal(
             @"The anti-forgery system has the configuration value AntiForgeryOptions.RequireSsl = true, " +
             "but the current request is not an SSL request.",
             ex.Message);
        }

        [Fact]
        public void GetFormInputElement_ExistingInvalidCookieToken_GeneratesANewCookieAndAnAntiForgeryToken()
        {
            // Arrange
            var config = new AntiForgeryOptions()
            {
                FormFieldName = "form-field-name"
            };

            // Make sure the existing cookie is invalid.
            var context = GetAntiForgeryWorkerContext(config, isOldCookieValid: false);
            var worker = GetAntiForgeryWorker(context);

            // Act
            var inputElement = worker.GetFormInputElement(context.HttpContext.Object);

            // Assert
            Assert.Equal(@"<input name=""form-field-name"" type=""hidden"" value=""serialized-form-token"" />",
                inputElement.ToString(TagRenderMode.SelfClosing));
            context.TokenStore.Verify();
        }

        [Fact]
        public void GetFormInputElement_ExistingInvalidCookieToken_SwallowsExceptions()
        {
            // Arrange
            var config = new AntiForgeryOptions()
            {
                FormFieldName = "form-field-name"
            };

            // Make sure the existing cookie is invalid.
            var context = GetAntiForgeryWorkerContext(config, isOldCookieValid: false);
            var worker = GetAntiForgeryWorker(context);

            // This will cause the cookieToken to be null.           
            context.TokenStore.Setup(o => o.GetCookieToken(context.HttpContext.Object))
                              .Throws(new Exception("should be swallowed"));

            // Setup so that the null cookie token returned is treated as invalid.
            context.TokenProvider.Setup(o => o.IsCookieTokenValid(null))
                                 .Returns(false);

            // Act
            var inputElement = worker.GetFormInputElement(context.HttpContext.Object);

            // Assert
            Assert.Equal(@"<input name=""form-field-name"" type=""hidden"" value=""serialized-form-token"" />",
                inputElement.ToString(TagRenderMode.SelfClosing));
            context.TokenStore.Verify();
        }

        [Fact]
        public void GetFormInputElement_ExistingValidCookieToken_GeneratesAnAntiForgeryToken()
        {
            // Arrange
            var options = new AntiForgeryOptions()
            {
                FormFieldName = "form-field-name"
            };

            // Make sure the existing cookie is valid and use the same cookie for the mock Token Provider.
            var context = GetAntiForgeryWorkerContext(options, useOldCookie: true, isOldCookieValid: true);
            var worker = GetAntiForgeryWorker(context);

            // Act
            var inputElement = worker.GetFormInputElement(context.HttpContext.Object);

            // Assert
            Assert.Equal(@"<input name=""form-field-name"" type=""hidden"" value=""serialized-form-token"" />",
                inputElement.ToString(TagRenderMode.SelfClosing));
        }

        [Theory]
        [InlineData(false, "SAMEORIGIN")]
        [InlineData(true, null)]
        public void GetFormInputElement_AddsXFrameOptionsHeader(bool suppressXFrameOptions, string expectedHeaderValue)
        {
            // Arrange
            var options = new AntiForgeryOptions()
            {
                SuppressXFrameOptionsHeader = suppressXFrameOptions
            };

            // Genreate a new cookie.
            var context = GetAntiForgeryWorkerContext(options, useOldCookie: false, isOldCookieValid: false);
            var worker = GetAntiForgeryWorker(context);

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
            var context = GetAntiForgeryWorkerContext(new AntiForgeryOptions(), useOldCookie: false, isOldCookieValid: false);
            var worker = GetAntiForgeryWorker(context);

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
            var context = GetAntiForgeryWorkerContext(new AntiForgeryOptions(), useOldCookie: false, isOldCookieValid: false);

            // This will cause the cookieToken to be null.           
            context.TokenSerializer.Setup(o => o.Deserialize("serialized-old-cookie-token"))
                                   .Throws(new Exception("should be swallowed"));

            // Setup so that the null cookie token returned is treated as invalid.
            context.TokenProvider.Setup(o => o.IsCookieTokenValid(null))
                                 .Returns(false);
            var worker = GetAntiForgeryWorker(context);

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
            var context = GetAntiForgeryWorkerContext(new AntiForgeryOptions(), useOldCookie: true, isOldCookieValid: true);
            context.TokenStore = null;
            var worker = GetAntiForgeryWorker(context);

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
            var context = GetAntiForgeryWorkerContext(new AntiForgeryOptions());

            context.TokenSerializer.Setup(o => o.Deserialize("cookie-token"))
                                   .Returns(context.TestTokenSet.OldCookieToken);
            context.TokenSerializer.Setup(o => o.Deserialize("form-token"))
                                   .Returns(context.TestTokenSet.FormToken);

            context.TokenProvider.Setup(o => o.ValidateTokens(
                                                context.HttpContext.Object,
                                                context.HttpContext.Object.User.Identity as ClaimsIdentity,
                                                context.TestTokenSet.OldCookieToken, context.TestTokenSet.FormToken))
                                 .Throws(new InvalidOperationException("my-message"));
            context.TokenStore = null;
            var worker = GetAntiForgeryWorker(context);

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
            var context = GetAntiForgeryWorkerContext(new AntiForgeryOptions());

            context.TokenSerializer.Setup(o => o.Deserialize("cookie-token"))
                                   .Returns(context.TestTokenSet.OldCookieToken);
            context.TokenSerializer.Setup(o => o.Deserialize("form-token"))
                                   .Returns(context.TestTokenSet.FormToken);

            context.TokenProvider.Setup(o => o.ValidateTokens(
                                                context.HttpContext.Object,
                                                context.HttpContext.Object.User.Identity as ClaimsIdentity,
                                                context.TestTokenSet.OldCookieToken, context.TestTokenSet.FormToken))
                                 .Verifiable();
            context.TokenStore = null;
            var worker = GetAntiForgeryWorker(context);

            // Act
            worker.Validate(context.HttpContext.Object, "cookie-token", "form-token");

            // Assert
            context.TokenProvider.Verify();
        }

        [Fact]
        public async Task Validate_FromStore_Failure()
        {
            // Arrange
            var context = GetAntiForgeryWorkerContext(new AntiForgeryOptions());

            context.TokenProvider.Setup(o => o.ValidateTokens(
                                                 context.HttpContext.Object,
                                                 context.HttpContext.Object.User.Identity as ClaimsIdentity,
                                                 context.TestTokenSet.OldCookieToken, context.TestTokenSet.FormToken))
                                  .Throws(new InvalidOperationException("my-message"));
            context.TokenSerializer = null;
            var worker = GetAntiForgeryWorker(context);

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
            var context = GetAntiForgeryWorkerContext(new AntiForgeryOptions());

            context.TokenProvider.Setup(o => o.ValidateTokens(
                                                 context.HttpContext.Object,
                                                 context.HttpContext.Object.User.Identity as ClaimsIdentity,
                                                 context.TestTokenSet.OldCookieToken, context.TestTokenSet.FormToken))
                                .Verifiable();
            context.TokenSerializer = null;
            var worker = GetAntiForgeryWorker(context);

            // Act
            await worker.ValidateAsync(context.HttpContext.Object);

            // Assert
            context.TokenProvider.Verify();
        }

        private AntiForgeryWorker GetAntiForgeryWorker(AntiForgeryWorkerContext context)
        {
            return new AntiForgeryWorker(
                 config: context.Options,
                 serializer: context.TokenSerializer != null ? context.TokenSerializer.Object : null,
                 tokenStore: context.TokenStore != null ? context.TokenStore.Object : null,
                 generator: context.TokenProvider != null ? context.TokenProvider.Object : null,
                 validator: context.TokenProvider != null ? context.TokenProvider.Object : null);
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

        private Mock<ITokenProvider> GetTokenProvider(HttpContext context, TestTokenSet testTokenSet, bool useOldCookie, bool isOldCookieValid = true, bool isNewCookieValid = true)
        {
            var oldCookieToken = testTokenSet.OldCookieToken;
            var newCookieToken = testTokenSet.NewCookieToken;
            var formToken = testTokenSet.FormToken;
            var mockValidator = new Mock<ITokenProvider>(MockBehavior.Strict);
            mockValidator.Setup(o => o.GenerateFormToken(context, context.User.Identity as ClaimsIdentity, useOldCookie ? oldCookieToken : newCookieToken))
                         .Returns(formToken);
            mockValidator.Setup(o => o.IsCookieTokenValid(oldCookieToken))
                         .Returns(isOldCookieValid);
            mockValidator.Setup(o => o.IsCookieTokenValid(newCookieToken))
                         .Returns(isNewCookieValid);

            mockValidator.Setup(o => o.GenerateCookieToken())
                         .Returns(useOldCookie ? oldCookieToken : newCookieToken);

            return mockValidator;
        }

        private Mock<ITokenStore> GetTokenStore(HttpContext context, TestTokenSet testTokenSet, bool saveNewCookie = true)
        {
            var oldCookieToken = testTokenSet.OldCookieToken;
            var formToken = testTokenSet.FormToken;
            var mockTokenStore = new Mock<ITokenStore>(MockBehavior.Strict);
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

        private Mock<IAntiForgeryTokenSerializer> GetTokenSerializer(TestTokenSet testTokenSet)
        {
            var oldCookieToken = testTokenSet.OldCookieToken;
            var newCookieToken = testTokenSet.NewCookieToken;
            var formToken = testTokenSet.FormToken;
            var mockSerializer = new Mock<IAntiForgeryTokenSerializer>(MockBehavior.Strict);
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
                FormToken = new AntiForgeryToken() { IsSessionToken = false },
                OldCookieToken = new AntiForgeryToken() { IsSessionToken = isOldCookieTokenSessionToken },
                NewCookieToken = new AntiForgeryToken() { IsSessionToken = isNewCookieSessionToken },
            };
        }

        private AntiForgeryWorkerContext GetAntiForgeryWorkerContext(AntiForgeryOptions config, bool useOldCookie = false, bool isOldCookieValid = true)
        {
            // Arrange
            var mockHttpContext = GetHttpContext();
            var testTokenSet = GetTokenSet(isOldCookieTokenSessionToken: true, isNewCookieSessionToken: true);

            var mockSerializer = GetTokenSerializer(testTokenSet);

            var mockTokenStore = GetTokenStore(mockHttpContext.Object, testTokenSet);
            var mockTokenProvider = GetTokenProvider(mockHttpContext.Object, testTokenSet, useOldCookie: useOldCookie, isOldCookieValid: isOldCookieValid);

            return new AntiForgeryWorkerContext()
            {
                Options = config,
                HttpContext = mockHttpContext,
                TokenProvider = mockTokenProvider,
                TokenSerializer = mockSerializer,
                TokenStore = mockTokenStore,
                TestTokenSet = testTokenSet
            };
        }

        private class TestTokenSet
        {
            public AntiForgeryToken FormToken { get; set; }
            public string FormTokenString { get; set; }
            public AntiForgeryToken OldCookieToken { get; set; }
            public string OldCookieTokenString { get; set; }
            public AntiForgeryToken NewCookieToken { get; set; }
            public string NewCookieTokenString { get; set; }
        }

        private class AntiForgeryWorkerContext
        {
            public AntiForgeryOptions Options { get; set; }

            public TestTokenSet TestTokenSet { get; set; }

            public Mock<HttpContext> HttpContext { get; set; }

            public Mock<ITokenProvider> TokenProvider { get; set; }

            public Mock<ITokenStore> TokenStore { get; set; }

            public Mock<IAntiForgeryTokenSerializer> TokenSerializer { get; set; }
        }
    }
}
