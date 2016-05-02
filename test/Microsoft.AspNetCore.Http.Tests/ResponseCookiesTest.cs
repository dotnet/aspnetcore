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
        private static readonly ObjectPool<StringBuilder> _builderPool =
            new DefaultObjectPoolProvider().Create<StringBuilder>(new StringBuilderPooledObjectPolicy());

        public static TheoryData BuilderPoolData
        {
            get
            {
                return new TheoryData<ObjectPool<StringBuilder>>
                {
                    null,
                    _builderPool,
                };
            }
        }

        [Theory]
        [MemberData(nameof(BuilderPoolData))]
        public void DeleteCookieShouldSetDefaultPath(ObjectPool<StringBuilder> builderPool)
        {
            var headers = new HeaderDictionary();
            var cookies = new ResponseCookies(headers, builderPool);
            var testcookie = "TestCookie";

            cookies.Delete(testcookie);

            var cookieHeaderValues = headers[HeaderNames.SetCookie];
            Assert.Equal(1, cookieHeaderValues.Count);
            Assert.StartsWith(testcookie, cookieHeaderValues[0]);
            Assert.Contains("path=/", cookieHeaderValues[0]);
            Assert.Contains("expires=Thu, 01 Jan 1970 00:00:00 GMT", cookieHeaderValues[0]);
        }

        [Theory]
        [MemberData(nameof(BuilderPoolData))]
        public void NoParamsDeleteRemovesCookieCreatedByAdd(ObjectPool<StringBuilder> builderPool)
        {
            var headers = new HeaderDictionary();
            var cookies = new ResponseCookies(headers, builderPool);
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
                return new TheoryData<string, string, ObjectPool<StringBuilder>, string>
                {
                    { "key", "value", null, "key=value" },
                    { "key,", "!value", null, "key%2C=%21value" },
                    { "ke#y,", "val^ue", null, "ke%23y%2C=val%5Eue" },
                    { "key", "value", _builderPool, "key=value" },
                    { "key,", "!value", _builderPool, "key%2C=%21value" },
                    { "ke#y,", "val^ue", _builderPool, "ke%23y%2C=val%5Eue" },
                    { "base64", "QUI+REU/Rw==", _builderPool, "base64=QUI%2BREU%2FRw%3D%3D" },
                };
            }
        }

        [Theory]
        [MemberData(nameof(EscapesKeyValuesBeforeSettingCookieData))]
        public void EscapesKeyValuesBeforeSettingCookie(
            string key,
            string value,
            ObjectPool<StringBuilder> builderPool,
            string expected)
        {
            var headers = new HeaderDictionary();
            var cookies = new ResponseCookies(headers, builderPool);

            cookies.Append(key, value);

            var cookieHeaderValues = headers[HeaderNames.SetCookie];
            Assert.Equal(1, cookieHeaderValues.Count);
            Assert.StartsWith(expected, cookieHeaderValues[0]);
        }
    }
}
