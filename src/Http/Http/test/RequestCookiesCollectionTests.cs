// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Http.Tests
{
    public class RequestCookiesCollectionTests
    {
        [Theory]
        [InlineData("key=value", "key", "value")]
        [InlineData("__secure-key=value", "__secure-key", "value")]
        [InlineData("key%2C=%21value", "key,", "!value")]
        [InlineData("ke%23y%2C=val%5Eue", "ke#y,", "val^ue")]
        [InlineData("base64=QUI%2BREU%2FRw%3D%3D", "base64", "QUI+REU/Rw==")]
        [InlineData("base64=QUI+REU/Rw==", "base64", "QUI+REU/Rw==")]
        public void UnEscapesValues(string input, string expectedKey, string expectedValue)
        {
            var cookies = RequestCookieCollection.Parse(new StringValues(input));

            Assert.Equal(1, cookies.Count);
            Assert.Equal(Uri.EscapeDataString(expectedKey), cookies.Keys.Single());
            Assert.Equal(expectedValue, cookies[expectedKey]);
        }

        [Theory]
        [InlineData("key=value", "key", "value")]
        [InlineData("__secure-key=value", "__secure-key", "value")]
        [InlineData("key%2C=%21value", "key,", "!value")]
        [InlineData("ke%23y%2C=val%5Eue", "ke#y,", "val^ue")]
        [InlineData("base64=QUI%2BREU%2FRw%3D%3D", "base64", "QUI+REU/Rw==")]
        [InlineData("base64=QUI+REU/Rw==", "base64", "QUI+REU/Rw==")]
        public void AppContextSwitchUnEscapesKeyValues(string input, string expectedKey, string expectedValue)
        {
            var cookies = RequestCookieCollection.ParseInternal(new StringValues(input), enableCookieNameDecoding: true);

            Assert.Equal(1, cookies.Count);
            Assert.Equal(expectedKey, cookies.Keys.Single());
            Assert.Equal(expectedValue, cookies[expectedKey]);
        }

        [Theory]
        [InlineData(",", null)]
        [InlineData(";", null)]
        [InlineData("er=dd,cc,bb", null)]
        [InlineData("er=dd,err=cc,errr=bb", null)]
        [InlineData("errorcookie=dd,:(\"sa;", null)]
        [InlineData("s;", null)]
        [InlineData("a@a=a;", null)]
        [InlineData("a@ a=a;", null)]
        [InlineData("a a=a;", null)]
        [InlineData(",a=a;", null)]
        [InlineData(",a=a", null)]
        [InlineData("a=a;,b=b", new []{ "a" })] // valid cookie followed by invalid cookie
        [InlineData(",a=a;b=b", new[] { "b" })] // invalid cookie followed by valid cookie
        public void ParseInvalidCookies(string cookieToParse, string[] expectedCookieValues)
        {
            var cookies = RequestCookieCollection.Parse(new StringValues(new[] { cookieToParse }));

            if (expectedCookieValues == null)
            {
                Assert.Equal(0, cookies.Count);
                return;
            }

            Assert.Equal(expectedCookieValues.Length, cookies.Count);
            for (int i = 0; i < expectedCookieValues.Length; i++)
            {
                var value = expectedCookieValues[i];
                Assert.Equal(value, cookies.ElementAt(i).Value);
            }
        }

        [Fact]
        public void ParseManyCookies()
        {
            var cookies = RequestCookieCollection.Parse(new StringValues(new[] { "a=a", "b=b", "c=c", "d=d", "e=e", "f=f", "g=g", "h=h", "i=i", "j=j", "k=k", "l=l" }));

            Assert.Equal(12, cookies.Count);
        }
    }
}
