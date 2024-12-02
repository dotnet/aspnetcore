// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http.Tests;

public class RequestCookiesCollectionTests
{
    [Theory]
    [InlineData("key=value", "key", "value")]
    [InlineData("key%2C=%21value", "key%2C", "!value")]
    [InlineData("ke%23y%2C=val%5Eue", "ke%23y%2C", "val^ue")]
    [InlineData("base64=QUI%2BREU%2FRw%3D%3D", "base64", "QUI+REU/Rw==")]
    [InlineData("base64=QUI+REU/Rw==", "base64", "QUI+REU/Rw==")]
    public void UnEscapesValues(string input, string expectedKey, string expectedValue)
    {
        var cookies = RequestCookieCollection.Parse(new StringValues(input));

        Assert.Equal(1, cookies.Count);
        Assert.Equal(expectedKey, cookies.Keys.Single());
        Assert.Equal(expectedValue, cookies[expectedKey]);
    }

    [Fact]
    public void ParseManyCookies()
    {
        var cookies = RequestCookieCollection.Parse(new StringValues(new[] { "a=a", "b=b", "c=c", "d=d", "e=e", "f=f", "g=g", "h=h", "i=i", "j=j", "k=k", "l=l" }));

        Assert.Equal(12, cookies.Count);
    }

    [Theory]
    [InlineData(",", null)]
    [InlineData(";", null)]
    [InlineData("er=dd,cc,bb", new[] { "dd" })]
    [InlineData("er=dd,err=cc,errr=bb", new[] { "dd", "cc", "bb" })]
    [InlineData("errorcookie=dd,:(\"sa;", new[] { "dd" })]
    [InlineData("s;", null)]
    [InlineData("er=;,err=,errr=\\,errrr=\"", null)]
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
    public void AllExpectedCookieValueCharsPresent()
    {
        foreach (var c in Enumerable.Range(0x00, 0xFF).Select(x => (char)x))
        {
            var cookies = RequestCookieCollection.Parse(new StringValues(new[] { $"something={c}" }));
            if (c < 0x21 || c > 0x7E || c == '\\' || c == ',' || c == ';' || c == '\"')
            {
                Assert.Equal(0, cookies.Count);
            }
            else
            {
                Assert.Equal(c.ToString(), cookies.Single().Value);
            }
        }
    }
}
