// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Primitives;
using Xunit;

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
                () => headers.Append(encoding.GetBytes(key), "value"));
            Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        }
    }
}
