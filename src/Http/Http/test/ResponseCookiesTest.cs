// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Http.Tests
{
    public class ResponseCookiesTest
    {
        private IFeatureCollection MakeFeatures(IHeaderDictionary headers)
        {
            var responseFeature = new HttpResponseFeature()
            {
                Headers = headers
            };
            var features = new FeatureCollection();
            features.Set<IHttpResponseFeature>(responseFeature);
            return features;
        }

        [Fact]
        public void AppendSameSiteNoneWithoutSecureLogsWarning()
        {
            var headers = new HeaderDictionary();
            var features = MakeFeatures(headers);
            var services = new ServiceCollection();

            var sink = new TestSink(TestSink.EnableWithTypeName<ResponseCookies>);
            var loggerFactory = new TestLoggerFactory(sink, enabled: true);
            services.AddLogging();
            services.AddSingleton<ILoggerFactory>(loggerFactory);

            features.Set<IServiceProvidersFeature>(new ServiceProvidersFeature() { RequestServices = services.BuildServiceProvider() });

            var cookies = new ResponseCookies(features);
            var testCookie = "TestCookie";

            cookies.Append(testCookie, "value", new CookieOptions()
            {
                SameSite = SameSiteMode.None,
            });

            var cookieHeaderValues = headers[HeaderNames.SetCookie];
            Assert.Single(cookieHeaderValues);
            Assert.StartsWith(testCookie, cookieHeaderValues[0]);
            Assert.Contains("path=/", cookieHeaderValues[0]);
            Assert.Contains("samesite=none", cookieHeaderValues[0]);
            Assert.DoesNotContain("secure", cookieHeaderValues[0]);

            var writeContext = Assert.Single(sink.Writes);
            Assert.Equal("The cookie 'TestCookie' has set 'SameSite=None' and must also set 'Secure'.", writeContext.Message);
        }

        [Fact]
        public void DeleteCookieShouldSetDefaultPath()
        {
            var headers = new HeaderDictionary();
            var features = MakeFeatures(headers);
            var cookies = new ResponseCookies(features);
            var testCookie = "TestCookie";

            cookies.Delete(testCookie);

            var cookieHeaderValues = headers[HeaderNames.SetCookie];
            Assert.Single(cookieHeaderValues);
            Assert.StartsWith(testCookie, cookieHeaderValues[0]);
            Assert.Contains("path=/", cookieHeaderValues[0]);
            Assert.Contains("expires=Thu, 01 Jan 1970 00:00:00 GMT", cookieHeaderValues[0]);
        }

        [Fact]
        public void DeleteCookieWithCookieOptionsShouldKeepPropertiesOfCookieOptions()
        {
            var headers = new HeaderDictionary();
            var features = MakeFeatures(headers);
            var cookies = new ResponseCookies(features);
            var testCookie = "TestCookie";
            var time = new DateTimeOffset(2000, 1, 1, 1, 1, 1, 1, TimeSpan.Zero);
            var options = new CookieOptions
            {
                Secure = true,
                HttpOnly = true,
                Path = "/",
                Expires = time,
                Domain = "example.com",
                SameSite = SameSiteMode.Lax
            };

            cookies.Delete(testCookie, options);

            var cookieHeaderValues = headers[HeaderNames.SetCookie];
            Assert.Single(cookieHeaderValues);
            Assert.StartsWith(testCookie, cookieHeaderValues[0]);
            Assert.Contains("path=/", cookieHeaderValues[0]);
            Assert.Contains("expires=Thu, 01 Jan 1970 00:00:00 GMT", cookieHeaderValues[0]);
            Assert.Contains("secure", cookieHeaderValues[0]);
            Assert.Contains("httponly", cookieHeaderValues[0]);
            Assert.Contains("samesite", cookieHeaderValues[0]);
        }

        [Fact]
        public void NoParamsDeleteRemovesCookieCreatedByAdd()
        {
            var headers = new HeaderDictionary();
            var features = MakeFeatures(headers);
            var cookies = new ResponseCookies(features);
            var testCookie = "TestCookie";

            cookies.Append(testCookie, testCookie);
            cookies.Delete(testCookie);

            var cookieHeaderValues = headers[HeaderNames.SetCookie];
            Assert.Single(cookieHeaderValues);
            Assert.StartsWith(testCookie, cookieHeaderValues[0]);
            Assert.Contains("path=/", cookieHeaderValues[0]);
            Assert.Contains("expires=Thu, 01 Jan 1970 00:00:00 GMT", cookieHeaderValues[0]);
        }

        [Fact]
        public void ProvidesMaxAgeWithCookieOptionsArgumentExpectMaxAgeToBeSet()
        {
            var headers = new HeaderDictionary();
            var features = MakeFeatures(headers);
            var cookies = new ResponseCookies(features);
            var cookieOptions = new CookieOptions();
            var maxAgeTime = TimeSpan.FromHours(1);
            cookieOptions.MaxAge = TimeSpan.FromHours(1);
            var testCookie = "TestCookie";

            cookies.Append(testCookie, testCookie, cookieOptions);

            var cookieHeaderValues = headers[HeaderNames.SetCookie];
            Assert.Single(cookieHeaderValues);
            Assert.Contains($"max-age={maxAgeTime.TotalSeconds}", cookieHeaderValues[0]);
        }

        [Theory]
        [InlineData("value", "key=value")]
        [InlineData("!value", "key=%21value")]
        [InlineData("val^ue", "key=val%5Eue")]
        [InlineData("QUI+REU/Rw==", "key=QUI%2BREU%2FRw%3D%3D")]
        public void EscapesValuesBeforeSettingCookie(string value, string expected)
        {
            var headers = new HeaderDictionary();
            var features = MakeFeatures(headers);
            var cookies = new ResponseCookies(features);

            cookies.Append("key", value);

            var cookieHeaderValues = headers[HeaderNames.SetCookie];
            Assert.Single(cookieHeaderValues);
            Assert.StartsWith(expected, cookieHeaderValues[0]);
        }

        [Theory]
        [InlineData("key,")]
        [InlineData("ke@y")]
        public void InvalidKeysThrow(string key)
        {
            var headers = new HeaderDictionary();
            var features = MakeFeatures(headers);
            var cookies = new ResponseCookies(features);

            Assert.Throws<ArgumentException>(() => cookies.Append(key, "1"));
        }

        [Theory]
        [InlineData("key", "value", "key=value")]
        [InlineData("key,", "!value", "key%2C=%21value")]
        [InlineData("ke#y,", "val^ue", "ke%23y%2C=val%5Eue")]
        [InlineData("base64", "QUI+REU/Rw==", "base64=QUI%2BREU%2FRw%3D%3D")]
        public void AppContextSwitchEscapesKeysAndValuesBeforeSettingCookie(string key, string value, string expected)
        {
            var headers = new HeaderDictionary();
            var features = MakeFeatures(headers);
            var cookies = new ResponseCookies(features);
            cookies._enableCookieNameEncoding = true;

            cookies.Append(key, value);

            var cookieHeaderValues = headers[HeaderNames.SetCookie];
            Assert.Single(cookieHeaderValues);
            Assert.StartsWith(expected, cookieHeaderValues[0]);
        }
    }
}
