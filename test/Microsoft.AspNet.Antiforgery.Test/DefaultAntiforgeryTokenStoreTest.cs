// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.DataProtection;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Antiforgery
{
    public class DefaultAntiforgeryTokenStoreTest
    {
        private readonly string _cookieName = "cookie-name";

        [Fact]
        public void GetCookieToken_CookieDoesNotExist_ReturnsNull()
        {
            // Arrange
            var requestCookies = new Mock<IRequestCookieCollection>();
            requestCookies
                .Setup(o => o[It.IsAny<string>()])
                .Returns(string.Empty);
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext
                .Setup(o => o.Request.Cookies)
                .Returns(requestCookies.Object);
            var contextAccessor = new DefaultAntiforgeryContextAccessor();
            mockHttpContext.SetupGet(o => o.RequestServices)
                           .Returns(GetServiceProvider(contextAccessor));
            var options = new AntiforgeryOptions()
            {
                CookieName = _cookieName
            };

            var tokenStore = new DefaultAntiforgeryTokenStore(
                optionsAccessor: new TestOptionsManager(options),
                tokenSerializer: Mock.Of<IAntiforgeryTokenSerializer>());

            // Act
            var token = tokenStore.GetCookieToken(mockHttpContext.Object);

            // Assert
            Assert.Null(token);
        }

        [Fact]
        public void GetCookieToken_CookieIsMissingInRequest_LooksUpCookieInAntiforgeryContext()
        {
            // Arrange
            var requestCookies = new Mock<IRequestCookieCollection>();
            requestCookies
                .Setup(o => o[It.IsAny<string>()])
                .Returns(string.Empty);
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext
                .Setup(o => o.Request.Cookies)
                .Returns(requestCookies.Object);
            var contextAccessor = new DefaultAntiforgeryContextAccessor();
            mockHttpContext.SetupGet(o => o.RequestServices)
                           .Returns(GetServiceProvider(contextAccessor));

            // add a cookie explicitly.
            var cookie = new AntiforgeryToken();
            contextAccessor.Value = new AntiforgeryContext() { CookieToken = cookie };
            var options = new AntiforgeryOptions()
            {
                CookieName = _cookieName
            };

            var tokenStore = new DefaultAntiforgeryTokenStore(
                optionsAccessor: new TestOptionsManager(options),
                tokenSerializer: Mock.Of<IAntiforgeryTokenSerializer>());

            // Act
            var token = tokenStore.GetCookieToken(mockHttpContext.Object);

            // Assert
            Assert.Equal(cookie, token);
        }

        [Fact]
        public void GetCookieToken_CookieIsEmpty_ReturnsNull()
        {
            // Arrange
            var mockHttpContext = GetMockHttpContext(_cookieName, string.Empty);

            var options = new AntiforgeryOptions()
            {
                CookieName = _cookieName
            };

            var tokenStore = new DefaultAntiforgeryTokenStore(
                optionsAccessor: new TestOptionsManager(options),
                tokenSerializer: Mock.Of<IAntiforgeryTokenSerializer>());

            // Act
            var token = tokenStore.GetCookieToken(mockHttpContext);

            // Assert
            Assert.Null(token);
        }

        [Fact]
        public void GetCookieToken_CookieIsInvalid_PropagatesException()
        {
            // Arrange
            var mockHttpContext = GetMockHttpContext(_cookieName, "invalid-value");

            var expectedException = new InvalidOperationException("some exception");
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
            var ex = Assert.Throws<InvalidOperationException>(() => tokenStore.GetCookieToken(mockHttpContext));
            Assert.Same(expectedException, ex);
        }

        [Fact]
        public void GetCookieToken_CookieIsValid_ReturnsToken()
        {
            // Arrange
            var expectedToken = new AntiforgeryToken();
            var mockHttpContext = GetMockHttpContext(_cookieName, "valid-value");

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
            AntiforgeryToken retVal = tokenStore.GetCookieToken(mockHttpContext);

            // Assert
            Assert.Same(expectedToken, retVal);
        }

        [Fact]
        public async Task GetRequestTokens_CookieIsEmpty_Throws()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>());
            httpContext.Request.Cookies = new RequestCookieCollection(new Dictionary<string, string>());

            var options = new AntiforgeryOptions()
            {
                CookieName = "cookie-name",
                FormFieldName = "form-field-name",
            };

            var tokenStore = new DefaultAntiforgeryTokenStore(
                optionsAccessor: new TestOptionsManager(options),
                tokenSerializer: Mock.Of<IAntiforgeryTokenSerializer>());

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await tokenStore.GetRequestTokensAsync(httpContext));

            // Assert         
            Assert.Equal("The required antiforgery cookie \"cookie-name\" is not present.", exception.Message);
        }

        [Fact]
        public async Task GetRequestTokens_NonFormContentType_HeaderDisabled_Throws()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "application/json";

            // Will not be accessed
            httpContext.Request.Form = null;
            httpContext.Request.Cookies = new RequestCookieCollection(new Dictionary<string, string>()
            {
                { "cookie-name", "cookie-value" },
            });

            var options = new AntiforgeryOptions()
            {
                CookieName = "cookie-name",
                FormFieldName = "form-field-name",
                HeaderName = null,
            };

            var tokenStore = new DefaultAntiforgeryTokenStore(
                optionsAccessor: new TestOptionsManager(options),
                tokenSerializer: new DefaultAntiforgeryTokenSerializer(new EphemeralDataProtectionProvider()));

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await tokenStore.GetRequestTokensAsync(httpContext));

            // Assert         
            Assert.Equal("The required antiforgery form field \"form-field-name\" is not present.", exception.Message);
        }

        [Fact]
        public async Task GetRequestTokens_FormContentType_FallbackHeaderToken()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "application/json";

            // Will not be accessed
            httpContext.Request.ContentType = "application/x-www-form-urlencoded";
            httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>());
            httpContext.Request.Cookies = new RequestCookieCollection(new Dictionary<string, string>()
            {
                { "cookie-name", "cookie-value" },
            });
            httpContext.Request.Headers.Add("header-name", "header-value");

            var options = new AntiforgeryOptions()
            {
                CookieName = "cookie-name",
                FormFieldName = "form-field-name",
                HeaderName = "header-name",
            };

            var tokenStore = new DefaultAntiforgeryTokenStore(
                optionsAccessor: new TestOptionsManager(options),
                tokenSerializer: new DefaultAntiforgeryTokenSerializer(new EphemeralDataProtectionProvider()));

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
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "application/json";

            // Will not be accessed
            httpContext.Request.Form = null;
            httpContext.Request.Cookies = new RequestCookieCollection(new Dictionary<string, string>()
            {
                { "cookie-name", "cookie-value" },
            });

            httpContext.Request.Headers.Add("header-name", "header-value");

            var options = new AntiforgeryOptions()
            {
                CookieName = "cookie-name",
                FormFieldName = "form-field-name",
                HeaderName = "header-name",
            };

            var tokenStore = new DefaultAntiforgeryTokenStore(
                optionsAccessor: new TestOptionsManager(options),
                tokenSerializer: new DefaultAntiforgeryTokenSerializer(new EphemeralDataProtectionProvider()));

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
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "application/json";

            // Will not be accessed
            httpContext.Request.Form = null;
            httpContext.Request.Cookies = new RequestCookieCollection(new Dictionary<string, string>()
            {
                { "cookie-name", "cookie-value" },
            });

            var options = new AntiforgeryOptions()
            {
                CookieName = "cookie-name",
                FormFieldName = "form-field-name",
                HeaderName = "header-name",
            };

            var tokenStore = new DefaultAntiforgeryTokenStore(
                optionsAccessor: new TestOptionsManager(options),
                tokenSerializer: new DefaultAntiforgeryTokenSerializer(new EphemeralDataProtectionProvider()));

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await tokenStore.GetRequestTokensAsync(httpContext));

            // Assert
            Assert.Equal("The required antiforgery header value \"header-name\" is not present.", exception.Message);
        }

        [Fact]
        public async Task GetRequestTokens_BothFieldsEmpty_Throws()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "application/x-www-form-urlencoded";
            httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>());
            httpContext.Request.Cookies = new RequestCookieCollection(new Dictionary<string, string>()
            {
                { "cookie-name", "cookie-value" },
            });

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
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
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
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentType = "application/x-www-form-urlencoded";
            httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>()
            {
                { "form-field-name", "form-value" },
            });
            httpContext.Request.Cookies = new RequestCookieCollection(new Dictionary<string, string>()
            {
                { "cookie-name", "cookie-value" },
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
            var mockCookies = new Mock<IResponseCookies>();

            bool defaultCookieSecureValue = expectedCookieSecureFlag ?? false; // pulled from config; set by ctor
            var cookies = new MockResponseCookieCollection();

            cookies.Count = 0;
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(o => o.Response.Cookies)
                           .Returns(cookies);
            var contextAccessor = new DefaultAntiforgeryContextAccessor();
            mockHttpContext.SetupGet(o => o.RequestServices)
                           .Returns(GetServiceProvider(contextAccessor));

            var mockSerializer = new Mock<IAntiforgeryTokenSerializer>();
            mockSerializer.Setup(o => o.Serialize(token))
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

        private HttpContext GetMockHttpContext(string cookieName, string cookieValue)
        {
            var requestCookies = new MockCookieCollection(new Dictionary<string, string>() { { cookieName, cookieValue } });

            var request = new Mock<HttpRequest>();
            request.Setup(o => o.Cookies)
                   .Returns(requestCookies);
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(o => o.Request)
                           .Returns(request.Object);

            var contextAccessor = new DefaultAntiforgeryContextAccessor();
            mockHttpContext.SetupGet(o => o.RequestServices)
                           .Returns(GetServiceProvider(contextAccessor));

            return mockHttpContext.Object;
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

        private class MockCookieCollection : IRequestCookieCollection
        {
            private Dictionary<string, string> _dictionary;

            public int Count
            {
                get
                {
                    return _dictionary.Count;
                }
            }

            public ICollection<string> Keys
            {
                get
                {
                    return _dictionary.Keys;
                }
            }

            public MockCookieCollection(Dictionary<string, string> dictionary)
            {
                _dictionary = dictionary;
            }

            public static MockCookieCollection GetDummyInstance(string key, string value)
            {
                return new MockCookieCollection(new Dictionary<string, string>() { { key, value } });
            }

            public bool ContainsKey(string key)
            {
                return _dictionary.ContainsKey(key);
            }

            public string this[string key]
            {
                get
                {
                    string value;
                    return _dictionary.TryGetValue(key, out value) ? value : null;
                }
            }

            public bool TryGetValue(string key, out string value)
            {
                return _dictionary.TryGetValue(key, out value);
            }

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }
    }
}
