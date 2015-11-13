// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNet.Http.Internal;

namespace Microsoft.AspNet.Http.Tests
{
    public class ResponseCookiesTest
    {
        [Fact]
        public void DeleteCookieShouldSetDefaultPath()
        {
            var headers = new HeaderDictionary();
            var cookies = new ResponseCookies(headers);
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
            var cookies = new ResponseCookies(headers);
            var testcookie = "TestCookie";

            cookies.Append(testcookie, testcookie);
            cookies.Delete(testcookie);

            var cookieHeaderValues = headers[HeaderNames.SetCookie];
            Assert.Equal(1, cookieHeaderValues.Count);
            Assert.StartsWith(testcookie, cookieHeaderValues[0]);
            Assert.Contains("path=/", cookieHeaderValues[0]);
            Assert.Contains("expires=Thu, 01 Jan 1970 00:00:00 GMT", cookieHeaderValues[0]);
        }

    }
}
