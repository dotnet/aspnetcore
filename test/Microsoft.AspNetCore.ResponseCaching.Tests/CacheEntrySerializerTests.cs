// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCaching.Internal;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.ResponseCaching.Tests
{
    public class CacheEntrySerializerTests
    {
        [Fact]
        public void Serialize_NullObject_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => ResponseCacheEntrySerializer.Serialize(null));
        }

        private class UnknownResponseCacheEntry : IResponseCacheEntry
        {
        }

        [Fact]
        public void Serialize_UnknownObject_Throws()
        {
            Assert.Throws<NotSupportedException>(() => ResponseCacheEntrySerializer.Serialize(new UnknownResponseCacheEntry()));
        }

        [Fact]
        public void Deserialize_NullObject_ReturnsNull()
        {
            Assert.Null(ResponseCacheEntrySerializer.Deserialize(null));
        }

        [Fact]
        public void RoundTrip_CachedResponseWithBody_Succeeds()
        {
            var headers = new HeaderDictionary();
            headers["keyA"] = "valueA";
            headers["keyB"] = "valueB";
            var body = Encoding.ASCII.GetBytes("Hello world");
            var cachedResponse = new CachedResponse()
            {
                Created = DateTimeOffset.UtcNow,
                StatusCode = StatusCodes.Status200OK,
                Body = new SegmentReadStream(new List<byte[]>(new[] { body }), body.Length),
                Headers = headers
            };

            AssertCachedResponseEqual(cachedResponse, (CachedResponse)ResponseCacheEntrySerializer.Deserialize(ResponseCacheEntrySerializer.Serialize(cachedResponse)));
        }

        [Fact]
        public void RoundTrip_CachedResponseWithMultivalueHeaders_Succeeds()
        {
            var headers = new HeaderDictionary();
            headers["keyA"] = new StringValues(new[] { "ValueA", "ValueB" });
            var body = Encoding.ASCII.GetBytes("Hello world");
            var cachedResponse = new CachedResponse()
            {
                Created = DateTimeOffset.UtcNow,
                StatusCode = StatusCodes.Status200OK,
                Body = new SegmentReadStream(new List<byte[]>(new[] { body }), body.Length),
                Headers = headers
            };

            AssertCachedResponseEqual(cachedResponse, (CachedResponse)ResponseCacheEntrySerializer.Deserialize(ResponseCacheEntrySerializer.Serialize(cachedResponse)));
        }

        [Fact]
        public void RoundTrip_CachedResponseWithEmptyHeaders_Succeeds()
        {
            var headers = new HeaderDictionary();
            headers["keyA"] = StringValues.Empty;
            var body = Encoding.ASCII.GetBytes("Hello world");
            var cachedResponse = new CachedResponse()
            {
                Created = DateTimeOffset.UtcNow,
                StatusCode = StatusCodes.Status200OK,
                Body = new SegmentReadStream(new List<byte[]>(new[] { body }), body.Length),
                Headers = headers
            };

            AssertCachedResponseEqual(cachedResponse, (CachedResponse)ResponseCacheEntrySerializer.Deserialize(ResponseCacheEntrySerializer.Serialize(cachedResponse)));
        }

        [Fact]
        public void RoundTrip_CachedVaryByRule_EmptyRules_Succeeds()
        {
            var cachedVaryByRule = new CachedVaryByRules()
            {
                VaryByKeyPrefix = FastGuid.NewGuid().IdString
            };

            AssertCachedVaryByRuleEqual(cachedVaryByRule, (CachedVaryByRules)ResponseCacheEntrySerializer.Deserialize(ResponseCacheEntrySerializer.Serialize(cachedVaryByRule)));
        }

        [Fact]
        public void RoundTrip_CachedVaryByRule_HeadersOnly_Succeeds()
        {
            var headers = new[] { "headerA", "headerB" };
            var cachedVaryByRule = new CachedVaryByRules()
            {
                VaryByKeyPrefix = FastGuid.NewGuid().IdString,
                Headers = headers
            };

            AssertCachedVaryByRuleEqual(cachedVaryByRule, (CachedVaryByRules)ResponseCacheEntrySerializer.Deserialize(ResponseCacheEntrySerializer.Serialize(cachedVaryByRule)));
        }

        [Fact]
        public void RoundTrip_CachedVaryByRule_QueryKeysOnly_Succeeds()
        {
            var queryKeys = new[] { "queryA", "queryB" };
            var cachedVaryByRule = new CachedVaryByRules()
            {
                VaryByKeyPrefix = FastGuid.NewGuid().IdString,
                QueryKeys = queryKeys
            };

            AssertCachedVaryByRuleEqual(cachedVaryByRule, (CachedVaryByRules)ResponseCacheEntrySerializer.Deserialize(ResponseCacheEntrySerializer.Serialize(cachedVaryByRule)));
        }

        [Fact]
        public void RoundTrip_CachedVaryByRule_HeadersAndQueryKeys_Succeeds()
        {
            var headers = new[] { "headerA", "headerB" };
            var queryKeys = new[] { "queryA", "queryB" };
            var cachedVaryByRule = new CachedVaryByRules()
            {
                VaryByKeyPrefix = FastGuid.NewGuid().IdString,
                Headers = headers,
                QueryKeys = queryKeys
            };

            AssertCachedVaryByRuleEqual(cachedVaryByRule, (CachedVaryByRules)ResponseCacheEntrySerializer.Deserialize(ResponseCacheEntrySerializer.Serialize(cachedVaryByRule)));
        }

        [Fact]
        public void Deserialize_InvalidEntries_ReturnsNull()
        {
            var headers = new[] { "headerA", "headerB" };
            var cachedVaryByRule = new CachedVaryByRules()
            {
                VaryByKeyPrefix = FastGuid.NewGuid().IdString,
                Headers = headers
            };
            var serializedEntry = ResponseCacheEntrySerializer.Serialize(cachedVaryByRule);
            Array.Reverse(serializedEntry);

            Assert.Null(ResponseCacheEntrySerializer.Deserialize(serializedEntry));
        }

        private static void AssertCachedResponseEqual(CachedResponse expected, CachedResponse actual)
        {
            Assert.NotNull(actual);
            Assert.NotNull(expected);
            Assert.Equal(expected.Created, actual.Created);
            Assert.Equal(expected.StatusCode, actual.StatusCode);
            Assert.Equal(expected.Headers.Count, actual.Headers.Count);
            foreach (var expectedHeader in expected.Headers)
            {
                Assert.Equal(expectedHeader.Value, actual.Headers[expectedHeader.Key]);
            }

            Assert.Equal(expected.Body.Length, actual.Body.Length);
            var bodyLength = (int)expected.Body.Length;
            var expectedBytes = new byte[bodyLength];
            var actualBytes = new byte[bodyLength];
            expected.Body.Position = 0; // Rewind
            Assert.Equal(bodyLength, expected.Body.Read(expectedBytes, 0, bodyLength));
            Assert.Equal(bodyLength, actual.Body.Read(actualBytes, 0, bodyLength));
            Assert.True(expectedBytes.SequenceEqual(actualBytes));
        }

        private static void AssertCachedVaryByRuleEqual(CachedVaryByRules expected, CachedVaryByRules actual)
        {
            Assert.NotNull(actual);
            Assert.NotNull(expected);
            Assert.Equal(expected.VaryByKeyPrefix, actual.VaryByKeyPrefix);
            Assert.Equal(expected.Headers, actual.Headers);
            Assert.Equal(expected.QueryKeys, actual.QueryKeys);
        }
    }
}
