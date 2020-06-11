// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Primitives;
using Xunit;
using static CodeGenerator.KnownHeaders;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class HttpRequestHeadersTests
    {
        [Fact]
        public void InitialDictionaryIsEmpty()
        {
            IDictionary<string, StringValues> headers = new HttpRequestHeaders();

            Assert.Equal(0, headers.Count);
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
        public void EntriesCanBeEnumerated()
        {
            IDictionary<string, StringValues> headers = new HttpRequestHeaders();
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

            Assert.Equal(0, headers.Count);
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

            Assert.Equal(1, headers.Count);
            Assert.False(headers.TryGetValue("host", out value));
            Assert.False(headers.TryGetValue("custom", out value));
            Assert.True(headers.TryGetValue("Content-Length", out value));

            Assert.True(headers.Remove("Content-Length"));
            Assert.False(headers.Remove("Content-Length"));

            Assert.Equal(0, headers.Count);
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
            Assert.Equal(new[] { "localhost" }, entries[1].Value);

            Assert.Equal("Content-Length", entries[2].Key);
            Assert.Equal(new[] { "0" }, entries[2].Value);

            Assert.Equal("custom", entries[3].Key);
            Assert.Equal(new[] { "value" }, entries[3].Value);

            Assert.Null(entries[4].Key);
            Assert.Equal(new StringValues(), entries[4].Value);
        }

        [Fact]
        public void AppendThrowsWhenHeaderNameContainsNonASCIICharacters()
        {
            var headers = new HttpRequestHeaders();
            const string key = "\u00141\u00F3d\017c";

            var encoding = Encoding.GetEncoding("iso-8859-1");
            var exception = Assert.Throws<BadHttpRequestException>(
                () => headers.Append(encoding.GetBytes(key), Encoding.ASCII.GetBytes("value")));
            Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        }

        [Theory]
        [MemberData(nameof(KnownRequestHeaders))]
        public void ValueReuseOnlyWhenAllowed(bool reuseValue, KnownHeader header)
        {
            const string HeaderValue = "Hello";

            var headers = new HttpRequestHeaders(reuseHeaderValues: reuseValue);

            for (var i = 0; i < 6; i++)
            {
                var prevName = ChangeNameCase(header.Name, variant: i);
                var nextName = ChangeNameCase(header.Name, variant: i + 1);

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
        public void ValueReuseChangedValuesOverwrite(bool reuseValue, KnownHeader header)
        {
            const string HeaderValue1 = "Hello1";
            const string HeaderValue2 = "Hello2";
            var headers = new HttpRequestHeaders(reuseHeaderValues: reuseValue);

            for (var i = 0; i < 6; i++)
            {
                var prevName = ChangeNameCase(header.Name, variant: i);
                var nextName = ChangeNameCase(header.Name, variant: i + 1);

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
        public void ValueReuseMissingValuesClear(bool reuseValue, KnownHeader header)
        {
            const string HeaderValue1 = "Hello1";
            var headers = new HttpRequestHeaders(reuseHeaderValues: reuseValue);

            for (var i = 0; i < 6; i++)
            {
                var prevName = ChangeNameCase(header.Name, variant: i);
                var nextName = ChangeNameCase(header.Name, variant: i + 1);

                var values = GetHeaderValues(headers, prevName, nextName, HeaderValue1, nextValue: null);

                Assert.Equal(HeaderValue1, values.PrevHeaderValue);
                Assert.NotSame(HeaderValue1, values.PrevHeaderValue);

                Assert.Equal(string.Empty, values.NextHeaderValue);

                Assert.NotEqual(values.PrevHeaderValue, values.NextHeaderValue);
            }
        }

        [Theory]
        [MemberData(nameof(KnownRequestHeaders))]
        public void ValueReuseNeverWhenNotAscii(bool reuseValue, KnownHeader header)
        {
            const string HeaderValue = "Hello \u03a0";

            var headers = new HttpRequestHeaders(reuseHeaderValues: reuseValue);

            for (var i = 0; i < 6; i++)
            {
                var prevName = ChangeNameCase(header.Name, variant: i);
                var nextName = ChangeNameCase(header.Name, variant: i + 1);

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
        public void ValueReuseLatin1NotConfusedForUtf16AndStillRejected(bool reuseValue, KnownHeader header)
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

                    var headerName = Encoding.ASCII.GetBytes(header.Name).AsSpan();
                    var prevSpan = Encoding.UTF8.GetBytes(headerValueUtf16Latin1CrossOver).AsSpan();

                    headers.Append(headerName, prevSpan);
                    headers.OnHeadersComplete();
                    var prevHeaderValue = ((IHeaderDictionary)headers)[header.Name].ToString();

                    Assert.Equal(headerValueUtf16Latin1CrossOver, prevHeaderValue);
                    Assert.NotSame(headerValueUtf16Latin1CrossOver, prevHeaderValue);

                    headers.Reset();

                    Assert.Throws<InvalidOperationException>(() =>
                    {
                        var headerName = Encoding.ASCII.GetBytes(header.Name).AsSpan();
                        var nextSpan = Encoding.GetEncoding("iso-8859-1").GetBytes(headerValueUtf16Latin1CrossOver).AsSpan();

                        Assert.False(nextSpan.SequenceEqual(Encoding.ASCII.GetBytes(headerValueUtf16Latin1CrossOver)));

                        headers.Append(headerName, nextSpan);
                    });
                }

                // Reset back to Ascii
                headerValue[i] = 'a';
            }
        }

        [Theory]
        [MemberData(nameof(KnownRequestHeaders))]
        public void Latin1ValuesAcceptedInLatin1ModeButNotReused(bool reuseValue, KnownHeader header)
        {
            var headers = new HttpRequestHeaders(reuseHeaderValues: reuseValue, useLatin1: true);

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

                    var headerName = Encoding.ASCII.GetBytes(header.Name).AsSpan();
                    var latinValueSpan = Encoding.GetEncoding("iso-8859-1").GetBytes(headerValueUtf16Latin1CrossOver).AsSpan();

                    Assert.False(latinValueSpan.SequenceEqual(Encoding.ASCII.GetBytes(headerValueUtf16Latin1CrossOver)));

                    headers.Append(headerName, latinValueSpan);
                    headers.OnHeadersComplete();
                    var parsedHeaderValue = ((IHeaderDictionary)headers)[header.Name].ToString();

                    Assert.Equal(headerValueUtf16Latin1CrossOver, parsedHeaderValue);
                    Assert.NotSame(headerValueUtf16Latin1CrossOver, parsedHeaderValue);
                }

                // Reset back to Ascii
                headerValue[i] = 'a';
            }
        }

        [Theory]
        [MemberData(nameof(KnownRequestHeaders))]
        public void NullCharactersRejectedInUTF8AndLatin1Mode(bool useLatin1, KnownHeader header)
        {
            var headers = new HttpRequestHeaders(useLatin1: useLatin1);

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
                    var headerName = Encoding.ASCII.GetBytes(header.Name).AsSpan();
                    var valueSpan = Encoding.ASCII.GetBytes(valueString).AsSpan();

                    headers.Append(headerName, valueSpan);
                });

                valueArray[i] = 'a';
            }
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
        public void ValueReuseEmptyAfterReset(bool reuseValue, KnownHeader header)
        {
            const string HeaderValue = "Hello";

            var headers = new HttpRequestHeaders(reuseHeaderValues: reuseValue);
            var headerName = Encoding.ASCII.GetBytes(header.Name).AsSpan();
            var prevSpan = Encoding.UTF8.GetBytes(HeaderValue).AsSpan();

            headers.Append(headerName, prevSpan);
            headers.OnHeadersComplete();
            var prevHeaderValue = ((IHeaderDictionary)headers)[header.Name].ToString();

            Assert.NotNull(prevHeaderValue);
            Assert.NotEqual(string.Empty, prevHeaderValue);
            Assert.Equal(HeaderValue, prevHeaderValue);
            Assert.NotSame(HeaderValue, prevHeaderValue);
            Assert.Single(headers);
            var count = headers.Count;
            Assert.Equal(1, count);

            headers.Reset();

            // Empty after reset
            var nextHeaderValue = ((IHeaderDictionary)headers)[header.Name].ToString();

            Assert.NotNull(nextHeaderValue);
            Assert.Equal(string.Empty, nextHeaderValue);
            Assert.NotEqual(HeaderValue, nextHeaderValue);
            Assert.Empty(headers);
            count = headers.Count;
            Assert.Equal(0, count);

            headers.OnHeadersComplete();

            // Still empty after complete
            nextHeaderValue = ((IHeaderDictionary)headers)[header.Name].ToString();

            Assert.NotNull(nextHeaderValue);
            Assert.Equal(string.Empty, nextHeaderValue);
            Assert.NotEqual(HeaderValue, nextHeaderValue);
            Assert.Empty(headers);
            count = headers.Count;
            Assert.Equal(0, count);
        }

        [Theory]
        [MemberData(nameof(KnownRequestHeaders))]
        public void MultiValueReuseEmptyAfterReset(bool reuseValue, KnownHeader header)
        {
            const string HeaderValue1 = "Hello1";
            const string HeaderValue2 = "Hello2";

            var headers = new HttpRequestHeaders(reuseHeaderValues: reuseValue);
            var headerName = Encoding.ASCII.GetBytes(header.Name).AsSpan();
            var prevSpan1 = Encoding.UTF8.GetBytes(HeaderValue1).AsSpan();
            var prevSpan2 = Encoding.UTF8.GetBytes(HeaderValue2).AsSpan();

            headers.Append(headerName, prevSpan1);
            headers.Append(headerName, prevSpan2);
            headers.OnHeadersComplete();
            var prevHeaderValue = ((IHeaderDictionary)headers)[header.Name];

            Assert.Equal(2, prevHeaderValue.Count);

            Assert.NotEqual(string.Empty, prevHeaderValue.ToString());
            Assert.Single(headers);
            var count = headers.Count;
            Assert.Equal(1, count);

            headers.Reset();

            // Empty after reset
            var nextHeaderValue = ((IHeaderDictionary)headers)[header.Name].ToString();

            Assert.NotNull(nextHeaderValue);
            Assert.Equal(string.Empty, nextHeaderValue);
            Assert.Empty(headers);
            count = headers.Count;
            Assert.Equal(0, count);

            headers.OnHeadersComplete();

            // Still empty after complete
            nextHeaderValue = ((IHeaderDictionary)headers)[header.Name].ToString();

            Assert.NotNull(nextHeaderValue);
            Assert.Equal(string.Empty, nextHeaderValue);
            Assert.Empty(headers);
            count = headers.Count;
            Assert.Equal(0, count);
        }

        private static (string PrevHeaderValue, string NextHeaderValue) GetHeaderValues(HttpRequestHeaders headers, string prevName, string nextName, string prevValue, string nextValue)
        {
            headers.Reset();
            var headerName = Encoding.ASCII.GetBytes(prevName).AsSpan();
            var prevSpan = Encoding.UTF8.GetBytes(prevValue).AsSpan();

            headers.Append(headerName, prevSpan);
            headers.OnHeadersComplete();
            var prevHeaderValue = ((IHeaderDictionary)headers)[prevName].ToString();

            headers.Reset();

            if (nextValue != null)
            {
                headerName = Encoding.ASCII.GetBytes(prevName).AsSpan();
                var nextSpan = Encoding.UTF8.GetBytes(nextValue).AsSpan();
                headers.Append(headerName, nextSpan);
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
            Assert.False(true);
            return name;
        }

        // Content-Length is numeric not a string, so we exclude it from the string reuse tests
        public static IEnumerable<object[]> KnownRequestHeaders =>
            RequestHeaders.Where(h => h.Name != "Content-Length").Select(h => new object[] { true, h }).Concat(
            RequestHeaders.Where(h => h.Name != "Content-Length").Select(h => new object[] { false, h }));
    }
}
