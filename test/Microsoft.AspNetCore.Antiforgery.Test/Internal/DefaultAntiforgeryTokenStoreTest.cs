// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Antiforgery.Internal
{
    public class DefaultAntiforgeryTokenStoreTest
    {
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

            var tokenStore = new DefaultAntiforgeryTokenStore(new TestOptionsManager(options));

            // Act
            var token = tokenStore.GetCookieToken(httpContext);

            // Assert
            Assert.Null(token);
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

            var tokenStore = new DefaultAntiforgeryTokenStore(new TestOptionsManager(options));

            // Act
            var token = tokenStore.GetCookieToken(httpContext);

            // Assert
            Assert.Null(token);
        }

        [Fact]
        public void GetCookieToken_CookieIsNotEmpty_ReturnsToken()
        {
            // Arrange
            var expectedToken = "valid-value";
            var httpContext = GetHttpContext(_cookieName, expectedToken);

            var options = new AntiforgeryOptions()
            {
                CookieName = _cookieName
            };

            var tokenStore = new DefaultAntiforgeryTokenStore(new TestOptionsManager(options));

            // Act
            var token = tokenStore.GetCookieToken(httpContext);

            // Assert
            Assert.Equal(expectedToken, token);
        }

        [Fact]
        public async Task GetRequestTokens_CookieIsEmpty_ReturnsNullTokens()
        {
            // Arrange
            var httpContext = GetHttpContext(new RequestCookieCollection());
            httpContext.Request.Form = FormCollection.Empty;

            var options = new AntiforgeryOptions()
            {
                CookieName = "cookie-name",
                FormFieldName = "form-field-name",
            };

            var tokenStore = new DefaultAntiforgeryTokenStore(new TestOptionsManager(options));

            // Act
            var tokenSet = await tokenStore.GetRequestTokensAsync(httpContext);

            // Assert
            Assert.Null(tokenSet.CookieToken);
            Assert.Null(tokenSet.RequestToken);
        }

        [Fact]
        public async Task GetRequestTokens_NonFormContentType_HeaderDisabled_ReturnsNullToken()
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

            var tokenStore = new DefaultAntiforgeryTokenStore(new TestOptionsManager(options));

            // Act
            var tokenSet = await tokenStore.GetRequestTokensAsync(httpContext);

            // Assert
            Assert.Equal("cookie-value", tokenSet.CookieToken);
            Assert.Null(tokenSet.RequestToken);
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

            var tokenStore = new DefaultAntiforgeryTokenStore(new TestOptionsManager(options));

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

            var tokenStore = new DefaultAntiforgeryTokenStore(new TestOptionsManager(options));

            // Act
            var tokens = await tokenStore.GetRequestTokensAsync(httpContext);

            // Assert
            Assert.Equal("cookie-value", tokens.CookieToken);
            Assert.Equal("header-value", tokens.RequestToken);
        }

        [Fact]
        public async Task GetRequestTokens_NonFormContentType_NoHeaderToken_ReturnsNullToken()
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

            var tokenStore = new DefaultAntiforgeryTokenStore(new TestOptionsManager(options));

            // Act
            var tokenSet = await tokenStore.GetRequestTokensAsync(httpContext);

            // Assert
            Assert.Equal("cookie-value", tokenSet.CookieToken);
            Assert.Null(tokenSet.RequestToken);
        }

        [Fact]
        public async Task GetRequestTokens_BothFieldsEmpty_ReturnsNullTokens()
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

            var tokenStore = new DefaultAntiforgeryTokenStore(new TestOptionsManager(options));

            // Act
            var tokenSet = await tokenStore.GetRequestTokensAsync(httpContext);

            // Assert
            Assert.Equal("cookie-value", tokenSet.CookieToken);
            Assert.Null(tokenSet.RequestToken);
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

            var tokenStore = new DefaultAntiforgeryTokenStore(new TestOptionsManager(options));

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
            var token = "serialized-value";
            bool defaultCookieSecureValue = expectedCookieSecureFlag ?? false; // pulled from config; set by ctor
            var cookies = new MockResponseCookieCollection();

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext
                .Setup(o => o.Response.Cookies)
                .Returns(cookies);

            var options = new AntiforgeryOptions()
            {
                CookieName = _cookieName,
                RequireSsl = requireSsl
            };

            var tokenStore = new DefaultAntiforgeryTokenStore(new TestOptionsManager(options));

            // Act
            tokenStore.SaveCookieToken(mockHttpContext.Object, token);

            // Assert
            Assert.Equal(1, cookies.Count);
            Assert.NotNull(cookies);
            Assert.Equal(_cookieName, cookies.Key);
            Assert.Equal("serialized-value", cookies.Value);
            Assert.True(cookies.Options.HttpOnly);
            Assert.Equal(defaultCookieSecureValue, cookies.Options.Secure);
        }

        private HttpContext GetHttpContext(string cookieName, string cookieValue)
        {
            var cookies = new RequestCookieCollection(new Dictionary<string, string>
            {
                { cookieName, cookieValue },
            });

            return GetHttpContext(cookies);
        }

        private HttpContext GetHttpContext(IRequestCookieCollection cookies)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Cookies = cookies;

            return httpContext;
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
