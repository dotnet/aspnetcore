// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Xunit;
using static CodeGenerator.KnownHeaders;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class HttpRequestHeadersTests
{
    [Fact]
    public void InitialDictionaryIsEmpty()
    {
        IDictionary<string, StringValues> headers = new HttpRequestHeaders();

        Assert.Empty(headers);
        Assert.False(headers.IsReadOnly);
    }

    [Fact]
    public void SettingUnknownHeadersWorks()
    {
        IDictionary<string, StringValues> headers = new HttpRequestHeaders();

        headers["custom"] = new[] { "value" };

        var header = Assert.Single(headers["custom"]);
        Assert.Equal("value", header);
    }

    [Fact]
    public void SettingKnownHeadersWorks()
    {
        IDictionary<string, StringValues> headers = new HttpRequestHeaders();

        headers["host"] = new[] { "value" };
        headers["content-length"] = new[] { "0" };

        var host = Assert.Single(headers["host"]);
        var contentLength = Assert.Single(headers["content-length"]);
        Assert.Equal("value", host);
        Assert.Equal("0", contentLength);
    }

    [Fact]
    public void KnownAndCustomHeaderCountAddedTogether()
    {
        IDictionary<string, StringValues> headers = new HttpRequestHeaders();

        headers["host"] = new[] { "value" };
        headers["custom"] = new[] { "value" };
        headers["Content-Length"] = new[] { "0" };

        Assert.Equal(3, headers.Count);
    }

    [Fact]
    public void TryGetValueWorksForKnownAndUnknownHeaders()
    {
        IDictionary<string, StringValues> headers = new HttpRequestHeaders();

        StringValues value;
        Assert.False(headers.TryGetValue("host", out value));
        Assert.False(headers.TryGetValue("custom", out value));
        Assert.False(headers.TryGetValue("Content-Length", out value));

        headers["host"] = new[] { "value" };
        Assert.True(headers.TryGetValue("host", out value));
        Assert.False(headers.TryGetValue("custom", out value));
        Assert.False(headers.TryGetValue("Content-Length", out value));

        headers["custom"] = new[] { "value" };
        Assert.True(headers.TryGetValue("host", out value));
        Assert.True(headers.TryGetValue("custom", out value));
        Assert.False(headers.TryGetValue("Content-Length", out value));

        headers["Content-Length"] = new[] { "0" };
        Assert.True(headers.TryGetValue("host", out value));
        Assert.True(headers.TryGetValue("custom", out value));
        Assert.True(headers.TryGetValue("Content-Length", out value));
    }

    [Fact]
    public void SameExceptionThrownForMissingKey()
    {
        IDictionary<string, StringValues> headers = new HttpRequestHeaders();

        Assert.Throws<KeyNotFoundException>(() => headers["custom"]);
        Assert.Throws<KeyNotFoundException>(() => headers["host"]);
        Assert.Throws<KeyNotFoundException>(() => headers["Content-Length"]);
    }

    [Fact]
    public void IHeaderDictionaryMembersReturnStringValuesEmptyForMissingHeaders()
    {
        IHeaderDictionary headers = new HttpRequestHeaders();

        // StringValues.Empty.Equals(default(StringValues)), so we check if the implicit conversion
        // to string[] returns null or Array.Empty<string>() to tell the difference.
        Assert.Same(Array.Empty<string>(), (string[])headers["custom"]);
        Assert.Same(Array.Empty<string>(), (string[])headers["host"]);
        Assert.Same(Array.Empty<string>(), (string[])headers["Content-Length"]);

        // Test both optimized and non-optimized properties.
        Assert.Same(Array.Empty<string>(), (string[])headers.Host);
        Assert.Same(Array.Empty<string>(), (string[])headers.AltSvc);
    }

    [Fact]
    public void EntriesCanBeEnumeratedAfterResets()
    {
        HttpRequestHeaders headers = new HttpRequestHeaders();

        EnumerateEntries((IHeaderDictionary)headers);
        headers.Reset();
        EnumerateEntries((IDictionary<string, StringValues>)headers);
        headers.Reset();
        EnumerateEntries((IHeaderDictionary)headers);
        headers.Reset();
        EnumerateEntries((IDictionary<string, StringValues>)headers);
    }

    [Fact]
    public void ClearPseudoRequestHeadersPlusResetClearsHeaderReferenceValue()
    {
        const BindingFlags privateFlags = BindingFlags.NonPublic | BindingFlags.Instance;

        HttpRequestHeaders headers = new HttpRequestHeaders(reuseHeaderValues: false);
        headers.HeaderMethod = "GET";
        headers.ClearPseudoRequestHeaders();
        headers.Reset();

        // Hacky but required because header references is private.
        var headerReferences = typeof(HttpRequestHeaders).GetField("_headers", privateFlags).GetValue(headers);
        var methodValue = (StringValues)headerReferences.GetType().GetField("_Method").GetValue(headerReferences);

        Assert.Equal(StringValues.Empty, methodValue);
    }

    [Fact]
    public void EnumeratorNotReusedBeforeReset()
    {
        HttpRequestHeaders headers = new HttpRequestHeaders();
        IEnumerable<KeyValuePair<string, StringValues>> enumerable = headers;

        var enumerator0 = enumerable.GetEnumerator();
        var enumerator1 = enumerable.GetEnumerator();

        Assert.NotSame(enumerator0, enumerator1);
    }

    [Fact]
    public void EnumeratorReusedAfterReset()
    {
        HttpRequestHeaders headers = new HttpRequestHeaders();
        IEnumerable<KeyValuePair<string, StringValues>> enumerable = headers;

        var enumerator0 = enumerable.GetEnumerator();
        headers.Reset();
        var enumerator1 = enumerable.GetEnumerator();

        Assert.Same(enumerator0, enumerator1);
    }

    private static void EnumerateEntries(IHeaderDictionary headers)
    {
        var v1 = new[] { "localhost" };
        var v2 = new[] { "0" };
        var v3 = new[] { "value" };
        headers.Host = v1;
        headers.ContentLength = 0;
        headers["custom"] = v3;

        Assert.Equal(
            new[] {
                    new KeyValuePair<string, StringValues>("Host", v1),
                    new KeyValuePair<string, StringValues>("Content-Length", v2),
                    new KeyValuePair<string, StringValues>("custom", v3),
            },
            headers);
    }

    private static void EnumerateEntries(IDictionary<string, StringValues> headers)
    {
        var v1 = new[] { "localhost" };
        var v2 = new[] { "0" };
        var v3 = new[] { "value" };
        headers["host"] = v1;
        headers["Content-Length"] = v2;
        headers["custom"] = v3;

        Assert.Equal(
            new[] {
                    new KeyValuePair<string, StringValues>("Host", v1),
                    new KeyValuePair<string, StringValues>("Content-Length", v2),
                    new KeyValuePair<string, StringValues>("custom", v3),
            },
            headers);
    }

    [Fact]
    public void KeysAndValuesCanBeEnumerated()
    {
        IDictionary<string, StringValues> headers = new HttpRequestHeaders();
        StringValues v1 = new[] { "localhost" };
        StringValues v2 = new[] { "0" };
        StringValues v3 = new[] { "value" };
        headers["host"] = v1;
        headers["Content-Length"] = v2;
        headers["custom"] = v3;

        Assert.Equal<string>(
            new[] { "Host", "Content-Length", "custom" },
            headers.Keys);

        Assert.Equal<StringValues>(
            new[] { v1, v2, v3 },
            headers.Values);
    }

    [Fact]
    public void ContainsAndContainsKeyWork()
    {
        IDictionary<string, StringValues> headers = new HttpRequestHeaders();
        var kv1 = new KeyValuePair<string, StringValues>("host", new[] { "localhost" });
        var kv2 = new KeyValuePair<string, StringValues>("custom", new[] { "value" });
        var kv3 = new KeyValuePair<string, StringValues>("Content-Length", new[] { "0" });
        var kv1b = new KeyValuePair<string, StringValues>("host", new[] { "not-localhost" });
        var kv2b = new KeyValuePair<string, StringValues>("custom", new[] { "not-value" });
        var kv3b = new KeyValuePair<string, StringValues>("Content-Length", new[] { "1" });

        Assert.False(headers.ContainsKey("host"));
        Assert.False(headers.ContainsKey("custom"));
        Assert.False(headers.ContainsKey("Content-Length"));
        Assert.False(headers.Contains(kv1));
        Assert.False(headers.Contains(kv2));
        Assert.False(headers.Contains(kv3));

        headers["host"] = kv1.Value;
        Assert.True(headers.ContainsKey("host"));
        Assert.False(headers.ContainsKey("custom"));
        Assert.False(headers.ContainsKey("Content-Length"));
        Assert.True(headers.Contains(kv1));
        Assert.False(headers.Contains(kv2));
        Assert.False(headers.Contains(kv3));
        Assert.False(headers.Contains(kv1b));
        Assert.False(headers.Contains(kv2b));
        Assert.False(headers.Contains(kv3b));

        headers["custom"] = kv2.Value;
        Assert.True(headers.ContainsKey("host"));
        Assert.True(headers.ContainsKey("custom"));
        Assert.False(headers.ContainsKey("Content-Length"));
        Assert.True(headers.Contains(kv1));
        Assert.True(headers.Contains(kv2));
        Assert.False(headers.Contains(kv3));
        Assert.False(headers.Contains(kv1b));
        Assert.False(headers.Contains(kv2b));
        Assert.False(headers.Contains(kv3b));

        headers["Content-Length"] = kv3.Value;
        Assert.True(headers.ContainsKey("host"));
        Assert.True(headers.ContainsKey("custom"));
        Assert.True(headers.ContainsKey("Content-Length"));
        Assert.True(headers.Contains(kv1));
        Assert.True(headers.Contains(kv2));
        Assert.True(headers.Contains(kv3));
        Assert.False(headers.Contains(kv1b));
        Assert.False(headers.Contains(kv2b));
        Assert.False(headers.Contains(kv3b));
    }

    [Fact]
    public void AddWorksLikeSetAndThrowsIfKeyExists()
    {
        IDictionary<string, StringValues> headers = new HttpRequestHeaders();

        StringValues value;
        Assert.False(headers.TryGetValue("host", out value));
        Assert.False(headers.TryGetValue("custom", out value));
        Assert.False(headers.TryGetValue("Content-Length", out value));

        headers.Add("host", new[] { "localhost" });
        headers.Add("custom", new[] { "value" });
        headers.Add("Content-Length", new[] { "0" });
        Assert.True(headers.TryGetValue("host", out value));
        Assert.True(headers.TryGetValue("custom", out value));
        Assert.True(headers.TryGetValue("Content-Length", out value));

        Assert.Throws<ArgumentException>(() => headers.Add("host", new[] { "localhost" }));
        Assert.Throws<ArgumentException>(() => headers.Add("custom", new[] { "value" }));
        Assert.Throws<ArgumentException>(() => headers.Add("Content-Length", new[] { "0" }));
        Assert.True(headers.TryGetValue("host", out value));
        Assert.True(headers.TryGetValue("custom", out value));
        Assert.True(headers.TryGetValue("Content-Length", out value));
    }

    [Fact]
    public void ClearRemovesAllHeaders()
    {
        IDictionary<string, StringValues> headers = new HttpRequestHeaders();
        headers.Add("host", new[] { "localhost" });
        headers.Add("custom", new[] { "value" });
        headers.Add("Content-Length", new[] { "0" });

        StringValues value;
        Assert.Equal(3, headers.Count);
        Assert.True(headers.TryGetValue("host", out value));
        Assert.True(headers.TryGetValue("custom", out value));
        Assert.True(headers.TryGetValue("Content-Length", out value));

        headers.Clear();

        Assert.Empty(headers);
        Assert.False(headers.TryGetValue("host", out value));
        Assert.False(headers.TryGetValue("custom", out value));
        Assert.False(headers.TryGetValue("Content-Length", out value));
    }

    [Fact]
    public void RemoveTakesHeadersOutOfDictionary()
    {
        IDictionary<string, StringValues> headers = new HttpRequestHeaders();
        headers.Add("host", new[] { "localhost" });
        headers.Add("custom", new[] { "value" });
        headers.Add("Content-Length", new[] { "0" });

        StringValues value;
        Assert.Equal(3, headers.Count);
        Assert.True(headers.TryGetValue("host", out value));
        Assert.True(headers.TryGetValue("custom", out value));
        Assert.True(headers.TryGetValue("Content-Length", out value));

        Assert.True(headers.Remove("host"));
        Assert.False(headers.Remove("host"));

        Assert.Equal(2, headers.Count);
        Assert.False(headers.TryGetValue("host", out value));
        Assert.True(headers.TryGetValue("custom", out value));

        Assert.True(headers.Remove("custom"));
        Assert.False(headers.Remove("custom"));

        Assert.Single(headers);
        Assert.False(headers.TryGetValue("host", out value));
        Assert.False(headers.TryGetValue("custom", out value));
        Assert.True(headers.TryGetValue("Content-Length", out value));

        Assert.True(headers.Remove("Content-Length"));
        Assert.False(headers.Remove("Content-Length"));

        Assert.Empty(headers);
        Assert.False(headers.TryGetValue("host", out value));
        Assert.False(headers.TryGetValue("custom", out value));
        Assert.False(headers.TryGetValue("Content-Length", out value));
    }

    [Fact]
    public void CopyToMovesDataIntoArray()
    {
        IDictionary<string, StringValues> headers = new HttpRequestHeaders();
        headers.Add("host", new[] { "localhost" });
        headers.Add("Content-Length", new[] { "0" });
        headers.Add("custom", new[] { "value" });

        var entries = new KeyValuePair<string, StringValues>[5];
        headers.CopyTo(entries, 1);

        Assert.Null(entries[0].Key);
        Assert.Equal(new StringValues(), entries[0].Value);

        Assert.Equal("Host", entries[1].Key);
        Assert.Equal(new[] { "localhost" }, entries[1].Value.ToArray());

        Assert.Equal("Content-Length", entries[2].Key);
        Assert.Equal(new[] { "0" }, entries[2].Value.ToArray());

        Assert.Equal("custom", entries[3].Key);
        Assert.Equal(new[] { "value" }, entries[3].Value.ToArray());

        Assert.Null(entries[4].Key);
        Assert.Equal(new StringValues(), entries[4].Value);
    }

    [Fact]
    public void AppendThrowsWhenHeaderNameContainsNonASCIICharacters()
    {
        var headers = new HttpRequestHeaders();
        const string key = "\u00141\u00F3d\017c";

#pragma warning disable CS0618 // Type or member is obsolete
        var exception = Assert.Throws<BadHttpRequestException>(
#pragma warning restore CS0618 // Type or member is obsolete
                () => headers.Append(Encoding.Latin1.GetBytes(key), Encoding.ASCII.GetBytes("value"), checkForNewlineChars: false));
        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [Theory]
    [MemberData(nameof(KnownRequestHeaders))]
    public void ValueReuseOnlyWhenAllowed(bool reuseValue, string headerName)
    {
        const string HeaderValue = "Hello";

        var headers = new HttpRequestHeaders(reuseHeaderValues: reuseValue);

        for (var i = 0; i < 6; i++)
        {
            var prevName = ChangeNameCase(headerName, variant: i);
            var nextName = ChangeNameCase(headerName, variant: i + 1);

            var values = GetHeaderValues(headers, prevName, nextName, HeaderValue, HeaderValue);

            Assert.Equal(HeaderValue, values.PrevHeaderValue);
            Assert.NotSame(HeaderValue, values.PrevHeaderValue);

            Assert.Equal(HeaderValue, values.NextHeaderValue);
            Assert.NotSame(HeaderValue, values.NextHeaderValue);

            Assert.Equal(values.PrevHeaderValue, values.NextHeaderValue);
            if (reuseValue)
            {
                // When materialized string is reused previous and new should be the same object
                Assert.Same(values.PrevHeaderValue, values.NextHeaderValue);
            }
            else
            {
                // When materialized string is not reused previous and new should be the different objects
                Assert.NotSame(values.PrevHeaderValue, values.NextHeaderValue);
            }
        }
    }

    [Theory]
    [MemberData(nameof(KnownRequestHeaders))]
    public void ValueReuseChangedValuesOverwrite(bool reuseValue, string headerName)
    {
        const string HeaderValue1 = "Hello1";
        const string HeaderValue2 = "Hello2";
        var headers = new HttpRequestHeaders(reuseHeaderValues: reuseValue);

        for (var i = 0; i < 6; i++)
        {
            var prevName = ChangeNameCase(headerName, variant: i);
            var nextName = ChangeNameCase(headerName, variant: i + 1);

            var values = GetHeaderValues(headers, prevName, nextName, HeaderValue1, HeaderValue2);

            Assert.Equal(HeaderValue1, values.PrevHeaderValue);
            Assert.NotSame(HeaderValue1, values.PrevHeaderValue);

            Assert.Equal(HeaderValue2, values.NextHeaderValue);
            Assert.NotSame(HeaderValue2, values.NextHeaderValue);

            Assert.NotEqual(values.PrevHeaderValue, values.NextHeaderValue);
        }
    }

    [Theory]
    [MemberData(nameof(KnownRequestHeaders))]
    public void ValueReuseMissingValuesClear(bool reuseValue, string headerName)
    {
        const string HeaderValue1 = "Hello1";
        var headers = new HttpRequestHeaders(reuseHeaderValues: reuseValue);

        for (var i = 0; i < 6; i++)
        {
            var prevName = ChangeNameCase(headerName, variant: i);
            var nextName = ChangeNameCase(headerName, variant: i + 1);

            var values = GetHeaderValues(headers, prevName, nextName, HeaderValue1, nextValue: null);

            Assert.Equal(HeaderValue1, values.PrevHeaderValue);
            Assert.NotSame(HeaderValue1, values.PrevHeaderValue);

            Assert.Equal(string.Empty, values.NextHeaderValue);

            Assert.NotEqual(values.PrevHeaderValue, values.NextHeaderValue);
        }
    }

    [Theory]
    [MemberData(nameof(KnownRequestHeaders))]
    public void ValueReuseNeverWhenNotAscii(bool reuseValue, string headerName)
    {
        const string HeaderValue = "Hello \u03a0";

        var headers = new HttpRequestHeaders(reuseHeaderValues: reuseValue);

        for (var i = 0; i < 6; i++)
        {
            var prevName = ChangeNameCase(headerName, variant: i);
            var nextName = ChangeNameCase(headerName, variant: i + 1);

            var values = GetHeaderValues(headers, prevName, nextName, HeaderValue, HeaderValue);

            Assert.Equal(HeaderValue, values.PrevHeaderValue);
            Assert.NotSame(HeaderValue, values.PrevHeaderValue);

            Assert.Equal(HeaderValue, values.NextHeaderValue);
            Assert.NotSame(HeaderValue, values.NextHeaderValue);

            Assert.Equal(values.PrevHeaderValue, values.NextHeaderValue);

            Assert.NotSame(values.PrevHeaderValue, values.NextHeaderValue);
        }
    }

    [Theory]
    [MemberData(nameof(KnownRequestHeaders))]
    public void ValueReuseLatin1NotConfusedForUtf16AndStillRejected(bool reuseValue, string headerName)
    {
        var headers = new HttpRequestHeaders(reuseHeaderValues: reuseValue);

        var headerValue = new char[127]; // 64 + 32 + 16 + 8 + 4 + 2 + 1
        for (var i = 0; i < headerValue.Length; i++)
        {
            headerValue[i] = 'a';
        }

        for (var i = 0; i < headerValue.Length; i++)
        {
            // Set non-ascii Latin char that is valid Utf16 when widened; but not a valid utf8 -> utf16 conversion.
            headerValue[i] = '\u00a3';

            for (var mode = 0; mode <= 1; mode++)
            {
                string headerValueUtf16Latin1CrossOver;
                if (mode == 0)
                {
                    // Full length
                    headerValueUtf16Latin1CrossOver = new string(headerValue);
                }
                else
                {
                    // Truncated length (to ensure different paths from changing lengths in matching)
                    headerValueUtf16Latin1CrossOver = new string(headerValue.AsSpan().Slice(0, i + 1));
                }

                headers.Reset();

                var headerNameBytes = Encoding.ASCII.GetBytes(headerName).AsSpan();
                var prevSpan = Encoding.UTF8.GetBytes(headerValueUtf16Latin1CrossOver).AsSpan();

                headers.Append(headerNameBytes, prevSpan, checkForNewlineChars: false);
                headers.OnHeadersComplete();
                var prevHeaderValue = ((IHeaderDictionary)headers)[headerName].ToString();

                Assert.Equal(headerValueUtf16Latin1CrossOver, prevHeaderValue);
                Assert.NotSame(headerValueUtf16Latin1CrossOver, prevHeaderValue);

                headers.Reset();

                Assert.Throws<InvalidOperationException>((Action)(() =>
                {
                    var headerNameBytes = Encoding.ASCII.GetBytes((string)headerName).AsSpan();
                    var nextSpan = Encoding.Latin1.GetBytes(headerValueUtf16Latin1CrossOver).AsSpan();

                    Assert.False(nextSpan.SequenceEqual(Encoding.ASCII.GetBytes(headerValueUtf16Latin1CrossOver)));

                    headers.Append(headerNameBytes, nextSpan, checkForNewlineChars: false);
                }));
            }

            // Reset back to Ascii
            headerValue[i] = 'a';
        }
    }

    [Theory]
    [MemberData(nameof(KnownRequestHeaders))]
    public void Latin1ValuesAcceptedInLatin1ModeButNotReused(bool reuseValue, string headerName)
    {
        var headers = new HttpRequestHeaders(reuseHeaderValues: reuseValue, _ => Encoding.Latin1);

        var headerValue = new char[127]; // 64 + 32 + 16 + 8 + 4 + 2 + 1
        for (var i = 0; i < headerValue.Length; i++)
        {
            headerValue[i] = 'a';
        }

        for (var i = 0; i < headerValue.Length; i++)
        {
            // Set non-ascii Latin char that is valid Utf16 when widened; but not a valid utf8 -> utf16 conversion.
            headerValue[i] = '\u00a3';

            for (var mode = 0; mode <= 1; mode++)
            {
                string headerValueUtf16Latin1CrossOver;
                if (mode == 0)
                {
                    // Full length
                    headerValueUtf16Latin1CrossOver = new string(headerValue);
                }
                else
                {
                    // Truncated length (to ensure different paths from changing lengths in matching)
                    headerValueUtf16Latin1CrossOver = new string(headerValue.AsSpan().Slice(0, i + 1));
                }

                var headerNameBytes = Encoding.ASCII.GetBytes(headerName).AsSpan();
                var latinValueSpan = Encoding.Latin1.GetBytes(headerValueUtf16Latin1CrossOver).AsSpan();

                Assert.False(latinValueSpan.SequenceEqual(Encoding.ASCII.GetBytes(headerValueUtf16Latin1CrossOver)));

                headers.Reset();
                headers.Append(headerNameBytes, latinValueSpan, checkForNewlineChars: false);
                headers.OnHeadersComplete();
                var parsedHeaderValue1 = ((IHeaderDictionary)headers)[headerName].ToString();

                headers.Reset();
                headers.Append(headerNameBytes, latinValueSpan, checkForNewlineChars: false);
                headers.OnHeadersComplete();
                var parsedHeaderValue2 = ((IHeaderDictionary)headers)[headerName].ToString();

                Assert.Equal(headerValueUtf16Latin1CrossOver, parsedHeaderValue1);
                Assert.Equal(parsedHeaderValue1, parsedHeaderValue2);
                Assert.NotSame(parsedHeaderValue1, parsedHeaderValue2);
            }

            // Reset back to Ascii
            headerValue[i] = 'a';
        }
    }

    [Theory]
    [MemberData(nameof(KnownRequestHeaders))]
    public void NullCharactersRejectedInUTF8AndLatin1Mode(bool useLatin1, string headerName)
    {
        var headers = new HttpRequestHeaders(encodingSelector: useLatin1 ? _ => Encoding.Latin1 : (Func<string, Encoding>)null);

        var valueArray = new char[127]; // 64 + 32 + 16 + 8 + 4 + 2 + 1
        for (var i = 0; i < valueArray.Length; i++)
        {
            valueArray[i] = 'a';
        }

        for (var i = 1; i < valueArray.Length; i++)
        {
            // Set non-ascii Latin char that is valid Utf16 when widened; but not a valid utf8 -> utf16 conversion.
            valueArray[i] = '\0';
            string valueString = new string(valueArray);

            headers.Reset();

            Assert.Throws<InvalidOperationException>(() =>
            {
                var headerNameBytes = Encoding.ASCII.GetBytes(headerName).AsSpan();
                var valueSpan = Encoding.ASCII.GetBytes(valueString).AsSpan();

                headers.Append(headerNameBytes, valueSpan, checkForNewlineChars: false);
            });

            valueArray[i] = 'a';
        }
    }

    [Fact]
    public void CanSpecifyEncodingBasedOnHeaderName()
    {
        const string headerValue = "Hello \u03a0";
        var acceptNameBytes = Encoding.ASCII.GetBytes(HeaderNames.Accept);
        var cookieNameBytes = Encoding.ASCII.GetBytes(HeaderNames.Cookie);
        var headerValueBytes = Encoding.UTF8.GetBytes(headerValue);

        var headers = new HttpRequestHeaders(encodingSelector: headerName =>
        {
            // For known headers, the HeaderNames value is passed in.
            if (ReferenceEquals(headerName, HeaderNames.Accept))
            {
                return Encoding.GetEncoding("ASCII", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
            }

            return Encoding.UTF8;
        });

        Assert.Throws<InvalidOperationException>(() => headers.Append(acceptNameBytes, headerValueBytes, checkForNewlineChars: false));
        headers.Append(cookieNameBytes, headerValueBytes, checkForNewlineChars: false);
        headers.OnHeadersComplete();

        var parsedAcceptHeaderValue = ((IHeaderDictionary)headers).Accept.ToString();
        var parsedCookieHeaderValue = ((IHeaderDictionary)headers).Cookie.ToString();

        Assert.Empty(parsedAcceptHeaderValue);
        Assert.Equal(headerValue, parsedCookieHeaderValue);
    }

    [Fact]
    public void CanSpecifyEncodingForContentLength()
    {
        var contentLengthNameBytes = Encoding.ASCII.GetBytes(HeaderNames.ContentLength);
        // Always 32 bits per code point, so not a superset of ASCII
        var contentLengthValueBytes = Encoding.UTF32.GetBytes("1337");

        var headers = new HttpRequestHeaders(encodingSelector: _ => Encoding.UTF32);
        headers.Append(contentLengthNameBytes, contentLengthValueBytes, checkForNewlineChars: false);
        headers.OnHeadersComplete();

        Assert.Equal(1337, headers.ContentLength);

        Assert.Throws<InvalidOperationException>(() =>
            new HttpRequestHeaders().Append(contentLengthNameBytes, contentLengthValueBytes, checkForNewlineChars: false));
    }

    [Fact]
    public void ValueReuseNeverWhenUnknownHeader()
    {
        const string HeaderName = "An-Unknown-Header";
        const string HeaderValue = "Hello";

        var headers = new HttpRequestHeaders(reuseHeaderValues: true);

        for (var i = 0; i < 6; i++)
        {
            var prevName = ChangeNameCase(HeaderName, variant: i);
            var nextName = ChangeNameCase(HeaderName, variant: i + 1);

            var values = GetHeaderValues(headers, prevName, nextName, HeaderValue, HeaderValue);

            Assert.Equal(HeaderValue, values.PrevHeaderValue);
            Assert.NotSame(HeaderValue, values.PrevHeaderValue);

            Assert.Equal(HeaderValue, values.NextHeaderValue);
            Assert.NotSame(HeaderValue, values.NextHeaderValue);

            Assert.Equal(values.PrevHeaderValue, values.NextHeaderValue);

            Assert.NotSame(values.PrevHeaderValue, values.NextHeaderValue);
        }
    }

    [Theory]
    [MemberData(nameof(KnownRequestHeaders))]
    public void ValueReuseEmptyAfterReset(bool reuseValue, string headerName)
    {
        const string HeaderValue = "Hello";

        var headers = new HttpRequestHeaders(reuseHeaderValues: reuseValue);
        var headerNameBytes = Encoding.ASCII.GetBytes(headerName).AsSpan();
        var prevSpan = Encoding.UTF8.GetBytes(HeaderValue).AsSpan();

        headers.Append(headerNameBytes, prevSpan, checkForNewlineChars: false);
        headers.OnHeadersComplete();
        var prevHeaderValue = ((IHeaderDictionary)headers)[headerName].ToString();

        Assert.NotNull(prevHeaderValue);
        Assert.NotEqual(string.Empty, prevHeaderValue);
        Assert.Equal(HeaderValue, prevHeaderValue);
        Assert.NotSame(HeaderValue, prevHeaderValue);
        Assert.Single(headers);
        var count = headers.Count;
        Assert.Equal(1, count);

        headers.Reset();

        // Empty after reset
        var nextHeaderValue = ((IHeaderDictionary)headers)[headerName].ToString();

        Assert.NotNull(nextHeaderValue);
        Assert.Equal(string.Empty, nextHeaderValue);
        Assert.NotEqual(HeaderValue, nextHeaderValue);
        Assert.Empty(headers);
        count = headers.Count;
        Assert.Equal(0, count);

        headers.OnHeadersComplete();

        // Still empty after complete
        nextHeaderValue = ((IHeaderDictionary)headers)[headerName].ToString();

        Assert.NotNull(nextHeaderValue);
        Assert.Equal(string.Empty, nextHeaderValue);
        Assert.NotEqual(HeaderValue, nextHeaderValue);
        Assert.Empty(headers);
        count = headers.Count;
        Assert.Equal(0, count);
    }

    [Theory]
    [MemberData(nameof(KnownRequestHeaders))]
    public void MultiValueReuseEmptyAfterReset(bool reuseValue, string headerName)
    {
        const string HeaderValue1 = "Hello1";
        const string HeaderValue2 = "Hello2";

        var headers = new HttpRequestHeaders(reuseHeaderValues: reuseValue);
        var headerNameBytes = Encoding.ASCII.GetBytes(headerName).AsSpan();
        var prevSpan1 = Encoding.UTF8.GetBytes(HeaderValue1).AsSpan();
        var prevSpan2 = Encoding.UTF8.GetBytes(HeaderValue2).AsSpan();

        headers.Append(headerNameBytes, prevSpan1, checkForNewlineChars: false);
        headers.Append(headerNameBytes, prevSpan2, checkForNewlineChars: false);
        headers.OnHeadersComplete();
        var prevHeaderValue = ((IHeaderDictionary)headers)[headerName];

        Assert.Equal(2, prevHeaderValue.Count);

        Assert.NotEqual(string.Empty, prevHeaderValue.ToString());
        Assert.Single(headers);
        var count = headers.Count;
        Assert.Equal(1, count);

        headers.Reset();

        // Empty after reset
        var nextHeaderValue = ((IHeaderDictionary)headers)[headerName].ToString();

        Assert.NotNull(nextHeaderValue);
        Assert.Equal(string.Empty, nextHeaderValue);
        Assert.Empty(headers);
        count = headers.Count;
        Assert.Equal(0, count);

        headers.OnHeadersComplete();

        // Still empty after complete
        nextHeaderValue = ((IHeaderDictionary)headers)[headerName].ToString();

        Assert.NotNull(nextHeaderValue);
        Assert.Equal(string.Empty, nextHeaderValue);
        Assert.Empty(headers);
        count = headers.Count;
        Assert.Equal(0, count);
    }

    [Fact]
    public void ContentLengthEnumerableWithoutOtherKnownHeader()
    {
        IHeaderDictionary headers = new HttpRequestHeaders();
        headers["content-length"] = "1024";
        Assert.Single(headers);
        headers["unknown"] = "value";
        Assert.Equal(2, headers.Count()); // NB: enumerable count, not property
        headers["host"] = "myhost";
        Assert.Equal(3, headers.Count()); // NB: enumerable count, not property
    }

    private static (string PrevHeaderValue, string NextHeaderValue) GetHeaderValues(HttpRequestHeaders headers, string prevName, string nextName, string prevValue, string nextValue)
    {
        headers.Reset();
        var headerName = Encoding.ASCII.GetBytes(prevName).AsSpan();
        var prevSpan = Encoding.UTF8.GetBytes(prevValue).AsSpan();

        headers.Append(headerName, prevSpan, checkForNewlineChars: false);
        headers.OnHeadersComplete();
        var prevHeaderValue = ((IHeaderDictionary)headers)[prevName].ToString();

        headers.Reset();

        if (nextValue != null)
        {
            headerName = Encoding.ASCII.GetBytes(prevName).AsSpan();
            var nextSpan = Encoding.UTF8.GetBytes(nextValue).AsSpan();
            headers.Append(headerName, nextSpan, checkForNewlineChars: false);
        }

        headers.OnHeadersComplete();

        var newHeaderValue = ((IHeaderDictionary)headers)[nextName].ToString();

        return (prevHeaderValue, newHeaderValue);
    }

    private static string ChangeNameCase(string name, int variant)
    {
        switch ((variant / 2) % 3)
        {
            case 0:
                return name;
            case 1:
                return name.ToLowerInvariant();
            case 2:
                return name.ToUpperInvariant();
        }

        // Never reached
        Assert.Fail();
        return name;
    }

    // Content-Length is numeric not a string, so we exclude it from the string reuse tests
    public static IEnumerable<object[]> KnownRequestHeaders =>
        RequestHeaders.Where(h => h.Name != "Content-Length").Select(h => new object[] { true, h.Name }).Concat(
        RequestHeaders.Where(h => h.Name != "Content-Length").Select(h => new object[] { false, h.Name }));
}
