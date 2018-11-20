// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Http.Tests
{
    public class ResponseCookiesTest
    {
        [Fact]
        public void DeleteCookieShouldSetDefaultPath()
        {
            var headers = new HeaderDictionary();
            var cookies = new ResponseCookies(headers, null);
            var testcookie = "TestCookie";

            cookies.Delete(testcookie);

            var cookieHeaderValues = headers[HeaderNames.SetCookie];
            Assert.Single(cookieHeaderValues);
            Assert.StartsWith(testcookie, cookieHeaderValues[0]);
            Assert.Contains("path=/", cookieHeaderValues[0]);
            Assert.Contains("expires=Thu, 01 Jan 1970 00:00:00 GMT", cookieHeaderValues[0]);
        }

        [Fact]
        public void DeleteCookieWithCookieOptionsShouldKeepPropertiesOfCookieOptions()
        {
            var headers = new HeaderDictionary();
            var cookies = new ResponseCookies(headers, null);
            var testcookie = "TestCookie";
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

            cookies.Delete(testcookie, options);

            var cookieHeaderValues = headers[HeaderNames.SetCookie];
            Assert.Single(cookieHeaderValues);
            Assert.StartsWith(testcookie, cookieHeaderValues[0]);
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
            var cookies = new ResponseCookies(headers, null);
            var testcookie = "TestCookie";

            cookies.Append(testcookie, testcookie);
            cookies.Delete(testcookie);

            var cookieHeaderValues = headers[HeaderNames.SetCookie];
            Assert.Single(cookieHeaderValues);
            Assert.StartsWith(testcookie, cookieHeaderValues[0]);
            Assert.Contains("path=/", cookieHeaderValues[0]);
            Assert.Contains("expires=Thu, 01 Jan 1970 00:00:00 GMT", cookieHeaderValues[0]);
        }

        [Fact]
        public void ProvidesMaxAgeWithCookieOptionsArgumentExpectMaxAgeToBeSet()
        {
            var headers = new HeaderDictionary();
            var cookies = new ResponseCookies(headers, null);
            var cookieOptions = new CookieOptions();
            var maxAgeTime = TimeSpan.FromHours(1);
            cookieOptions.MaxAge = TimeSpan.FromHours(1);
            var testcookie = "TestCookie";

            cookies.Append(testcookie, testcookie, cookieOptions);

            var cookieHeaderValues = headers[HeaderNames.SetCookie];
            Assert.Single(cookieHeaderValues);
            Assert.Contains($"max-age={maxAgeTime.TotalSeconds.ToString()}", cookieHeaderValues[0]);
        }

        public static TheoryData EscapesKeyValuesBeforeSettingCookieData
        {
            get
            {
                // key, value, object pool, expected
                return new TheoryData<string, string, string>
                {
                    { "key", "value", "key=value" },
                    { "key,", "!value", "key%2C=%21value" },
                    { "ke#y,", "val^ue", "ke%23y%2C=val%5Eue" },
                    { "base64", "QUI+REU/Rw==", "base64=QUI%2BREU%2FRw%3D%3D" },
                };
            }
        }

        [Theory]
        [MemberData(nameof(EscapesKeyValuesBeforeSettingCookieData))]
        public void EscapesKeyValuesBeforeSettingCookie(
            string key,
            string value,
            string expected)
        {
            var headers = new HeaderDictionary();
            var cookies = new ResponseCookies(headers, null);

            cookies.Append(key, value);

            var cookieHeaderValues = headers[HeaderNames.SetCookie];
            Assert.Single(cookieHeaderValues);
            Assert.StartsWith(expected, cookieHeaderValues[0]);
        }
    }
}
