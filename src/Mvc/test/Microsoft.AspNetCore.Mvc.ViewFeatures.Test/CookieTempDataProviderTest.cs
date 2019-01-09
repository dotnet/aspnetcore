// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    public class CookieTempDataProviderTest
    {
        private static readonly byte[] Bytes = Encoding.UTF8.GetBytes("test value");
        private static readonly IDictionary<string, object> Dictionary = new Dictionary<string, object>
        {
            { "key", "value" },
        };

        [Fact]
        public void SaveTempData_UsesCookieName_FromOptions()
        {
            // Arrange
            var expectedCookieName = "TestCookieName";

            var expectedDataInCookie = WebEncoders.Base64UrlEncode(Bytes);
            var tempDataProvider = GetProvider(dataProtector: null, options: new CookieTempDataProviderOptions()
            {
                Cookie = { Name = expectedCookieName }
            });

            var responseCookies = new MockResponseCookieCollection();
            var httpContext = new Mock<HttpContext>();
            httpContext
                .SetupGet(hc => hc.Request.PathBase)
                .Returns("/");
            httpContext
                .Setup(hc => hc.Response.Cookies)
                .Returns(responseCookies);

            // Act
            tempDataProvider.SaveTempData(httpContext.Object, Dictionary);

            // Assert
            Assert.Contains(responseCookies, (cookie) => cookie.Key == expectedCookieName);
            var cookieInfo = responseCookies[expectedCookieName];
            Assert.Equal(expectedDataInCookie, cookieInfo.Value);
            Assert.Equal("/", cookieInfo.Options.Path);
        }

        [Fact]
        public void LoadTempData_ReturnsEmptyDictionary_WhenNoCookieDataIsAvailable()
        {
            // Arrange
            var tempDataProvider = GetProvider();

            // Act
            var tempDataDictionary = tempDataProvider.LoadTempData(new DefaultHttpContext());

            // Assert
            Assert.NotNull(tempDataDictionary);
            Assert.Empty(tempDataDictionary);
        }

        [Fact]
        public void LoadTempData_ReturnsEmptyDictionary_AndClearsCookie_WhenDataIsInvalid()
        {
            // Arrange
            var dataProtector = new Mock<IDataProtector>(MockBehavior.Strict);
            dataProtector
                .Setup(d => d.Unprotect(It.IsAny<byte[]>()))
                .Throws(new Exception());

            var tempDataProvider = GetProvider(dataProtector.Object);

            var expectedDataToUnprotect = Bytes;
            var base64AndUrlEncodedDataInCookie = WebEncoders.Base64UrlEncode(expectedDataToUnprotect);

            var context = new DefaultHttpContext();
            context.Request.Cookies = new RequestCookieCollection(new Dictionary<string, string>()
            {
                { CookieTempDataProvider.CookieName, base64AndUrlEncodedDataInCookie }
            });

            // Act
            var tempDataDictionary = tempDataProvider.LoadTempData(context);

            // Assert
            Assert.Empty(tempDataDictionary);

            var setCookieHeader = SetCookieHeaderValue.Parse(context.Response.Headers["Set-Cookie"].ToString());
            Assert.Equal(CookieTempDataProvider.CookieName, setCookieHeader.Name.ToString());
            Assert.Equal(string.Empty, setCookieHeader.Value.ToString());
        }

        [Fact]
        public void LoadTempData_Base64UrlDecodesAnd_UnprotectsData_FromCookie()
        {
            // Arrange
            var expectedDataToUnprotect = Bytes;
            var base64AndUrlEncodedDataInCookie = WebEncoders.Base64UrlEncode(expectedDataToUnprotect);
            var dataProtector = new PassThroughDataProtector();
            var tempDataProvider = GetProvider(dataProtector);
            var requestCookies = new RequestCookieCollection(new Dictionary<string, string>()
            {
                { CookieTempDataProvider.CookieName, base64AndUrlEncodedDataInCookie }
            });
            var httpContext = new Mock<HttpContext>();
            httpContext
               .Setup(hc => hc.Request.Cookies)
               .Returns(requestCookies);

            // Act
            var actualValues = tempDataProvider.LoadTempData(httpContext.Object);

            // Assert
            Assert.Equal(expectedDataToUnprotect, dataProtector.DataToUnprotect);
            Assert.Same(Dictionary, actualValues);
        }

        [Fact]
        public void SaveTempData_ProtectsAnd_Base64UrlEncodesDataAnd_SetsCookie()
        {
            // Arrange
            var values = new Dictionary<string, object>();
            values.Add("int", 10);
            var expectedDataToProtect = Bytes;
            var expectedDataInCookie = WebEncoders.Base64UrlEncode(expectedDataToProtect);
            var dataProtector = new PassThroughDataProtector();
            var tempDataProvider = GetProvider(dataProtector);
            var responseCookies = new MockResponseCookieCollection();
            var httpContext = new Mock<HttpContext>();
            httpContext
                .SetupGet(hc => hc.Request.PathBase)
                .Returns("/");
            httpContext
                .Setup(hc => hc.Response.Cookies)
                .Returns(responseCookies);

            // Act
            tempDataProvider.SaveTempData(httpContext.Object, Dictionary);

            // Assert
            Assert.Equal(1, responseCookies.Count);
            var cookieInfo = responseCookies[CookieTempDataProvider.CookieName];
            Assert.NotNull(cookieInfo);
            Assert.Equal(expectedDataInCookie, cookieInfo.Value);
            Assert.Equal(expectedDataToProtect, dataProtector.PlainTextToProtect);
        }

        [Theory]
        [InlineData(true, CookieSecurePolicy.None, false)]
        [InlineData(false, CookieSecurePolicy.None, false)]
        [InlineData(true, CookieSecurePolicy.Always, true)]
        [InlineData(false, CookieSecurePolicy.Always, true)]
        [InlineData(true, CookieSecurePolicy.SameAsRequest, true)]
        [InlineData(false, CookieSecurePolicy.SameAsRequest, false)]
        public void SaveTempData_HonorsCookieSecurePolicy_OnOptions(
            bool isRequestSecure,
            CookieSecurePolicy cookieSecurePolicy,
            bool expectedSecureFlag)
        {
            // Arrange
            var expectedDataToProtect = Bytes;
            var expectedDataInCookie = WebEncoders.Base64UrlEncode(expectedDataToProtect);
            var dataProtector = new PassThroughDataProtector();
            var options = new CookieTempDataProviderOptions();
            options.Cookie.SecurePolicy = cookieSecurePolicy;
            var tempDataProvider = GetProvider(dataProtector, options);
            var responseCookies = new MockResponseCookieCollection();
            var httpContext = new Mock<HttpContext>();
            httpContext
                .SetupGet(hc => hc.Request.PathBase)
                .Returns("/");
            httpContext
                .SetupGet(hc => hc.Request.IsHttps)
                .Returns(isRequestSecure);
            httpContext
                .Setup(hc => hc.Response.Cookies)
                .Returns(responseCookies);

            // Act
            tempDataProvider.SaveTempData(httpContext.Object, Dictionary);

            // Assert
            Assert.Equal(1, responseCookies.Count);
            var cookieInfo = responseCookies[CookieTempDataProvider.CookieName];
            Assert.NotNull(cookieInfo);
            Assert.Equal(expectedDataInCookie, cookieInfo.Value);
            Assert.Equal(expectedDataToProtect, dataProtector.PlainTextToProtect);
            Assert.Equal("/", cookieInfo.Options.Path);
            Assert.Equal(expectedSecureFlag, cookieInfo.Options.Secure);
            Assert.True(cookieInfo.Options.HttpOnly);
            Assert.Null(cookieInfo.Options.Expires);
            Assert.Null(cookieInfo.Options.Domain);
        }

        [Theory]
        [InlineData(null, "/")]
        [InlineData("", "/")]
        [InlineData("/", "/")]
        [InlineData("/vdir1", "/vdir1")]
        public void SaveTempData_DefaultProviderOptions_SetsCookie_WithAppropriateCookieOptions(
            string pathBase,
            string expectedCookiePath)
        {
            // Arrange
            var expectedDataToProtect = Bytes;
            var expectedDataInCookie = WebEncoders.Base64UrlEncode(expectedDataToProtect);
            var dataProtector = new PassThroughDataProtector();
            var tempDataProvider = GetProvider(dataProtector);
            var responseCookies = new MockResponseCookieCollection();
            var httpContext = new Mock<HttpContext>();
            httpContext
                .SetupGet(hc => hc.Request.PathBase)
                .Returns(pathBase);
            httpContext
                .SetupGet(hc => hc.Request.IsHttps)
                .Returns(false);
            httpContext
                .Setup(hc => hc.Response.Cookies)
                .Returns(responseCookies);

            // Act
            tempDataProvider.SaveTempData(httpContext.Object, Dictionary);

            // Assert
            Assert.Equal(1, responseCookies.Count);
            var cookieInfo = responseCookies[CookieTempDataProvider.CookieName];
            Assert.NotNull(cookieInfo);
            Assert.Equal(expectedDataInCookie, cookieInfo.Value);
            Assert.Equal(expectedDataToProtect, dataProtector.PlainTextToProtect);
            Assert.Equal(expectedCookiePath, cookieInfo.Options.Path);
            Assert.False(cookieInfo.Options.Secure);
            Assert.True(cookieInfo.Options.HttpOnly);
            Assert.Null(cookieInfo.Options.Expires);
            Assert.Null(cookieInfo.Options.Domain);
        }

        [Theory]
        [InlineData(null, null, null, "/", null)]
        [InlineData("", null, null, "/", null)]
        [InlineData("/", null, null, "/", null)]
        [InlineData("/", "/vdir1", null, "/vdir1", null)]
        [InlineData("/", "/vdir1", ".abc.com", "/vdir1", ".abc.com")]
        [InlineData("/vdir1", "/", ".abc.com", "/", ".abc.com")]
        public void SaveTempData_CustomProviderOptions_SetsCookie_WithAppropriateCookieOptions(
            string requestPathBase,
            string optionsPath,
            string optionsDomain,
            string expectedCookiePath,
            string expectedDomain)
        {
            // Arrange
            var expectedDataToProtect = Bytes;
            var expectedDataInCookie = WebEncoders.Base64UrlEncode(expectedDataToProtect);
            var dataProtector = new PassThroughDataProtector();
            var tempDataProvider = GetProvider(
                dataProtector,
                new CookieTempDataProviderOptions
                {
                    Cookie =
                    {
                        Path = optionsPath,
                        Domain = optionsDomain
                    }
                });
            var responseCookies = new MockResponseCookieCollection();
            var httpContext = new Mock<HttpContext>();
            httpContext
                .SetupGet(hc => hc.Request.IsHttps)
                .Returns(false);
            httpContext
                .SetupGet(hc => hc.Request.PathBase)
                .Returns(requestPathBase);
            httpContext
                .Setup(hc => hc.Response.Cookies)
                .Returns(responseCookies);

            // Act
            tempDataProvider.SaveTempData(httpContext.Object, Dictionary);

            // Assert
            Assert.Equal(1, responseCookies.Count);
            var cookieInfo = responseCookies[CookieTempDataProvider.CookieName];
            Assert.NotNull(cookieInfo);
            Assert.Equal(expectedDataInCookie, cookieInfo.Value);
            Assert.Equal(expectedDataToProtect, dataProtector.PlainTextToProtect);
            Assert.Equal(expectedCookiePath, cookieInfo.Options.Path);
            Assert.Equal(expectedDomain, cookieInfo.Options.Domain);
            Assert.False(cookieInfo.Options.Secure);
            Assert.True(cookieInfo.Options.HttpOnly);
            Assert.Null(cookieInfo.Options.Expires);
        }

        [Fact]
        public void SaveTempData_RemovesCookie_WhenNoDataToSave()
        {
            // Arrange
            var values = new Dictionary<string, object>();
            var serializedData = Bytes;
            var base64AndUrlEncodedData = WebEncoders.Base64UrlEncode(serializedData);
            var dataProtector = new PassThroughDataProtector();
            var tempDataProvider = GetProvider(dataProtector);
            var requestCookies = new RequestCookieCollection(new Dictionary<string, string>()
            {
                { CookieTempDataProvider.CookieName, base64AndUrlEncodedData }
            });
            var responseCookies = new MockResponseCookieCollection();
            var httpContext = new Mock<HttpContext>();
            httpContext
                .SetupGet(hc => hc.Request.PathBase)
                .Returns("/");
            httpContext
                .Setup(hc => hc.Request.Cookies)
                .Returns(requestCookies);
            httpContext
                .Setup(hc => hc.Response.Cookies)
                .Returns(responseCookies);
            httpContext
                .Setup(hc => hc.Response.Headers)
                .Returns(new HeaderDictionary());

            // Act
            tempDataProvider.SaveTempData(httpContext.Object, new Dictionary<string, object>());

            // Assert
            Assert.Equal(1, responseCookies.Count);
            var cookie = responseCookies[CookieTempDataProvider.CookieName];
            Assert.NotNull(cookie);
            Assert.Equal(string.Empty, cookie.Value);
            Assert.NotNull(cookie.Options.Expires);
            Assert.True(cookie.Options.Expires.Value < DateTimeOffset.Now); // expired cookie
        }

        [Fact]
        public void SaveAndLoad_Works()
        {
            // Arrange
            var testProvider = GetProvider();
            var input = new Dictionary<string, object>
            {
                { "string", "value" }
            };
            var context = GetHttpContext();

            // Act
            testProvider.SaveTempData(context, input);
            UpdateRequestWithCookies(context);
            var tempData = testProvider.LoadTempData(context);

            // Assert
            Assert.Same(Dictionary, tempData);
        }

        private static HttpContext GetHttpContext()
        {
            var context = new Mock<HttpContext>();
            context
                .SetupGet(hc => hc.Request.PathBase)
                .Returns("/");
            context
                .SetupGet(hc => hc.Response.Cookies)
                .Returns(new MockResponseCookieCollection());
            return context.Object;
        }

        private void UpdateRequestWithCookies(HttpContext httpContext)
        {
            var responseCookies = (MockResponseCookieCollection)httpContext.Response.Cookies;

            var values = new Dictionary<string, string>();

            foreach (var responseCookie in responseCookies)
            {
                values.Add(responseCookie.Key, responseCookie.Value);
            }

            if (values.Count > 0)
            {
                httpContext.Request.Cookies = new RequestCookieCollection(values);
            }
        }

        private class MockResponseCookieCollection : IResponseCookies, IEnumerable<CookieInfo>
        {
            private Dictionary<string, CookieInfo> _cookies = new Dictionary<string, CookieInfo>(StringComparer.OrdinalIgnoreCase);

            public int Count
            {
                get
                {
                    return _cookies.Count;
                }
            }

            public CookieInfo this[string key] => _cookies[key];

            public void Append(string key, string value, CookieOptions options)
            {
                _cookies[key] = new CookieInfo()
                {
                    Key = key,
                    Value = value,
                    Options = options
                };
            }

            public void Append(string key, string value)
            {
                Append(key, value, new CookieOptions());
            }

            public void Delete(string key, CookieOptions options)
            {
                _cookies.Remove(key);
            }

            public void Delete(string key)
            {
                _cookies.Remove(key);
            }

            public IEnumerator<CookieInfo> GetEnumerator()
            {
                return _cookies.Values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private CookieTempDataProvider GetProvider(IDataProtector dataProtector = null, CookieTempDataProviderOptions options = null)
        {
            if (dataProtector == null)
            {
                dataProtector = new PassThroughDataProtector();
            }
            if (options == null)
            {
                options = new CookieTempDataProviderOptions();
            }

            var testOptions = new Mock<IOptions<CookieTempDataProviderOptions>>();
            testOptions.SetupGet(o => o.Value).Returns(options);

            return new CookieTempDataProvider(
                new PassThroughDataProtectionProvider(dataProtector),
                NullLoggerFactory.Instance,
                testOptions.Object,
                new TestTempDataSerializer());
        }

        private class PassThroughDataProtectionProvider : IDataProtectionProvider
        {
            private readonly IDataProtector _dataProtector;

            public PassThroughDataProtectionProvider(IDataProtector dataProtector)
            {
                _dataProtector = dataProtector;
            }

            public IDataProtector CreateProtector(string purpose)
            {
                return _dataProtector;
            }
        }

        private class PassThroughDataProtector : IDataProtector
        {
            public byte[] DataToUnprotect { get; private set; }
            public byte[] PlainTextToProtect { get; private set; }
            public string Purpose { get; private set; }

            public IDataProtector CreateProtector(string purpose)
            {
                Purpose = purpose;
                return this;
            }

            public byte[] Protect(byte[] plaintext)
            {
                PlainTextToProtect = plaintext;
                return PlainTextToProtect;
            }

            public byte[] Unprotect(byte[] protectedData)
            {
                DataToUnprotect = protectedData;
                return DataToUnprotect;
            }
        }

        private class CookieInfo
        {
            public string Key { get; set; }

            public string Value { get; set; }

            public CookieOptions Options { get; set; }
        }

        private class TestTempDataSerializer : TempDataSerializer
        {
            public override IDictionary<string, object> Deserialize(byte[] unprotectedData)
            {
                return Dictionary;
            }

            public override byte[] Serialize(IDictionary<string, object> values)
            {
                return Bytes;
            }
        }
    }
}