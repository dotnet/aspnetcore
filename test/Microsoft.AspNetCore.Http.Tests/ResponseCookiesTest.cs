// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.ObjectPool;
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
            Assert.Equal(1, cookieHeaderValues.Count);
            Assert.StartsWith(testcookie, cookieHeaderValues[0]);
            Assert.Contains("path=/", cookieHeaderValues[0]);
            Assert.Contains("expires=Thu, 01 Jan 1970 00:00:00 GMT", cookieHeaderValues[0]);
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
            Assert.Equal(1, cookieHeaderValues.Count);
            Assert.StartsWith(testcookie, cookieHeaderValues[0]);
            Assert.Contains("path=/", cookieHeaderValues[0]);
            Assert.Contains("expires=Thu, 01 Jan 1970 00:00:00 GMT", cookieHeaderValues[0]);
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
            Assert.Equal(1, cookieHeaderValues.Count);
            Assert.StartsWith(expected, cookieHeaderValues[0]);
        }
    }
}
