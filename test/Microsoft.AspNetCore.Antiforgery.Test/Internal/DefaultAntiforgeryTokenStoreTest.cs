// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Antiforgery.Internal
{
    public class DefaultAntiforgeryTokenStoreTest
    {
        private static readonly ObjectPool<AntiforgerySerializationContext> _pool =
            new DefaultObjectPoolProvider().Create(new AntiforgerySerializationContextPooledObjectPolicy());
        private readonly string _cookieName = "cookie-name";

        [Fact]
        public void GetCookieToken_CookieDoesNotExist_ReturnsNull()
        {
            // Arrange
            var httpContext = GetHttpContext(new RequestCookieCollection());
            var options = new AntiforgeryOptions()
            {
                CookieName = _cookieName
            };

            var tokenStore = new DefaultAntiforgeryTokenStore(
                optionsAccessor: new TestOptionsManager(options),
                tokenSerializer: Mock.Of<IAntiforgeryTokenSerializer>());

            // Act
            var token = tokenStore.GetCookieToken(httpContext);

            // Assert
            Assert.Null(token);
        }

        [Fact]
        public void GetCookieToken_CookieIsMissingInRequest_LooksUpCookieInAntiforgeryContext()
        {
            // Arrange
            var contextAccessor = new DefaultAntiforgeryContextAccessor();
            var httpContext = GetHttpContext(_cookieName, string.Empty, contextAccessor);

            // add a cookie explicitly.
            var cookie = new AntiforgeryToken();
            contextAccessor.Value = new AntiforgeryContext() { CookieToken = cookie };

            var options = new AntiforgeryOptions
            {
                CookieName = _cookieName
            };

            var tokenStore = new DefaultAntiforgeryTokenStore(
                optionsAccessor: new TestOptionsManager(options),
                tokenSerializer: Mock.Of<IAntiforgeryTokenSerializer>());

            // Act
            var token = tokenStore.GetCookieToken(httpContext);

            // Assert
            Assert.Equal(cookie, token);
        }

        [Fact]
        public void GetCookieToken_CookieIsEmpty_ReturnsNull()
        {
            // Arrange
            var httpContext = GetHttpContext(_cookieName, string.Empty);
            var options = new AntiforgeryOptions()
            {
                CookieName = _cookieName
            };

            var tokenStore = new DefaultAntiforgeryTokenStore(
                optionsAccessor: new TestOptionsManager(options),
                tokenSerializer: Mock.Of<IAntiforgeryTokenSerializer>());

            // Act
            var token = tokenStore.GetCookieToken(httpContext);

            // Assert
            Assert.Null(token);
        }

        [Fact]
        public void GetCookieToken_CookieIsInvalid_PropagatesException()
        {
            // Arrange
            var httpContext = GetHttpContext(_cookieName, "invalid-value");

            var expectedException = new AntiforgeryValidationException("some exception");
            var mockSerializer = new Mock<IAntiforgeryTokenSerializer>();
            mockSerializer
                .Setup(o => o.Deserialize("invalid-value"))
                .Throws(expectedException);

            var options = new AntiforgeryOptions()
            {
                CookieName = _cookieName
            };

            var tokenStore = new DefaultAntiforgeryTokenStore(
                optionsAccessor: new TestOptionsManager(options),
                tokenSerializer: mockSerializer.Object);

            // Act & assert
            var ex = Assert.Throws<AntiforgeryValidationException>(() => tokenStore.GetCookieToken(httpContext));
            Assert.Same(expectedException, ex);
        }

        [Fact]
        public void GetCookieToken_CookieIsValid_ReturnsToken()
        {
            // Arrange
            var expectedToken = new AntiforgeryToken();
            var httpContext = GetHttpContext(_cookieName, "valid-value");

            var mockSerializer = new Mock<IAntiforgeryTokenSerializer>();
            mockSerializer
                .Setup(o => o.Deserialize("valid-value"))
                .Returns(expectedToken);

            var options = new AntiforgeryOptions()
            {
                CookieName = _cookieName
            };

            var tokenStore = new DefaultAntiforgeryTokenStore(
                optionsAccessor: new TestOptionsManager(options),
                tokenSerializer: mockSerializer.Object);

            // Act
            var token = tokenStore.GetCookieToken(httpContext);

            // Assert
            Assert.Same(expectedToken, token);
        }

        [Fact]
        public async Task GetRequestTokens_CookieIsEmpty_Throws()
        {
            // Arrange
            var httpContext = GetHttpContext(new RequestCookieCollection());
            httpContext.Request.Form = FormCollection.Empty;

            var options = new AntiforgeryOptions()
            {
                CookieName = "cookie-name",
                FormFieldName = "form-field-name",
            };

            var tokenStore = new DefaultAntiforgeryTokenStore(
                optionsAccessor: new TestOptionsManager(options),
                tokenSerializer: Mock.Of<IAntiforgeryTokenSerializer>());

            // Act
            var exception = await Assert.ThrowsAsync<AntiforgeryValidationException>(
                async () => await tokenStore.GetRequestTokensAsync(httpContext));

            // Assert
            Assert.Equal("The required antiforgery cookie \"cookie-name\" is not present.", exception.Message);
        }

        [Fact]
        public async Task GetRequestTokens_NonFormContentType_HeaderDisabled_Throws()
        {
            // Arrange
            var httpContext = GetHttpContext("cookie-name", "cookie-value");
            httpContext.Request.ContentType = "application/json";

            // Will not be accessed
            httpContext.Request.Form = null;

            var options = new AntiforgeryOptions()
            {
                CookieName = "cookie-name",
                FormFieldName = "form-field-name",
                HeaderName = null,
            };

            var tokenStore = new DefaultAntiforgeryTokenStore(
                optionsAccessor: new TestOptionsManager(options),
                tokenSerializer: new DefaultAntiforgeryTokenSerializer(new EphemeralDataProtectionProvider(), _pool));

            // Act
            var exception = await Assert.ThrowsAsync<AntiforgeryValidationException>(
                async () => await tokenStore.GetRequestTokensAsync(httpContext));

            // Assert
            Assert.Equal("The required antiforgery form field \"form-field-name\" is not present.", exception.Message);
        }

        [Fact]
        public async Task GetRequestTokens_FormContentType_FallbackHeaderToken()
        {
            // Arrange
            var httpContext = GetHttpContext("cookie-name", "cookie-value");
            httpContext.Request.ContentType = "application/x-www-form-urlencoded";
            httpContext.Request.Form = FormCollection.Empty;
            httpContext.Request.Headers.Add("header-name", "header-value");

            var options = new AntiforgeryOptions()
            {
                CookieName = "cookie-name",
                FormFieldName = "form-field-name",
                HeaderName = "header-name",
            };

            var tokenStore = new DefaultAntiforgeryTokenStore(
                optionsAccessor: new TestOptionsManager(options),
                tokenSerializer: new DefaultAntiforgeryTokenSerializer(new EphemeralDataProtectionProvider(), _pool));

            // Act
            var tokens = await tokenStore.GetRequestTokensAsync(httpContext);

            // Assert
            Assert.Equal("cookie-value", tokens.CookieToken);
            Assert.Equal("header-value", tokens.RequestToken);
        }

        [Fact]
        public async Task GetRequestTokens_NonFormContentType_UsesHeaderToken()
        {
            // Arrange
            var httpContext = GetHttpContext("cookie-name", "cookie-value");
            httpContext.Request.ContentType = "application/json";
            httpContext.Request.Headers.Add("header-name", "header-value");

            // Will not be accessed
            httpContext.Request.Form = null;

            var options = new AntiforgeryOptions()
            {
                CookieName = "cookie-name",
                FormFieldName = "form-field-name",
                HeaderName = "header-name",
            };

            var tokenStore = new DefaultAntiforgeryTokenStore(
                optionsAccessor: new TestOptionsManager(options),
                tokenSerializer: new DefaultAntiforgeryTokenSerializer(new EphemeralDataProtectionProvider(), _pool));

            // Act
            var tokens = await tokenStore.GetRequestTokensAsync(httpContext);

            // Assert
            Assert.Equal("cookie-value", tokens.CookieToken);
            Assert.Equal("header-value", tokens.RequestToken);
        }

        [Fact]
        public async Task GetRequestTokens_NonFormContentType_UsesHeaderToken_ThrowsOnMissingValue()
        {
            // Arrange
            var httpContext = GetHttpContext("cookie-name", "cookie-value");
            httpContext.Request.ContentType = "application/json";

            // Will not be accessed
            httpContext.Request.Form = null;

            var options = new AntiforgeryOptions()
            {
                CookieName = "cookie-name",
                FormFieldName = "form-field-name",
                HeaderName = "header-name",
            };

            var tokenStore = new DefaultAntiforgeryTokenStore(
                optionsAccessor: new TestOptionsManager(options),
                tokenSerializer: new DefaultAntiforgeryTokenSerializer(new EphemeralDataProtectionProvider(), _pool));

            // Act
            var exception = await Assert.ThrowsAsync<AntiforgeryValidationException>(
                async () => await tokenStore.GetRequestTokensAsync(httpContext));

            // Assert
            Assert.Equal("The required antiforgery header value \"header-name\" is not present.", exception.Message);
        }

        [Fact]
        public async Task GetRequestTokens_BothFieldsEmpty_Throws()
        {
            // Arrange
            var httpContext = GetHttpContext("cookie-name", "cookie-value");
            httpContext.Request.ContentType = "application/x-www-form-urlencoded";
            httpContext.Request.Form = FormCollection.Empty;

            var options = new AntiforgeryOptions()
            {
                CookieName = "cookie-name",
                FormFieldName = "form-field-name",
                HeaderName = "header-name",
            };

            var tokenStore = new DefaultAntiforgeryTokenStore(
                optionsAccessor: new TestOptionsManager(options),
                tokenSerializer: Mock.Of<IAntiforgeryTokenSerializer>());

            // Act
            var exception = await Assert.ThrowsAsync<AntiforgeryValidationException>(
                async () => await tokenStore.GetRequestTokensAsync(httpContext));

            // Assert
            Assert.Equal(
                "The required antiforgery request token was not provided in either form field \"form-field-name\" " +
                "or header value \"header-name\".",
                exception.Message);
        }

        [Fact]
        public async Task GetFormToken_FormFieldIsValid_ReturnsToken()
        {
            // Arrange
            var httpContext = GetHttpContext("cookie-name", "cookie-value");
            httpContext.Request.ContentType = "application/x-www-form-urlencoded";
            httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>
            {
                { "form-field-name", "form-value" },
            });
            httpContext.Request.Headers.Add("header-name", "header-value"); // form value has priority.

            var options = new AntiforgeryOptions()
            {
                CookieName = "cookie-name",
                FormFieldName = "form-field-name",
                HeaderName = "header-name",
            };

            var tokenStore = new DefaultAntiforgeryTokenStore(
                optionsAccessor: new TestOptionsManager(options),
                tokenSerializer: Mock.Of<IAntiforgeryTokenSerializer>());

            // Act
            var tokens = await tokenStore.GetRequestTokensAsync(httpContext);

            // Assert
            Assert.Equal("cookie-value", tokens.CookieToken);
            Assert.Equal("form-value", tokens.RequestToken);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, null)]
        public void SaveCookieToken(bool requireSsl, bool? expectedCookieSecureFlag)
        {
            // Arrange
            var token = new AntiforgeryToken();
            bool defaultCookieSecureValue = expectedCookieSecureFlag ?? false; // pulled from config; set by ctor
            var cookies = new MockResponseCookieCollection();

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext
                .Setup(o => o.Response.Cookies)
                .Returns(cookies);

            var contextAccessor = new DefaultAntiforgeryContextAccessor();
            mockHttpContext
                .SetupGet(o => o.RequestServices)
                .Returns(GetServiceProvider(contextAccessor));

            var mockSerializer = new Mock<IAntiforgeryTokenSerializer>();
            mockSerializer
                .Setup(o => o.Serialize(token))
                .Returns("serialized-value");

            var options = new AntiforgeryOptions()
            {
                CookieName = _cookieName,
                RequireSsl = requireSsl
            };

            var tokenStore = new DefaultAntiforgeryTokenStore(
                optionsAccessor: new TestOptionsManager(options),
                tokenSerializer: mockSerializer.Object);

            // Act
            tokenStore.SaveCookieToken(mockHttpContext.Object, token);

            // Assert
            Assert.Equal(1, cookies.Count);
            Assert.NotNull(contextAccessor.Value.CookieToken);
            Assert.NotNull(cookies);
            Assert.Equal(_cookieName, cookies.Key);
            Assert.Equal("serialized-value", cookies.Value);
            Assert.True(cookies.Options.HttpOnly);
            Assert.Equal(defaultCookieSecureValue, cookies.Options.Secure);
        }

        private HttpContext GetHttpContext(
            string cookieName,
            string cookieValue,
            IAntiforgeryContextAccessor contextAccessor = null)
        {
            var cookies = new RequestCookieCollection(new Dictionary<string, string>
            {
                { cookieName, cookieValue },
            });

            return GetHttpContext(cookies, contextAccessor);
        }

        private HttpContext GetHttpContext(
            IRequestCookieCollection cookies,
            IAntiforgeryContextAccessor contextAccessor = null)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Cookies = cookies;

            contextAccessor = contextAccessor ?? new DefaultAntiforgeryContextAccessor();
            httpContext.RequestServices = GetServiceProvider(contextAccessor);

            return httpContext;
        }

        private static IServiceProvider GetServiceProvider(IAntiforgeryContextAccessor contextAccessor)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(contextAccessor);
            return serviceCollection.BuildServiceProvider();
        }

        private class MockResponseCookieCollection : IResponseCookies
        {
            public string Key { get; set; }
            public string Value { get; set; }
            public CookieOptions Options { get; set; }
            public int Count { get; set; }

            public void Append(string key, string value, CookieOptions options)
            {
                this.Key = key;
                this.Value = value;
                this.Options = options;
                this.Count++;
            }

            public void Append(string key, string value)
            {
                throw new NotImplementedException();
            }

            public void Delete(string key, CookieOptions options)
            {
                throw new NotImplementedException();
            }

            public void Delete(string key)
            {
                throw new NotImplementedException();
            }
        }
    }
}
