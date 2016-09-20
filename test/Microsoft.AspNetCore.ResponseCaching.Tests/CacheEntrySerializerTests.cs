// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCaching.Internal;
using Xunit;

namespace Microsoft.AspNetCore.ResponseCaching.Tests
{
    public class CacheEntrySerializerTests
    {
        [Fact]
        public void Serialize_NullObject_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => CacheEntrySerializer.Serialize(null));
        }

        [Fact]
        public void Serialize_UnknownObject_Throws()
        {
            Assert.Throws<NotSupportedException>(() => CacheEntrySerializer.Serialize(new object()));
        }

        [Fact]
        public void Deserialize_NullObject_ReturnsNull()
        {
            Assert.Null(CacheEntrySerializer.Deserialize(null));
        }

        [Fact]
        public void RoundTrip_CachedResponseBody_Succeeds()
        {
            var cachedResponseBody = new CachedResponseBody()
            {
                Body = Encoding.ASCII.GetBytes("Hello world"),
            };

            AssertCachedResponseBodyEqual(cachedResponseBody, (CachedResponseBody)CacheEntrySerializer.Deserialize(CacheEntrySerializer.Serialize(cachedResponseBody)));
        }

        [Fact]
        public void RoundTrip_CachedResponseWithoutBody_Succeeds()
        {
            var headers = new HeaderDictionary();
            headers["keyA"] = "valueA";
            headers["keyB"] = "valueB";
            var cachedResponse = new CachedResponse()
            {
                BodyKeyPrefix = FastGuid.NewGuid().IdString,
                Created = DateTimeOffset.UtcNow,
                StatusCode = StatusCodes.Status200OK,
                Headers = headers
            };

            AssertCachedResponseEqual(cachedResponse, (CachedResponse)CacheEntrySerializer.Deserialize(CacheEntrySerializer.Serialize(cachedResponse)));
        }

        [Fact]
        public void RoundTrip_CachedResponseWithBody_Succeeds()
        {
            var headers = new HeaderDictionary();
            headers["keyA"] = "valueA";
            headers["keyB"] = "valueB";
            var cachedResponse = new CachedResponse()
            {
                BodyKeyPrefix = FastGuid.NewGuid().IdString,
                Created = DateTimeOffset.UtcNow,
                StatusCode = StatusCodes.Status200OK,
                Body = Encoding.ASCII.GetBytes("Hello world"),
                Headers = headers
            };

            AssertCachedResponseEqual(cachedResponse, (CachedResponse)CacheEntrySerializer.Deserialize(CacheEntrySerializer.Serialize(cachedResponse)));
        }

        [Fact]
        public void RoundTrip_CachedVaryByRule_EmptyRules_Succeeds()
        {
            var cachedVaryByRule = new CachedVaryByRules()
            {
                VaryByKeyPrefix = FastGuid.NewGuid().IdString
            };

            AssertCachedVaryByRuleEqual(cachedVaryByRule, (CachedVaryByRules)CacheEntrySerializer.Deserialize(CacheEntrySerializer.Serialize(cachedVaryByRule)));
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

            AssertCachedVaryByRuleEqual(cachedVaryByRule, (CachedVaryByRules)CacheEntrySerializer.Deserialize(CacheEntrySerializer.Serialize(cachedVaryByRule)));
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

            AssertCachedVaryByRuleEqual(cachedVaryByRule, (CachedVaryByRules)CacheEntrySerializer.Deserialize(CacheEntrySerializer.Serialize(cachedVaryByRule)));
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

            AssertCachedVaryByRuleEqual(cachedVaryByRule, (CachedVaryByRules)CacheEntrySerializer.Deserialize(CacheEntrySerializer.Serialize(cachedVaryByRule)));
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
            var serializedEntry = CacheEntrySerializer.Serialize(cachedVaryByRule);
            Array.Reverse(serializedEntry);

            Assert.Null(CacheEntrySerializer.Deserialize(serializedEntry));
        }

        private static void AssertCachedResponseBodyEqual(CachedResponseBody expected, CachedResponseBody actual)
        {
            Assert.True(expected.Body.SequenceEqual(actual.Body));
        }

        private static void AssertCachedResponseEqual(CachedResponse expected, CachedResponse actual)
        {
            Assert.NotNull(actual);
            Assert.NotNull(expected);
            Assert.Equal(expected.BodyKeyPrefix, actual.BodyKeyPrefix);
            Assert.Equal(expected.Created, actual.Created);
            Assert.Equal(expected.StatusCode, actual.StatusCode);
            Assert.Equal(expected.Headers.Count, actual.Headers.Count);
            foreach (var expectedHeader in expected.Headers)
            {
                Assert.Equal(expectedHeader.Value, actual.Headers[expectedHeader.Key]);
            }
            if (expected.Body == null)
            {
                Assert.Null(actual.Body);
            }
            else
            {
                Assert.True(expected.Body.SequenceEqual(actual.Body));
            }
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
