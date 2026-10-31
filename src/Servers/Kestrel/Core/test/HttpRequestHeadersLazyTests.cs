// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class HttpRequestHeadersLazyTests
{
    public static IEnumerable<object[]> ValueReaders()
    {
        var references = typeof(HttpRequestHeaders).GetNestedType("HeaderReferences", BindingFlags.NonPublic);
        var identifiers = references.GetFields().Where(f => f.Name.StartsWith("_value", StringComparison.Ordinal))
            .Select(f => f.Name.Substring(6));
        var names = typeof(HeaderNames).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => identifiers.Contains(f.Name, StringComparer.OrdinalIgnoreCase))
            .Select(f => (string)f.GetValue(null))
            .Where(n => !n.StartsWith(':') && n is not ("Host" or "Connection" or "Transfer-Encoding"));
        foreach (var name in names.Concat(new[] { "Alt-Used", "X-Example", "Alt-Svc" }))
        {
            foreach (var lazy in new[] { false, true })
            {
                for (var reader = 0; reader < 12; reader++)
                {
                    yield return new object[] { name, lazy, reader };
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(ValueReaders))]
    public void EveryValueReaderOwnsAndMaterializesFreshInput(string name, bool lazy, int reader)
    {
        var headers = new HttpRequestHeaders(lazyRequestHeaders: lazy);
        IHeaderDictionary dictionary = headers;
        var input = "first-value"u8.ToArray();
        Append(headers, name, input);
        input.AsSpan().Fill((byte)'x');
        headers.OnHeadersComplete();
        Assert.Equal(lazy ? 1 : 0, headers.LazyValueCount);
        Assert.True(headers.Count == 1);
        Assert.True(dictionary.ContainsKey(name.ToUpperInvariant()));
        Assert.Contains(name, dictionary.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(lazy ? 1 : 0, headers.LazyValueCount);

        StringValues value;
        switch (reader)
        {
            case 0:
                value = dictionary[name.ToUpperInvariant()];
                break;
            case 1:
                value = ((IDictionary<string, StringValues>)headers)[name];
                break;
            case 2:
                Assert.True(dictionary.TryGetValue(name, out value));
                break;
            case 3:
                var copy = new KeyValuePair<string, StringValues>[1];
                dictionary.CopyTo(copy, 0);
                value = copy[0].Value;
                break;
            case 4:
                var enumerator = headers.GetEnumerator();
                Assert.True(enumerator.MoveNext());
                value = enumerator.Current.Value;
                break;
            case 5:
                value = Assert.Single((IEnumerable<KeyValuePair<string, StringValues>>)headers).Value;
                break;
            case 6:
                var untyped = ((IEnumerable)headers).GetEnumerator();
                Assert.True(untyped.MoveNext());
                value = ((KeyValuePair<string, StringValues>)untyped.Current).Value;
                break;
            case 7:
                value = Assert.Single(dictionary.Values);
                break;
            case 8:
                Assert.True(dictionary.Contains(new(name, "first-value")));
                value = dictionary[name];
                break;
            case 9:
                Assert.True(dictionary.Remove(new KeyValuePair<string, StringValues>(name, "first-value")));
                Assert.Equal(0, headers.LazyValueCount);
                Assert.Empty(dictionary);
                return;
            case 10:
                var property = typeof(IHeaderDictionary).GetProperty(name.Replace("-", ""),
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                value = property is null ? dictionary[name] : (StringValues)property.GetValue(dictionary);
                break;
            default:
                var internalProperty = typeof(HttpRequestHeaders).GetProperty("Header" + name.Replace("-", ""),
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                value = internalProperty is null ? dictionary[name] : (StringValues)internalProperty.GetValue(headers);
                break;
        }
        Assert.Equal("first-value", value);
        Assert.Equal(0, headers.LazyValueCount);
        Assert.Same(value[0], dictionary[name][0]);
    }

    [Theory]
    [InlineData("Accept")]
    [InlineData("Authorization")]
    [InlineData("Cookie")]
    [InlineData("Expect")]
    [InlineData("Upgrade")]
    [InlineData("Content-Type")]
    [InlineData("X-Example")]
    [InlineData("Alt-Svc")]
    public void DuplicatesAndMutationDiscardUnreadState(string name)
    {
        var headers = new HttpRequestHeaders(lazyRequestHeaders: true);
        IHeaderDictionary dictionary = headers;
        Append(headers, name, "first"u8);
        Append(headers, name.ToUpperInvariant(), "second"u8);
        Append(headers, name.ToLowerInvariant(), "third"u8);
        Assert.Equal(0, headers.LazyValueCount);
        Assert.Equal(new[] { "first", "second", "third" }, dictionary[name].ToArray());
        headers.Reset();
        Append(headers, name, "unread"u8);
        Assert.Throws<ArgumentException>(() => dictionary.Add(name.ToUpperInvariant(), "duplicate"));
        Assert.Equal(1, headers.LazyValueCount);
        dictionary[name] = "replacement";
        Assert.Equal(0, headers.LazyValueCount);
        Assert.Equal("replacement", dictionary[name]);
        headers.Reset();
        Append(headers, name, "removed"u8);
        Assert.True(dictionary.Remove(name.ToUpperInvariant()));
        Assert.Equal(0, headers.LazyValueCount);
        Append(headers, name, "cleared"u8);
        dictionary.Clear();
        Assert.Empty(dictionary);
        Assert.Equal(0, headers.LazyValueCount);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ResetAndReusePreserveOnlyDecodedKnownValues(bool reuse)
    {
        var headers = new HttpRequestHeaders(reuseHeaderValues: reuse, lazyRequestHeaders: true);
        IHeaderDictionary dictionary = headers;
        Append(headers, "Accept", "decoded"u8);
        Append(headers, "User-Agent", "unread"u8);
        Append(headers, "X-Example", "unknown"u8);
        headers.OnHeadersComplete();
        var decoded = dictionary.Accept[0];
        headers.Reset();
        Assert.Equal(0, headers.LazyValueCount);
        Append(headers, "Accept", "decoded"u8);
        headers.OnHeadersComplete();
        if (reuse)
        {
            Assert.Equal(0, headers.LazyValueCount);
            Assert.Same(decoded, dictionary.Accept[0]);
        }
        Assert.False(dictionary.ContainsKey("User-Agent"));
        Assert.False(dictionary.ContainsKey("X-Example"));
        headers.Reset();
        headers.OnHeadersComplete();
        Assert.Empty(dictionary);
        headers.Reset();
        Append(headers, "User-Agent", "new"u8);
        Assert.Equal("new", dictionary.UserAgent);
    }

    [Theory]
    [InlineData("Accept")]
    [InlineData("X-Example")]
    public void ValidationAndEncodingRemainAtIngestion(string name)
    {
        foreach (var value in new[] { "bad\0value"u8.ToArray(), "bad\rvalue"u8.ToArray(), "bad\nvalue"u8.ToArray(), new byte[] { 0xff } })
        {
            var headers = new HttpRequestHeaders(lazyRequestHeaders: true);
            Assert.Throws<InvalidOperationException>(() => Append(headers, name, value));
            Assert.Equal(0, headers.LazyValueCount);
        }
        var accepted = new HttpRequestHeaders(lazyRequestHeaders: true);
        Append(accepted, name, "\tcontrol\u0001"u8);
        Assert.Equal("\tcontrol\u0001", ((IHeaderDictionary)accepted)[name]);
        var unicode = new HttpRequestHeaders(lazyRequestHeaders: true);
        Append(unicode, name, Encoding.UTF8.GetBytes("caf\u00e9"));
        Assert.Equal(0, unicode.LazyValueCount);
        Assert.Equal("caf\u00e9", ((IHeaderDictionary)unicode)[name]);
        var calls = 0;
        var custom = new HttpRequestHeaders(encodingSelector: actual =>
        {
            Assert.Equal(name, actual);
            calls++;
            return Encoding.Latin1;
        }, lazyRequestHeaders: true);
        Append(custom, name, new byte[] { 0xe9 });
        Assert.Equal(1, calls);
        Assert.Equal(0, custom.LazyValueCount);
        Assert.Equal("\u00e9", ((IHeaderDictionary)custom)[name]);
    }

    [Fact]
    public void UnknownSelectorPreservesOriginalNameCasing()
    {
        string observed = null;
        var headers = new HttpRequestHeaders(encodingSelector: name => { observed = name; return null; }, lazyRequestHeaders: true);
        Append(headers, "aLt-SvC", "value"u8);
        Assert.Equal("aLt-SvC", observed);
    }

    [Fact]
    public void AggregateBudgetGrowthFallbackAndDiscardedHoles()
    {
        var headers = new HttpRequestHeaders(lazyRequestHeaders: true);
        IHeaderDictionary dictionary = headers;
        Append(headers, "Accept", Encoding.ASCII.GetBytes(new string('a', 255)));
        Append(headers, "X-Example", Encoding.ASCII.GetBytes(new string('b', 3841)));
        Assert.Equal(2, headers.LazyValueCount);
        Append(headers, "User-Agent", "fallback"u8);
        Assert.Equal(2, headers.LazyValueCount);
        Assert.True(dictionary.Remove("Accept"));
        Append(headers, "Cookie", "still-fallback"u8);
        Assert.Equal(1, headers.LazyValueCount);
        Assert.Equal(new string('b', 3841), dictionary["X-Example"]);
        Assert.Equal("fallback", dictionary.UserAgent);
        Assert.Equal("still-fallback", dictionary.Cookie);
        headers.Reset();
        Append(headers, "Accept", Encoding.ASCII.GetBytes(new string('c', 4096)));
        Assert.Equal(1, headers.LazyValueCount);
        Assert.Equal(4096, dictionary.Accept.ToString().Length);
        headers.Reset();
        Append(headers, "Accept", Encoding.ASCII.GetBytes(new string('d', 4097)));
        Assert.Equal(0, headers.LazyValueCount);
        Assert.Equal(4097, dictionary.Accept.ToString().Length);
    }

    [Fact]
    public void SensitiveBytesAreClearedOnGrowthReadMutationAndReset()
    {
        var headers = new HttpRequestHeaders(lazyRequestHeaders: true);
        IHeaderDictionary dictionary = headers;
        Append(headers, "Authorization", "secret"u8);
        var oldBuffer = GetBuffer(headers);
        Append(headers, "X-Example", Encoding.ASCII.GetBytes(new string('x', 512)));
        Assert.All(oldBuffer, b => Assert.Equal(0, b));
        var buffer = GetBuffer(headers);
        Assert.Equal("secret", dictionary.Authorization);
        Assert.All(buffer.Take(6), b => Assert.Equal(0, b));
        dictionary.Remove("X-Example");
        Assert.All(buffer, b => Assert.Equal(0, b));
        Append(headers, "Cookie", "secret-cookie"u8);
        Append(headers, "X-Secret", "secret-unknown"u8);
        headers.Reset();
        Assert.All(buffer, b => Assert.Equal(0, b));
    }

    [Theory]
    [InlineData("Accept", false)]
    [InlineData("Accept", true)]
    [InlineData("X-Example", false)]
    [InlineData("X-Example", true)]
    public void MutationDoesNotAllocateDiscardedString(string name, bool remove)
    {
        var headers = new HttpRequestHeaders(lazyRequestHeaders: true);
        IHeaderDictionary dictionary = headers;
        var bytes = Encoding.ASCII.GetBytes(new string('a', 1024));
        for (var i = 0; i < 2; i++)
        {
            headers.Reset();
            Append(headers, name, bytes);
            var before = GC.GetAllocatedBytesForCurrentThread();
            if (remove)
            {
                dictionary.Remove(name);
            }
            else
            {
                dictionary[name] = "replacement";
            }
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.Equal(0, allocated);
            Assert.Equal(0, headers.LazyValueCount);
        }
    }

    [Fact]
    public void UnknownEnumeratorsReadAuthoritativeCachedValuesWithoutInvalidation()
    {
        var headers = new HttpRequestHeaders(lazyRequestHeaders: true);
        for (var i = 0; i < 32; i++)
        {
            Append(headers, "X-" + i, Encoding.ASCII.GetBytes("value-" + i));
        }
        var first = ((IEnumerable<KeyValuePair<string, StringValues>>)headers).GetEnumerator();
        var second = headers.GetEnumerator();
        var count = 0;
        while (first.MoveNext())
        {
            Assert.True(second.MoveNext());
            Assert.Equal(first.Current, second.Current);
            count++;
        }
        Assert.Equal(32, count);
        Assert.Equal(0, headers.LazyValueCount);
        second.Reset();
        Assert.True(second.MoveNext());
        headers.Reset();
        Append(headers, "X-New", "next"u8);
        Assert.Equal("next", Assert.Single(headers).Value);
    }

    [Fact]
    public void UnknownCopyToPreservesDictionaryArgumentErrorsWithoutMaterializing()
    {
        var headers = new HttpRequestHeaders(lazyRequestHeaders: true);
        IHeaderDictionary dictionary = headers;
        Append(headers, "X-Example", "unread"u8);
        Assert.Throws<ArgumentNullException>(() => dictionary.CopyTo(null, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => dictionary.CopyTo(Array.Empty<KeyValuePair<string, StringValues>>(), 1));
        Assert.Throws<ArgumentException>(() => dictionary.CopyTo(Array.Empty<KeyValuePair<string, StringValues>>(), 0));
        Assert.Equal(1, headers.LazyValueCount);
    }

    [Fact]
    public void MissingEmptyReadOnlyAndTypedUnknownSemantics()
    {
        var headers = new HttpRequestHeaders(lazyRequestHeaders: true);
        IHeaderDictionary dictionary = headers;
        Assert.Equal(StringValues.Empty, dictionary["missing"]);
        Assert.Throws<KeyNotFoundException>(() => ((IDictionary<string, StringValues>)headers)["missing"]);
        foreach (var name in new[] { "Accept", "X-Example" })
        {
            Append(headers, name, ReadOnlySpan<byte>.Empty);
            Assert.True(dictionary.ContainsKey(name));
            Assert.Equal(1, dictionary[name].Count);
        }
        Append(headers, "Alt-Svc", "unread"u8);
        dictionary.AltSvc = "replacement";
        Assert.Equal(0, headers.LazyValueCount);
        Assert.Equal("replacement", dictionary.AltSvc);
        Assert.Throws<InvalidOperationException>(() => dictionary[""] = "value");
        headers.SetReadOnly();
        Assert.Throws<InvalidOperationException>(() => dictionary.Accept = "value");
        Assert.Throws<InvalidOperationException>(() => dictionary.AltSvc = "value");
        Assert.Throws<InvalidOperationException>(() => dictionary.Remove("X-Example"));
        Assert.Throws<InvalidOperationException>(() => dictionary.Clear());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ProtocolGatesRemainEagerExceptExplicitHttp11(int protocol)
    {
        var headers = new HttpRequestHeaders(lazyRequestHeaders: true);
        switch (protocol)
        {
            case 0:
                headers.Append("User-Agent"u8, "value"u8, true);
                break;
            case 1:
                headers.Append("User-Agent"u8, "value"u8, true, allowLazyValues: true);
                break;
            case 2:
                Assert.True(headers.TryHPackAppend(58, "value"u8, true));
                break;
            case 3:
                Assert.True(headers.TryQPackAppend(95, "value"u8, true));
                break;
        }
        Assert.Equal(protocol == 1 ? 1 : 0, headers.LazyValueCount);
        Assert.Equal("value", ((IHeaderDictionary)headers).UserAgent);
    }

    [Fact]
    public void FramingAndHostStayEagerAndValidateImmediately()
    {
        var headers = new HttpRequestHeaders(lazyRequestHeaders: true);
        Append(headers, "Host", "localhost"u8);
        Append(headers, "Connection", "keep-alive"u8);
        Append(headers, "Transfer-Encoding", "chunked"u8);
        Append(headers, "Content-Length", "12"u8);
        Assert.Equal(0, headers.LazyValueCount);
        Assert.Equal(12, headers.ContentLength);
        Assert.ThrowsAny<Microsoft.AspNetCore.Http.BadHttpRequestException>(() => Append(headers, "Content-Length", "12"u8));
        foreach (var value in new[] { "", "-1", "+1", "9223372036854775808", "1x" })
        {
            headers.Reset();
            Assert.ThrowsAny<Microsoft.AspNetCore.Http.BadHttpRequestException>(() => Append(headers, "Content-Length", Encoding.ASCII.GetBytes(value)));
        }
    }

    private static void Append(HttpRequestHeaders headers, string name, ReadOnlySpan<byte> value)
        => headers.Append(Encoding.ASCII.GetBytes(name), value, checkForNewlineChars: true, allowLazyValues: true);

    private static byte[] GetBuffer(HttpRequestHeaders headers)
    {
        var references = typeof(HttpRequestHeaders).GetField("_headers", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(headers);
        return (byte[])references.GetType().GetField("_bytes", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(references);
    }
}
