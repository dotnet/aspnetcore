// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class HttpRequestHeadersLazyTests
{
    [Theory]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    public void ValuesOwnInputAndSupportAllAccessPaths(int protocol, bool lazy)
    {
        var headers = new HttpRequestHeaders(lazyUserAgent: lazy);
        var input = Encoding.ASCII.GetBytes("agent-one");
        Append(headers, protocol, input);
        input.AsSpan().Fill((byte)'x');
        headers.OnHeadersComplete();

        Assert.True(headers.Count == 1);
        Assert.True(((IHeaderDictionary)headers).ContainsKey("uSeR-aGeNt"));
        Assert.Equal("agent-one", ((IHeaderDictionary)headers).UserAgent);
        Assert.True(((IHeaderDictionary)headers).TryGetValue("USER-AGENT", out var value));
        Assert.Equal("agent-one", value);
        Assert.Equal("agent-one", Assert.Single(headers).Value);
        var copy = new KeyValuePair<string, StringValues>[1];
        ((ICollection<KeyValuePair<string, StringValues>>)headers).CopyTo(copy, 0);
        Assert.Equal("agent-one", copy[0].Value);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void EnumerationAndCopyMaterializeUnreadValue(int protocol)
    {
        var headers = new HttpRequestHeaders(lazyUserAgent: true);
        Append(headers, protocol, "enumerated"u8);
        Assert.Equal("enumerated", Assert.Single(headers).Value);
        headers.Reset();
        Append(headers, protocol, "copied"u8);
        var copy = new KeyValuePair<string, StringValues>[1];
        ((ICollection<KeyValuePair<string, StringValues>>)headers).CopyTo(copy, 0);
        Assert.Equal("copied", copy[0].Value);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void DuplicatesAndMutationPreserveValues(int protocol)
    {
        var headers = new HttpRequestHeaders(lazyUserAgent: true);
        IHeaderDictionary dictionary = headers;
        Append(headers, protocol, "first"u8);
        Append(headers, protocol, "second"u8);
        Assert.Equal(new[] { "first", "second" }, dictionary["USER-AGENT"].ToArray());
        headers.Reset();
        Append(headers, protocol, "unread"u8);
        ((IHeaderDictionary)headers).UserAgent = "replacement";
        Assert.Equal("replacement", dictionary["user-agent"]);
        Assert.True(dictionary.Remove("USER-AGENT"));
        Assert.False(dictionary.ContainsKey("User-Agent"));
        Append(headers, protocol, "after-remove"u8);
        Assert.Equal("after-remove", dictionary["User-Agent"]);
        dictionary.Clear();
        Assert.Empty(headers);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    public void ResetDoesNotLeakPreviousUnreadValue(int protocol, bool reuse)
    {
        var headers = new HttpRequestHeaders(reuseHeaderValues: reuse, lazyUserAgent: true);
        Append(headers, protocol, "previous"u8);
        headers.OnHeadersComplete();
        headers.Reset();
        headers.OnHeadersComplete();
        Assert.Empty(headers);
        Assert.Equal(StringValues.Empty, ((IHeaderDictionary)headers).UserAgent);
        Append(headers, protocol, "next"u8);
        headers.OnHeadersComplete();
        Assert.Equal("next", ((IHeaderDictionary)headers)["user-agent"]);
        headers.Reset();
        Append(headers, protocol, "changed"u8);
        headers.OnHeadersComplete();
        Assert.Equal("changed", ((IHeaderDictionary)headers)["user-agent"]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void FallbacksPreserveEncodingAndValidation(int protocol)
    {
        var headers = new HttpRequestHeaders(lazyUserAgent: true);
        IHeaderDictionary dictionary = headers;
        Assert.Throws<InvalidOperationException>(() => Append(headers, protocol, "bad\0value"u8));
        Assert.Throws<InvalidOperationException>(() => Append(headers, protocol, "bad\rvalue"u8));
        Assert.Throws<InvalidOperationException>(() => Append(headers, protocol, "bad\nvalue"u8));
        Assert.Throws<InvalidOperationException>(() => Append(headers, protocol, new byte[] { 0xff }));
        Append(headers, protocol, Encoding.UTF8.GetBytes("caf\u00e9"));
        Assert.Equal("caf\u00e9", dictionary["User-Agent"]);
        headers.Reset();
        Append(headers, protocol, ReadOnlySpan<byte>.Empty);
        Assert.Equal(string.Empty, dictionary["User-Agent"].ToString());
        Assert.True(dictionary.ContainsKey("User-Agent"));
        headers.Reset();
        var large = new string('a', 4097);
        Append(headers, protocol, Encoding.ASCII.GetBytes(large));
        Assert.Equal(large, dictionary["User-Agent"]);

        var calls = 0;
        var custom = new HttpRequestHeaders(encodingSelector: name =>
        {
            Assert.Equal("User-Agent", name);
            calls++;
            return Encoding.Latin1;
        }, lazyUserAgent: true);
        Append(custom, protocol, new byte[] { 0xe9 });
        Assert.Equal(1, calls);
        Assert.Equal("\u00e9", ((IHeaderDictionary)custom)["User-Agent"]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void IgnoredChangingValuesAllocateNoStringsAfterWarmup(int protocol)
    {
        var headers = new HttpRequestHeaders(lazyUserAgent: true);
        Append(headers, protocol, "first"u8);
        headers.OnHeadersComplete();
        headers.Reset();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100; i++)
        {
            Append(headers, protocol, (i & 1) == 0 ? "other"u8 : "first"u8);
            headers.OnHeadersComplete();
            headers.Reset();
        }
        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void RepeatedReadValuesReuseMaterializedString(int protocol)
    {
        var headers = new HttpRequestHeaders(lazyUserAgent: true);
        Append(headers, protocol, "repeated"u8);
        headers.OnHeadersComplete();
        var first = ((IHeaderDictionary)headers).UserAgent[0];
        headers.Reset();
        Append(headers, protocol, "repeated"u8);
        headers.OnHeadersComplete();
        Assert.Same(first, ((IHeaderDictionary)headers).UserAgent[0]);
    }

    [Fact]
    public void UnknownAndOtherKnownHeadersAreUnchanged()
    {
        var headers = new HttpRequestHeaders(lazyUserAgent: true);
        headers.Append("X-Example"u8, "first"u8, checkForNewlineChars: true);
        headers.Append("x-example"u8, "second"u8, checkForNewlineChars: true);
        headers.Append("Accept"u8, "text/plain"u8, checkForNewlineChars: true);
        Assert.Equal(new[] { "first", "second" }, ((IHeaderDictionary)headers)["X-EXAMPLE"].ToArray());
        Assert.Equal("text/plain", ((IHeaderDictionary)headers)["accept"]);
        headers.Reset();
        headers.OnHeadersComplete();
        Assert.Empty(headers);
    }

    private static void Append(HttpRequestHeaders headers, int protocol, ReadOnlySpan<byte> value)
    {
        switch (protocol)
        {
            case 1:
                headers.Append("uSeR-aGeNt"u8, value, checkForNewlineChars: true);
                break;
            case 2:
                Assert.True(headers.TryHPackAppend(58, value, checkForNewlineChars: true));
                break;
            case 3:
                Assert.True(headers.TryQPackAppend(95, value, checkForNewlineChars: true));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(protocol));
        }
    }
}
