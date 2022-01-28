// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class HttpUtilitiesTest
{
    [Theory]
    [InlineData("CONNECT / HTTP/1.1", true, "CONNECT", (int)HttpMethod.Connect)]
    [InlineData("DELETE / HTTP/1.1", true, "DELETE", (int)HttpMethod.Delete)]
    [InlineData("GET / HTTP/1.1", true, "GET", (int)HttpMethod.Get)]
    [InlineData("HEAD / HTTP/1.1", true, "HEAD", (int)HttpMethod.Head)]
    [InlineData("PATCH / HTTP/1.1", true, "PATCH", (int)HttpMethod.Patch)]
    [InlineData("POST / HTTP/1.1", true, "POST", (int)HttpMethod.Post)]
    [InlineData("PUT / HTTP/1.1", true, "PUT", (int)HttpMethod.Put)]
    [InlineData("OPTIONS / HTTP/1.1", true, "OPTIONS", (int)HttpMethod.Options)]
    [InlineData("TRACE / HTTP/1.1", true, "TRACE", (int)HttpMethod.Trace)]
    [InlineData("GET/ HTTP/1.1", false, null, (int)HttpMethod.Custom)]
    [InlineData("get / HTTP/1.1", false, null, (int)HttpMethod.Custom)]
    [InlineData("GOT / HTTP/1.1", false, null, (int)HttpMethod.Custom)]
    [InlineData("ABC / HTTP/1.1", false, null, (int)HttpMethod.Custom)]
    [InlineData("PO / HTTP/1.1", false, null, (int)HttpMethod.Custom)]
    [InlineData("PO ST / HTTP/1.1", false, null, (int)HttpMethod.Custom)]
    [InlineData("short ", false, null, (int)HttpMethod.Custom)]
    public void GetsKnownMethod(string input, bool expectedResult, string expectedKnownString, int intExpectedMethod)
    {
        var expectedMethod = (HttpMethod)intExpectedMethod;
        // Arrange
        var block = new ReadOnlySpan<byte>(Encoding.ASCII.GetBytes(input));

        // Act
        var result = block.GetKnownMethod(out var knownMethod, out var length);

        string toString = null;
        if (knownMethod != HttpMethod.Custom)
        {
            toString = HttpUtilities.MethodToString(knownMethod);
        }

        // Assert
        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedMethod, knownMethod);
        Assert.Equal(toString, expectedKnownString);
        Assert.Equal(length, expectedKnownString?.Length ?? 0);
    }

    [Theory]
    [InlineData("HTTP/1.0\r", true, "HTTP/1.0", (int)HttpVersion.Http10)]
    [InlineData("HTTP/1.1\r", true, "HTTP/1.1", (int)HttpVersion.Http11)]
    [InlineData("HTTP/1.1\rmoretext", true, "HTTP/1.1", (int)HttpVersion.Http11)]
    [InlineData("HTTP/3.0\r", false, null, (int)HttpVersion.Unknown)]
    [InlineData("http/1.0\r", false, null, (int)HttpVersion.Unknown)]
    [InlineData("http/1.1\r", false, null, (int)HttpVersion.Unknown)]
    [InlineData("short ", false, null, (int)HttpVersion.Unknown)]
    public void GetsKnownVersion(string input, bool expectedResult, string expectedKnownString, int intVersion)
    {
        var version = (HttpVersion)intVersion;
        // Arrange
        var block = new ReadOnlySpan<byte>(Encoding.ASCII.GetBytes(input));

        // Act
        var result = block.GetKnownVersion(out HttpVersion knownVersion, out var length);
        string toString = null;
        if (knownVersion != HttpVersion.Unknown)
        {
            toString = HttpUtilities.VersionToString(knownVersion);
        }

        // Assert
        Assert.Equal(version, knownVersion);
        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedKnownString, toString);
        Assert.Equal(expectedKnownString?.Length ?? 0, length);
    }

    [Theory]
    [InlineData("HTTP/1.0\r", "HTTP/1.0")]
    [InlineData("HTTP/1.1\r", "HTTP/1.1")]
    public void KnownVersionsAreInterned(string input, string expected)
    {
        TestKnownStringsInterning(input, expected, span =>
        {
            HttpUtilities.GetKnownVersion(span, out var version, out var _);
            return HttpUtilities.VersionToString(version);
        });
    }

    [Theory]
    [InlineData("https://host/", "https://")]
    [InlineData("http://host/", "http://")]
    public void KnownSchemesAreInterned(string input, string expected)
    {
        TestKnownStringsInterning(input, expected, span =>
        {
            HttpUtilities.GetKnownHttpScheme(span, out var scheme);
            return HttpUtilities.SchemeToString(scheme);
        });
    }

    [Theory]
    [InlineData("CONNECT / HTTP/1.1", "CONNECT")]
    [InlineData("DELETE / HTTP/1.1", "DELETE")]
    [InlineData("GET / HTTP/1.1", "GET")]
    [InlineData("HEAD / HTTP/1.1", "HEAD")]
    [InlineData("PATCH / HTTP/1.1", "PATCH")]
    [InlineData("POST / HTTP/1.1", "POST")]
    [InlineData("PUT / HTTP/1.1", "PUT")]
    [InlineData("OPTIONS / HTTP/1.1", "OPTIONS")]
    [InlineData("TRACE / HTTP/1.1", "TRACE")]
    public void KnownMethodsAreInterned(string input, string expected)
    {
        TestKnownStringsInterning(input, expected, span =>
        {
            HttpUtilities.GetKnownMethod(span, out var method, out var length);
            return HttpUtilities.MethodToString(method);
        });
    }

    private void TestKnownStringsInterning(string input, string expected, Func<byte[], string> action)
    {
        // Act
        var knownString1 = action(Encoding.ASCII.GetBytes(input));
        var knownString2 = action(Encoding.ASCII.GetBytes(input));

        // Assert
        Assert.Equal(knownString1, expected);
        Assert.Same(knownString1, knownString2);
    }

    public static TheoryData<string> HostHeaderData
    {
        get
        {
            return new TheoryData<string> {
                    "z",
                    "1",
                    "y:1",
                    "1:1",
                    "[ABCdef]",
                    "[abcDEF]:0",
                    "[abcdef:127.2355.1246.114]:0",
                    "[::1]:80",
                    "127.0.0.1:80",
                    "900.900.900.900:9523547852",
                    "foo",
                    "foo:234",
                    "foo.bar.baz",
                    "foo.BAR.baz:46245",
                    "foo.ba-ar.baz:46245",
                    "-foo:1234",
                    "xn--asdfaf:134",
                    "-",
                    "_",
                    "~",
                    "!",
                    "$",
                    "'",
                    "(",
                    ")",
                };
        }
    }

    [Theory]
    [MemberData(nameof(HostHeaderData))]
    public void ValidHostHeadersParsed(string host)
    {
        Assert.True(HttpUtilities.IsHostHeaderValid(host));
    }

    public static TheoryData<string> HostHeaderInvalidData
    {
        get
        {
            // see https://tools.ietf.org/html/rfc7230#section-5.4
            var data = new TheoryData<string> {
                    "[]", // Too short
                    "[::]", // Too short
                    "[ghijkl]", // Non-hex
                    "[afd:adf:123", // Incomplete
                    "[afd:adf]123", // Missing :
                    "[afd:adf]:", // Missing port digits
                    "[afd adf]", // Space
                    "[ad-314]", // dash
                    ":1234", // Missing host
                    "a:b:c", // Missing []
                    "::1", // Missing []
                    "::", // Missing everything
                    "abcd:1abcd", // Letters in port
                    "abcd:1.2", // Dot in port
                    "1.2.3.4:", // Missing port digits
                    "1.2 .4", // Space
                };

            // These aren't allowed anywhere in the host header
            var invalid = "\"#%*+,/;<=>?@[]\\^`{}|";
            foreach (var ch in invalid)
            {
                data.Add(ch.ToString());
            }

            invalid = "!\"#$%&'()*+,/;<=>?@[]\\^_`{}|~-";
            foreach (var ch in invalid)
            {
                data.Add("[abd" + ch + "]:1234");
            }

            invalid = "!\"#$%&'()*+,/;<=>?@[]\\^_`{}|~:abcABC-.";
            foreach (var ch in invalid)
            {
                data.Add("a.b.c:" + ch);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(HostHeaderInvalidData))]
    public void InvalidHostHeadersRejected(string host)
    {
        Assert.False(HttpUtilities.IsHostHeaderValid(host));
    }

    public static TheoryData<Func<string, Encoding>> ExceptionThrownForCRLFData
    {
        get
        {
            return new TheoryData<Func<string, Encoding>> {
                    KestrelServerOptions.DefaultHeaderEncodingSelector,
                    str => null,
                    str => Encoding.Latin1
                };
        }
    }

    [Theory]
    [MemberData(nameof(ExceptionThrownForCRLFData))]
    private void ExceptionThrownForCRLF(Func<string, Encoding> selector)
    {
        byte[] encodedBytes = { 0x01, 0x0A, 0x0D };
        Assert.Throws<InvalidOperationException>(() =>
            HttpUtilities.GetRequestHeaderString(encodedBytes.AsSpan(), HeaderNames.Accept, selector, checkForNewlineChars: true));
    }

    [Theory]
    [MemberData(nameof(ExceptionThrownForCRLFData))]
    private void ExceptionNotThrownForCRLF(Func<string, Encoding> selector)
    {
        byte[] encodedBytes = { 0x01, 0x0A, 0x0D };
        HttpUtilities.GetRequestHeaderString(encodedBytes.AsSpan(), HeaderNames.Accept, selector, checkForNewlineChars: false);
    }
}
