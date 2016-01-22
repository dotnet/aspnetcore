using System;
using System.Collections.Generic;
using Microsoft.AspNet.Server.Kestrel.Http;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNet.Server.KestrelTests
{
    public class FrameRequestHeadersTests
    {
        [Fact]
        public void InitialDictionaryIsEmpty()
        {
            IDictionary<string, StringValues> headers = new FrameRequestHeaders();

            Assert.Equal(0, headers.Count);
            Assert.False(headers.IsReadOnly);
        }

        [Fact]
        public void SettingUnknownHeadersWorks()
        {
            IDictionary<string, StringValues> headers = new FrameRequestHeaders();

            headers["custom"] = new[] { "value" };

            Assert.NotNull(headers["custom"]);
            Assert.Equal(1, headers["custom"].Count);
            Assert.Equal("value", headers["custom"][0]);
        }

        [Fact]
        public void SettingKnownHeadersWorks()
        {
            IDictionary<string, StringValues> headers = new FrameRequestHeaders();

            headers["host"] = new[] { "value" };

            Assert.NotNull(headers["host"]);
            Assert.Equal(1, headers["host"].Count);
            Assert.Equal("value", headers["host"][0]);
        }

        [Fact]
        public void KnownAndCustomHeaderCountAddedTogether()
        {
            IDictionary<string, StringValues> headers = new FrameRequestHeaders();

            headers["host"] = new[] { "value" };
            headers["custom"] = new[] { "value" };

            Assert.Equal(2, headers.Count);
        }

        [Fact]
        public void TryGetValueWorksForKnownAndUnknownHeaders()
        {
            IDictionary<string, StringValues> headers = new FrameRequestHeaders();

            StringValues value;
            Assert.False(headers.TryGetValue("host", out value));
            Assert.False(headers.TryGetValue("custom", out value));

            headers["host"] = new[] { "value" };
            Assert.True(headers.TryGetValue("host", out value));
            Assert.False(headers.TryGetValue("custom", out value));

            headers["custom"] = new[] { "value" };
            Assert.True(headers.TryGetValue("host", out value));
            Assert.True(headers.TryGetValue("custom", out value));
        }

        [Fact]
        public void SameExceptionThrownForMissingKey()
        {
            IDictionary<string, StringValues> headers = new FrameRequestHeaders();

            Assert.Throws<KeyNotFoundException>(() => headers["custom"]);
            Assert.Throws<KeyNotFoundException>(() => headers["host"]);
        }

        [Fact]
        public void EntriesCanBeEnumerated()
        {
            IDictionary<string, StringValues> headers = new FrameRequestHeaders();
            var v1 = new[] { "localhost" };
            var v2 = new[] { "value" };
            headers["host"] = v1;
            headers["custom"] = v2;

            Assert.Equal(
                new[] {
                    new KeyValuePair<string, StringValues>("Host", v1),
                    new KeyValuePair<string, StringValues>("custom", v2),
                },
                headers);
        }

        [Fact]
        public void KeysAndValuesCanBeEnumerated()
        {
            IDictionary<string, StringValues> headers = new FrameRequestHeaders();
            StringValues v1 = new[] { "localhost" };
            StringValues v2 = new[] { "value" };
            headers["host"] = v1;
            headers["custom"] = v2;

            Assert.Equal<string>(
                new[] { "Host", "custom" },
                headers.Keys);

            Assert.Equal<StringValues>(
                new[] { v1, v2 },
                headers.Values);
        }

        [Fact]
        public void ContainsAndContainsKeyWork()
        {
            IDictionary<string, StringValues> headers = new FrameRequestHeaders();
            var kv1 = new KeyValuePair<string, StringValues>("host", new[] { "localhost" });
            var kv2 = new KeyValuePair<string, StringValues>("custom", new[] { "value" });
            var kv1b = new KeyValuePair<string, StringValues>("host", new[] { "not-localhost" });
            var kv2b = new KeyValuePair<string, StringValues>("custom", new[] { "not-value" });

            Assert.False(headers.ContainsKey("host"));
            Assert.False(headers.ContainsKey("custom"));
            Assert.False(headers.Contains(kv1));
            Assert.False(headers.Contains(kv2));

            headers["host"] = kv1.Value;
            Assert.True(headers.ContainsKey("host"));
            Assert.False(headers.ContainsKey("custom"));
            Assert.True(headers.Contains(kv1));
            Assert.False(headers.Contains(kv2));
            Assert.False(headers.Contains(kv1b));
            Assert.False(headers.Contains(kv2b));

            headers["custom"] = kv2.Value;
            Assert.True(headers.ContainsKey("host"));
            Assert.True(headers.ContainsKey("custom"));
            Assert.True(headers.Contains(kv1));
            Assert.True(headers.Contains(kv2));
            Assert.False(headers.Contains(kv1b));
            Assert.False(headers.Contains(kv2b));
        }

        [Fact]
        public void AddWorksLikeSetAndThrowsIfKeyExists()
        {
            IDictionary<string, StringValues> headers = new FrameRequestHeaders();

            StringValues value;
            Assert.False(headers.TryGetValue("host", out value));
            Assert.False(headers.TryGetValue("custom", out value));

            headers.Add("host", new[] { "localhost" });
            headers.Add("custom", new[] { "value" });
            Assert.True(headers.TryGetValue("host", out value));
            Assert.True(headers.TryGetValue("custom", out value));

            Assert.Throws<ArgumentException>(() => headers.Add("host", new[] { "localhost" }));
            Assert.Throws<ArgumentException>(() => headers.Add("custom", new[] { "value" }));
            Assert.True(headers.TryGetValue("host", out value));
            Assert.True(headers.TryGetValue("custom", out value));
        }

        [Fact]
        public void ClearRemovesAllHeaders()
        {
            IDictionary<string, StringValues> headers = new FrameRequestHeaders();
            headers.Add("host", new[] { "localhost" });
            headers.Add("custom", new[] { "value" });

            StringValues value;
            Assert.Equal(2, headers.Count);
            Assert.True(headers.TryGetValue("host", out value));
            Assert.True(headers.TryGetValue("custom", out value));

            headers.Clear();

            Assert.Equal(0, headers.Count);
            Assert.False(headers.TryGetValue("host", out value));
            Assert.False(headers.TryGetValue("custom", out value));
        }

        [Fact]
        public void RemoveTakesHeadersOutOfDictionary()
        {
            IDictionary<string, StringValues> headers = new FrameRequestHeaders();
            headers.Add("host", new[] { "localhost" });
            headers.Add("custom", new[] { "value" });

            StringValues value;
            Assert.Equal(2, headers.Count);
            Assert.True(headers.TryGetValue("host", out value));
            Assert.True(headers.TryGetValue("custom", out value));

            Assert.True(headers.Remove("host"));
            Assert.False(headers.Remove("host"));

            Assert.Equal(1, headers.Count);
            Assert.False(headers.TryGetValue("host", out value));
            Assert.True(headers.TryGetValue("custom", out value));

            Assert.True(headers.Remove("custom"));
            Assert.False(headers.Remove("custom"));

            Assert.Equal(0, headers.Count);
            Assert.False(headers.TryGetValue("host", out value));
            Assert.False(headers.TryGetValue("custom", out value));
        }

        [Fact]
        public void CopyToMovesDataIntoArray()
        {
            IDictionary<string, StringValues> headers = new FrameRequestHeaders();
            headers.Add("host", new[] { "localhost" });
            headers.Add("custom", new[] { "value" });

            var entries = new KeyValuePair<string, StringValues>[4];
            headers.CopyTo(entries, 1);

            Assert.Null(entries[0].Key);
            Assert.Equal(new StringValues(), entries[0].Value);

            Assert.Equal("Host", entries[1].Key);
            Assert.Equal(new[] { "localhost" }, entries[1].Value);

            Assert.Equal("custom", entries[2].Key);
            Assert.Equal(new[] { "value" }, entries[2].Value);

            Assert.Null(entries[3].Key);
            Assert.Equal(new StringValues(), entries[0].Value);
        }
    }
}
